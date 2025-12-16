# Chapter 12 — End-to-End Case Study

## 12.1 Requirements and Dataset Setup

We have arrived at the capstone. Until now, we have examined F\# features in isolation—types, pipelines, parallelism, and testing. In this chapter, we will assemble these pieces into a cohesive, production-grade research application.

### The Research Question

Imagine we are analyzing data from a multi-site clinical trial. Our goal is to determine if a new treatment ("Drug A") is more effective than a placebo.

**The Input:**
We receive raw CSV files from different hospital sites. The data is messy: some fields are missing, and some values are malformed.

  * Format: `PatientID, Group, PreScore, PostScore`
  * Groups: "Control", "Treatment" (but sometimes misspelled like "treatmnt" or "ctrl").

**The Requirements:**

1.  **Robust Ingestion:** Load data from multiple files without crashing on a single bad row.
2.  **Validation:** Discard invalid data but *log* exactly what was discarded and why.
3.  **Analysis:** Calculate the average improvement (`PostScore - PreScore`) for each group and determine the improvement delta.
4.  **Reproducibility:** Save a metadata file recording exactly when the run happened and what logic version was used.

### The Project Structure

We will adopt the structure recommended in Chapter 11:

```text
/ClinicalTrialApp
  /src
     /Domain.fs       <-- Types and Business Logic
     /Ingestion.fs    <-- Parsing and Cleaning
     /Analysis.fs     <-- Statistics
     /Program.fs      <-- The Entry Point (Wiring it all together)
```

-----

## 12.2 Full Pipeline Implementation

We will build this layer by layer, starting with the Domain. This is "Thinking in Types" (Chapter 3) applied to a full system.

### Step 1: The Domain Model (`Domain.fs`)

We define what valid data looks like. Note that we don't use strings for the Group; we use a Discriminated Union to enforce correctness.

```fsharp
namespace ClinicalTrial

open System

// The Group must be one of these two. No "strings" allowed in the core logic.
type Group = 
    | Control 
    | Treatment

// A valid, clean patient record
type Patient = {
    Id: string
    Group: Group
    PreScore: float
    PostScore: float
}

// Derived metric
module Patient =
    let improvement p = p.PostScore - p.PreScore
```

### Step 2: Safe Ingestion (`Ingestion.fs`)

We need to move from the "messy string world" to our nice "typed domain world." We will use the `Result` type (Chapter 8) to handle failures gracefully.

```fsharp
namespace ClinicalTrial

module Ingestion =
    open System
    open System.IO

    // Helper to parse the Group column safely
    let parseGroup (s: string) =
        match s.ToLower().Trim() with
        | "control" | "ctrl" -> Ok Control
        | "treatment" | "treat" | "drug_a" -> Ok Treatment
        | _ -> Error (sprintf "Unknown group: '%s'" s)

    // Parse a single CSV line
    let parseLine (line: string) =
        try
            let parts = line.Split(',')
            if parts.Length < 4 then Error "Insufficient columns"
            else
                // Applicative style or nested matching could be used here.
                // For simplicity, we use a basic workflow.
                match parseGroup parts.[1], Double.TryParse parts.[2], Double.TryParse parts.[3] with
                | Ok g, (true, pre), (true, post) ->
                    Ok { Id = parts.[0]; Group = g; PreScore = pre; PostScore = post }
                | Error e, _, _ -> Error e
                | _, _, _ -> Error "Invalid numeric scores"
        with
        | ex -> Error ex.Message

    // The main loader: Returns a tuple of (Good Data, Bad Data Logs)
    let loadFromFiles (files: string list) =
        files
        |> List.collect (fun f -> File.ReadAllLines(f) |> Array.toList |> List.skip 1) // Skip headers
        |> List.map parseLine
        |> List.partition (function -> 
            | Ok _ -> true
            | Error _ -> false
        )
        |> fun (oks, errors) ->
            let validPatients = oks |> List.map (fun (Ok p) -> p)
            let errorLogs = errors |> List.map (fun (Error e) -> e)
            (validPatients, errorLogs)
```

### Step 3: Analysis Logic (`Analysis.fs`)

Now that we have a list of `Patient` objects, the math is trivial and safe. We don't need to check for nulls or bad strings here; the type system guarantees the data is valid.

```fsharp
namespace ClinicalTrial

type AnalysisResult = {
    ControlMean: float
    TreatmentMean: float
    ImprovementDelta: float
    SampleCount: int
}

module Analysis =
    let computeStats (patients: Patient list) =
        let controlGroup = patients |> List.filter (fun p -> p.Group = Control)
        let treatmentGroup = patients |> List.filter (fun p -> p.Group = Treatment)

        let meanImprovement group =
            if List.isEmpty group then 0.0
            else group |> List.averageBy Patient.improvement

        let cMean = meanImprovement controlGroup
        let tMean = meanImprovement treatmentGroup

        {
            ControlMean = cMean
            TreatmentMean = tMean
            ImprovementDelta = tMean - cMean
            SampleCount = patients.Length
        }
```

-----

## 12.3 Experiment Logging and Metadata

In production research, the answer "42" is useless if you don't know *how* you got it. We need to attach metadata to our results.

### Structured Metadata

We define a type that captures the context of the run.

```fsharp
type RunMetadata = {
    RunId: Guid
    Timestamp: DateTime
    User: string
    InputFiles: string list
    LogicVersion: string // e.g., "v1.2-beta"
}
```

### The Wiring (`Program.fs`)

This is the entry point where we compose the pipeline, handle logging, and save artifacts.

```fsharp
open ClinicalTrial
open System
open System.IO
open System.Text.Json // Standard JSON library

[<EntryPoint>]
let main argv =
    // 1. Setup Context
    let runId = Guid.NewGuid()
    let inputs = ["./data/site1.csv"; "./data/site2.csv"] // In real app, from argv
    printfn "Starting Run %O..." runId

    // 2. Execute Pipeline
    let validData, parseErrors = Ingestion.loadFromFiles inputs
    
    printfn "Loaded %d valid records." validData.Length
    printfn "Skipped %d bad rows." parseErrors.Length
    
    // Save error log for debugging
    if not (List.isEmpty parseErrors) then
        File.WriteAllLines(sprintf "run_%O_errors.log" runId, parseErrors)

    // 3. Run Analysis
    let result = Analysis.computeStats validData

    // 4. Create Metadata
    let metadata = {
        RunId = runId
        Timestamp = DateTime.UtcNow
        User = Environment.UserName
        InputFiles = inputs
        LogicVersion = "1.0.0"
    }

    // 5. Output Results (JSON)
    let output = {| Metadata = metadata; Results = result |}
    let json = JsonSerializer.Serialize(output, JsonSerializerOptions(WriteIndented = true))
    File.WriteAllText(sprintf "run_%O_results.json" runId, json)

    printfn "Success! Delta: %.4f" result.ImprovementDelta
    0 // Exit code
```

**Why this is better than a script:**

  * **Separation of Concerns:** `Analysis.fs` knows nothing about CSVs. `Ingestion.fs` knows nothing about means.
  * **Traceability:** Every run produces a unique JSON artifact. You can look back 6 months later and see exactly which files were processed.
  * **Safety:** The parser acts as a firewall. Bad data cannot crash the math logic.

-----

## 12.4 Extensions and Variants

A common problem in research code is "Scope Creep." Suddenly, you need to handle a third group, or different input formats.

### Extending the Domain

Because we used Discriminated Unions, extending the code is guided by the compiler.

If we add a new group:

```fsharp
type Group = 
    | Control 
    | Treatment
    | Placebo2 // New variant
```

The compiler will immediately issue warnings in `Analysis.fs` (match cases are incomplete) and `Ingestion.fs` (parser doesn't handle the string). This "compiler-driven refactoring" ensures you don't forget to update a critical part of your pipeline.

### Parameterizing the Analysis

If we want to run different types of statistical tests, we can represent the *choice of algorithm* as a type.

```fsharp
type StatMethod =
    | MeanDifference
    | MedianDifference
    | TTest of ConfidenceLevel: float // e.g., 0.95

let runAnalysis method patients =
    match method with
    | MeanDifference -> ...
    | TTest conf -> ...
```

This pattern allows you to build a sophisticated experiment runner where the configuration file (parsed into these types) determines the entire behavior of the engine.

-----

## Recap

  * **The Domain Gatekeeper:** Use a dedicated ingestion layer to validate data immediately. Never let "dirty" strings flow deep into your calculation functions.
  * **The Result Pattern:** Use `Result<Success, Failure>` to collect and log errors without crashing the entire batch process.
  * **Metadata is Mandatory:** Always save context (Run ID, Timestamp, Input Params) alongside your numerical results.
  * **Compiler-Assisted Evolution:** Discriminated Unions make it safe to extend your research logic (e.g., adding new experimental groups) by forcing you to handle the new cases everywhere.

-----

## Exercises

### 1\. The JSON Logger

Modify the `Program.fs` example to accept a command-line argument for the output directory.

  * If the directory doesn't exist, create it.
  * Save the JSON result file inside that directory.

### 2\. Handling Outliers

Modify `Ingestion.fs` to add a validation rule:

  * If `PreScore` or `PostScore` is outside the range [0.0, 100.0], return an `Error "Score out of range"`.
  * Run the pipeline with a file containing a score of `999`. Ensure it gets logged to the error file and not included in the average.

### 3\. Architecture Quiz

**Multiple Choice:** In the architecture shown above, if you needed to swap the input format from CSV to JSON, which file(s) would you need to modify?

  * A. Only `Domain.fs`
  * B. Only `Ingestion.fs`
  * C. `Ingestion.fs` and `Analysis.fs`
  * D. Every file in the project
