// ==========================================
// 0. Environment Setup (Chapter 13)
// ==========================================
#r "nuget: pythonnet"

open System
open Python.Runtime

// NOTE: If Python.NET crashes, you may need to explicitly point to your Python DLL 
// before opening Python.Runtime, e.g.:
// Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", "/usr/lib/libpython3.10.so") 

// ==========================================
// 1. Domain Layer (Chapter 3)
// ==========================================
// Pure F# types. No Python dependencies here.

type SentimentCategory = 
| Positive 
| Negative 
| Neutral

type AnalysisResult = {
  InputText: string
  Score: float
  Category: SentimentCategory
}

// Logic to derive category from score (Pure Domain Logic)
let categorize score =
  if score > 0.6 then Positive
  elif score < 0.4 then Negative
  else Neutral

// ==========================================
// 2. Service Layer (Chapter 14 - Contracts)
// ==========================================

// The Interface: A function signature that takes a string and returns a float.
// We use a type alias for the function signature to keep it functional.
type ScorerStrategy = string -> float

// ==========================================
// 3. Infrastructure: The Adapters (Chapter 13 & 14)
// ==========================================

module Adapters =
  
  // --- Adapter A: The Mock (Safe, Fast, Pure F#) ---
  let mockScorer (text: string) =
    // A deterministic fake implementation
    let len = float text.Length
    // just return a pseudo-random normalized number based on length
    (len % 10.0) / 10.0

  // --- Adapter B: The Python Bridge (The "Real" Model) ---
  let pythonScorer : ScorerStrategy =
    
    // A. Initialize Engine (One-time setup)
    if not PythonEngine.IsInitialized then
      PythonEngine.Initialize()
      // Begin the Global Interpreter Lock (GIL)
      let _ = Py.GIL() 
      ()

    // B. Inject our "Library" into the Python Environment
    // In a real app, this would be `import tensorflow` etc.
    let pythonScript = """
def analyze_sentiment_v1(text):
  text = text.lower()
  score = 0.5
  if "excellent" in text or "superb" in text: score += 0.4
  if "fast" in text: score += 0.1
  if "terrible" in text or "abysmal" in text: score -= 0.4
  if "failure" in text: score -= 0.1
  return score
"""
    // Execute the definition in Python scope
    use scope = Py.CreateScope()
    scope.Exec(pythonScript) |> ignore

    // C. The Actual Function Implementation
    fun (text: string) ->
      // Acquire GIL for this specific call
      using (Py.GIL()) (fun _ ->
        // 1. Marshal Input: F# string -> Python Object
        let pyText = new PyString(text)
        
        // 2. Call Function
        let func = scope.Get("analyze_sentiment_v1")
        let result = func.Invoke(pyText)
        
        // 3. Marshal Output: Python Object -> F# float (Anti-Corruption)
        // This is the CRITICAL step. We convert to float immediately.
        // If we returned `result` (PyObject), we would leak Python into our domain.
        let fSharpScore = result.As<float>()
        fSharpScore
      )

// ==========================================
// 4. Application Layer (The Pipeline)
// ==========================================

module App = 
  
  // This function doesn't care if it's using Python or F# Mock.
  // It just asks for a 'ScorerStrategy'.
  let analyzeBatch (scorer: ScorerStrategy) (inputs: string list) =
    inputs
    |> List.map (fun text ->
      let score = scorer text
      { 
        InputText = text
        Score = score
        Category = categorize score 
      }
    )

  let printReport title results =
    printfn "\n--- Report: %s ---" title
    results 
    |> List.iter (fun r -> 
      printfn "[%A] (%.2f) \"%s\"" r.Category r.Score r.InputText
    )

// ==========================================
// 5. Execution
// ==========================================

let inputs = [
  "The service was excellent and fast."
  "Terrible experience, would not recommend."
  "It was okay, average performance."
  "Abysmal failure, system crashed."
  "Superb! Highly optimized."
]

printfn "System initializing..."

// SCENARIO 1: Run with Mock (e.g. Unit Tests)
// This proves our architecture is decoupled.
let mockResults = App.analyzeBatch Adapters.mockScorer inputs
App.printReport "DEV MODE (Mock Data)" mockResults

// SCENARIO 2: Run with Real Python (e.g. Production)
// We wrap this in a try/with block in case Python isn't configured on your machine
try
  let realResults = App.analyzeBatch Adapters.pythonScorer inputs
  App.printReport "PROD MODE (Python Engine)" realResults
with
| ex -> 
  printfn "\n[!] Python Interop Failed (Check Runtime.PythonDLL path): %s" ex.Message
  printfn "    Skipping Python execution demo."

// Cleanup (Optional but good practice)
if PythonEngine.IsInitialized then PythonEngine.Shutdown()