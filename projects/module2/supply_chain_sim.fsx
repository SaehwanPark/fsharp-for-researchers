// ==========================================
// 0. Environment & Dependencies (Chapter 2)
// ==========================================
#r "nuget: MathNet.Numerics"

open System
open System.IO
open MathNet.Numerics.Distributions
open MathNet.Numerics.Random

// ==========================================
// 1. Domain Modeling (Chapter 5 & 7)
// ==========================================

type Item = {
  Id: string
  CurrentStock: float
  AvgDemand: float
  StdDevDemand: float
}

type SimulationConfig = {
  DaysToSimulate: int
  Iterations: int
  RandomSeed: int
}

// ==========================================
// 2. Data Ingestion with Result Types (Chapter 8)
// ==========================================

// Helper to wrap parsing logic in a Result
let parseDouble (fieldName: string) (input: string) =
  match Double.TryParse input with
  | true, v -> Ok v
  | _ -> Error (sprintf "Invalid %s: '%s'" fieldName input)

// The Row Parser: String -> Result<Item, String>
// We use a "Validation" pattern here (collecting errors or failing fast)
let parseRow (line: string) : Result<Item, string> =
  let parts = line.Split(',')
  if parts.Length <> 4 then
    Error "Incorrect column count"
  else
    // Using a computation expression "builder" is advanced, 
    // so we will use nested matching for explicit clarity as per Module 2.
    match parseDouble "Stock" parts.[1], parseDouble "Mean" parts.[2], parseDouble "StdDev" parts.[3] with
    | Ok stock, Ok mean, Ok std ->
      if stock < 0.0 then Error "Stock cannot be negative"
      else 
        Ok { Id = parts.[0]; CurrentStock = stock; AvgDemand = mean; StdDevDemand = std }
    | Error e, _, _ -> Error e
    | _, Error e, _ -> Error e
    | _, _, Error e -> Error e

// ==========================================
// 3. Numerical Simulation (Chapter 6 & 7)
// ==========================================

/// Runs a single 30-day trajectory. Returns true if stockout occurs.
let runSingleSimulation (rng: Random) (item: Item) (days: int) =
  // Initialize distribution for this specific item
  // Note: MathNet Normal handles the math.
  let dist = Normal(item.AvgDemand, item.StdDevDemand, rng)
  
  // Recursive loop to simulate days passing
  let rec simulateDay currentStock day =
    if currentStock <= 0.0 then true // Stockout happened
    elif day > days then false       // Survived the duration
    else
      // Sample random demand (ensure it's not negative)
      let demand = Math.Max(0.0, dist.Sample())
      simulateDay (currentStock - demand) (day + 1)

  simulateDay item.CurrentStock 1

/// Orchestrates the Monte Carlo runs for one item
let calculateRisk (config: SimulationConfig) (item: Item) =
  // Chapter 6: Reproducibility via seeded Random Source
  let rng = System.Random(config.RandomSeed) 
  
  let failures = 
    [1 .. config.Iterations]
    |> List.map (fun _ -> runSingleSimulation rng item config.DaysToSimulate)
    |> List.filter (fun failed -> failed)
    |> List.length

  let probability = (float failures) / (float config.Iterations)
  item, probability

// ==========================================
// 4. Execution Pipeline (Chapter 5)
// ==========================================

// Create dummy data file for the script to run standalone
let csvContent = """ItemID,CurrentStock,AvgDailyDemand,StdDevDemand
MED-101,500,12.5,3.0
MED-102,100,5.0,1.5
ERR-999,NULL,10.0,2.0
MED-103,25,2.0,5.0
MED-104,-5,10.0,1.0
MED-105,200,6.0,0.5"""

File.WriteAllText("inventory.csv", csvContent)

printfn "--- Pipeline Started ---"

// Configuration
let config = { DaysToSimulate = 30; Iterations = 1000; RandomSeed = 42 }

// Step A: Load and Parse
let results = 
  File.ReadAllLines("inventory.csv")
  |> Array.skip 1 // Skip Header
  |> Array.map parseRow
  |> Array.toList

// Step B: Partition Successes and Failures (Chapter 8)
let validItems, errors = 
  // Custom partitioner or just simple List.choose logic
  let oks = results |> List.choose (function Ok x -> Some x | _ -> None)
  let errs = results |> List.choose (function Error x -> Some x | _ -> None)
  (oks, errs)

// Report Errors
if not (List.isEmpty errors) then
  printfn "\n[!] Data Quality Issues Found:"
  errors |> List.iter (fun e -> printfn "    - %s" e)

// Step C: Run Simulation (Parallelism is Ch 9, so we do sequential for now)
printfn "\n[...] Running Monte Carlo Simulations (%d runs/item)..." config.Iterations

let riskReport = 
  validItems
  |> List.map (calculateRisk config)
  |> List.sortByDescending snd // Sort by highest risk

// Step D: Final Output
printfn "\n--- STOCKOUT RISK REPORT (Next 30 Days) ---"
printfn "%-10s | %-10s | %s" "ItemID" "Stock" "Risk Prob"
printfn "-----------------------------------------"

riskReport
|> List.iter (fun (item, prob) ->
  // Highlight high risk
  let flag = if prob > 0.20 then "**HIGH RISK**" else ""
  printfn "%-10s | %-10.0f | %.1f%% %s" item.Id item.CurrentStock (prob * 100.0) flag
)