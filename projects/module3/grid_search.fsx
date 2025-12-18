// ==========================================
// 0. Dependencies
// ==========================================
#r "nuget: Plotly.NET, 4.2.0"

open System
open Plotly.NET
open Plotly.NET.LayoutObjects

// ==========================================
// 1. Domain Module
// ==========================================
module Domain =
  
  type Parameters = {
    Pressure: float
    Temperature: float
  }

  type ExperimentResult = {
    Input: Parameters
    Yield: float
    DurationMs: int64
  }

// ==========================================
// 2. Core Simulation Module (The "Model")
// ==========================================
module Simulation =
  open Domain

  // A "fake" expensive scientific calculation
  // Yield peaks when Pressure is around 5.0 and Temp is around 5.0
  let calculateYield (p: float) (t: float) =
    let x = p - 5.0
    let y = t - 5.0
    // Inverted gaussian-like shape + some noise
    let z = 100.0 * Math.Exp(-(x*x + y*y) / 10.0)
    z

  // The Async Workflow (Chapter 9)
  // Wraps the calculation with simulated latency
  let runExperimentAsync (params': Parameters) = async {
    let sw = System.Diagnostics.Stopwatch.StartNew()
    
    // Simulate "heavy compute" (non-blocking sleep)
    do! Async.Sleep 50 
    
    let resultYield = calculateYield params'.Pressure params'.Temperature
    
    sw.Stop()
    
    return {
      Input = params'
      Yield = resultYield
      DurationMs = sw.ElapsedMilliseconds
    }
  }

// ==========================================
// 3. Testing Module
// ==========================================
module Tests =
  open Domain
  open Simulation

  let runSanityCheck () =
    printf "Running Pre-flight Unit Test... "
    // We expect the peak (5,5) to be close to 100.0
    let y = calculateYield 5.0 5.0
    if y > 99.0 && y <= 100.0 then
      printfn "PASS ✅"
    else
      failwithf "FAIL ❌ - Model logic is broken. Expected ~100.0, got %f" y

// ==========================================
// 4. Runner Module
// ==========================================
module Runner =
  open Domain
  open Simulation

  // Generate the Grid
  let generateGrid () =
    [
      for p in 0.0 .. 0.5 .. 10.0 do
        for t in 0.0 .. 0.5 .. 10.0 do
          yield { Pressure = p; Temperature = t }
    ]

  // Execute in Parallel
  let executeBatch (grid: Parameters list) =
    printfn "Scheduling %d experiments..." grid.Length
    
    grid
    // 1. Map inputs to Async computations
    |> List.map runExperimentAsync 
    // 2. Convert list of Asyncs to one Async of list (Parallelism happens here)
    |> Async.Parallel
    // 3. Actually trigger the execution
    |> Async.RunSynchronously

// ==========================================
// 5. Visualization Module
// ==========================================
module Viz =
  open Domain

  let generateReport (results: ExperimentResult array) =
    printfn "Generating HTML Report..."

    // 1. Extract unique sorted labels for Axes
    // We must convert them to STRING because the overload requires seq<string>
    let xLabels = 
      results 
      |> Array.map (fun r -> r.Input.Pressure) 
      |> Array.distinct 
      |> Array.sort

    let yLabels = 
      results 
      |> Array.map (fun r -> r.Input.Temperature) 
      |> Array.distinct 
      |> Array.sort

    // 2. Pivot the data: Transform List -> Matrix (Seq of Seqs)
    // Create a lookup map: (Pressure, Temp) -> Yield
    let yieldMap = 
      results 
      |> Array.map (fun r -> (r.Input.Pressure, r.Input.Temperature), r.Yield) 
      |> Map.ofArray

    // Build the Z-Matrix (Rows = Y/Temp, Cols = X/Pressure)
    let zMatrix = 
      yLabels 
      |> Array.map (fun t -> 
        xLabels 
        |> Array.map (fun p -> 
          match yieldMap.TryFind(p, t) with
          | Some v -> v
          | None -> Double.NaN
        )
      )

    // 3. Create the Chart
    // Explicitly using the parameter names from your signature
    let chart =
      Chart.Heatmap(
        zData = zMatrix,
        colNames = (xLabels |> Array.map string), // Must be strings
        rowNames = (yLabels |> Array.map string), // Must be strings
        ColorScale = StyleParam.Colorscale.Viridis
      )
      |> Chart.withTitle "BioSim-X: Yield Optimization Landscape"
      // Since we converted data to strings, we just label the axes for context
      |> Chart.withXAxisStyle "Pressure (kPa)"
      |> Chart.withYAxisStyle "Temperature (C)"

    // Save to HTML
    let path = "ExperimentReport.html"
    chart
    |> Chart.saveHtml path
    
    printfn "Report saved to: %s" (System.IO.Path.GetFullPath path)

// ==========================================
// Main Execution
// ==========================================
// Open modules to make functions available at top level
open Domain
open Tests
open Runner
open Viz

// 1. Run Tests
runSanityCheck()

// 2. Define Inputs
let parameterGrid = generateGrid()

// 3. Run Parallel Experiments
printfn "Starting Parallel Grid Search..."
let startTotal = System.DateTime.Now
let results = executeBatch parameterGrid
let endTotal = System.DateTime.Now

printfn "Completed %d runs in %O seconds." results.Length (endTotal - startTotal)

// 4. Report
generateReport results
