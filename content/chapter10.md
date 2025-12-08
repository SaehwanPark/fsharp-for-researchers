# Chapter 10 — Visualization and Reporting

## 10.1 Choosing Visualization Tools

In the Python ecosystem, visualization often feels like a fragmented decision tree: "Do I use Matplotlib for control? Seaborn for statistical defaults? Plotly for interactivity? Or Altair for a declarative grammar?"

In F\#, the landscape is more consolidated. While there are several options, the community has largely coalesced around **Plotly.NET** for general research and **ScottPlot** for high-performance desktop applications.

### The Functional Approach to Plotting

Coming from Matplotlib, you are likely used to an **imperative** style:

```python
# Python (Matplotlib)
plt.plot(x, y)
plt.title("Results")
plt.xlabel("Time")
plt.show() # Mutating a global state object
```

F\# treats a chart as an immutable data structure. You create a chart object, pass it through transformation functions to style it, and finally render it. This aligns perfectly with the pipeline (`|>`) operator you learned in Chapter 4.

### The Primary Contender: Plotly.NET

Plotly.NET is a wrapper around the famous Plotly.js engine. It generates HTML/JavaScript charts that are interactive by default (zoom, pan, hover tooltips).

**Why it fits research:**

  * **Interactive:** You can inspect data points by hovering (essential for outlier detection).
  * **Portable:** Charts render as self-contained HTML files you can email to colleagues.
  * **Typed:** You get compile-time safety on styling parameters.

<!-- end list -->

```fsharp
#r "nuget: Plotly.NET, 4.2.0" // Load from NuGet
open Plotly.NET

let xData = [ 1. .. 10. ]
let yData = xData |> List.map (fun x -> x ** 2.0)

// The Pipeline Approach
let myChart =
    Chart.Point(xData, yData)
    |> Chart.withTitle "Squared Values"
    |> Chart.withXAxisStyle "Input (x)"
    |> Chart.withYAxisStyle "Output (x^2)"
    |> Chart.withSize (800., 600.)

// Render in browser
myChart |> Chart.show
```

### The Performance Specialist: ScottPlot

If you are visualizing millions of points in real-time (e.g., signal processing or sensor data), HTML-based plotters like Plotly can lag. **ScottPlot** renders static images (PNG/SVG) or interacts via GUI windows using high-performance backend drawing. It is the F\# equivalent of a highly optimized Matplotlib backend.

-----

## 10.2 Streaming Results into Plots

One of the most frustrating aspects of long-running research code is the "black box" phase: you start a 4-hour simulation and stare at the terminal, hoping the parameters were correct.

In Python, you might print logs or use `tqdm` progress bars. In F\#, we can integrate visualization directly into the loop to monitor convergence.

### Dynamic Visualization

Because F\# charts are just data, we can update them. While full GUI development is outside this scope, we can use a simple pattern to refresh charts during iterative processes.

Below is an example using a conceptual "Live Plot" workflow (common in Notebook environments or using ScottPlot's windowing).

```fsharp
open Plotly.NET

// A function to simulate an evolving loss curve
let runTrainingLoop epochs =
    let rng = System.Random()
    let mutable currentLoss = 10.0
    let history = ResizeArray<float>() // Mutable list for accumulation
    
    for i in 1 .. epochs do
        // Simulate training step
        currentLoss <- currentLoss * 0.95 + (rng.NextDouble() - 0.5) * 0.5
        history.Add(currentLoss)
        
        // Every 10 steps, visualize the state
        if i % 10 = 0 then
            // Clear console and show update
            printfn "Epoch %d: Loss = %.4f" i currentLoss
            
            // In a notebook, this would refresh the cell output.
            // In a script, we might generate a temporary snapshot.
            Chart.Line(history)
            |> Chart.withTitle (sprintf "Live Training: Epoch %d" i)
            |> Chart.withSize(600., 400.)
            |> Chart.show 
            // Note: In a browser, this opens a new tab. 
            // In VS Code Polyglot Notebooks, this updates the output in place.
```

### The "Watch" Pattern

For file-based workflows, a common F\# pattern is to have your script write a `current_results.html` file every few minutes. You simply keep that file open in your browser and refresh it to see the latest state of your simulation, without interrupting the computation.

-----

## 10.3 Producing Notebooks and Reports

You are likely familiar with **Jupyter Notebooks**. The .NET ecosystem has fully embraced this paradigm through **Polyglot Notebooks** (formerly .NET Interactive).

### VS Code + Polyglot Notebooks

Instead of `.ipynb` files running a Python kernel, you can run an F\# kernel. This offers a distinct advantage over standard Jupyter: **IntelliSense**.

Because F\# is statically typed, the notebook knows exactly what columns are in your data and what functions are available *before* you run the cell. This drastically reduces the "run-crash-edit-run" loop common in Python notebooks.

### The "Literate Programming" Workflow

In F\#, we distinguish between **Scripts** (`.fsx`) and **Notebooks** (`.ipynb`).

1.  **Exploration (.ipynb):** Use Polyglot Notebooks for EDA (Exploratory Data Analysis). Combine markdown, LaTeX equations, and F\# code blocks to narrate your discovery process.
2.  **Production (.fs):** Once the logic is solid, copy the code into a compiled `.fs` file for the heavy lifting (Chapter 11).

> **Tip:** You can reference existing F\# scripts inside a notebook using `#load "MyModel.fs"`. This allows you to keep your heavy logic in clean source files while using the notebook solely for visualization and commentary.

-----

## 10.4 Automating Report Generation

The final stage of research is reporting. Manually copying screenshots of charts into Word documents is error-prone and tedious. If the data changes, you have to redo the manual labor.

In F\#, we can treat "The Report" as just another function output.

### HTML as a Universal Format

Since Plotly.NET produces HTML, we can embed these charts directly into a larger HTML report string.

### Building a Reporting Pipeline

Let's define a function that takes our `ExperimentResult` (from Chapter 9) and produces a standalone HTML report.

```fsharp
open Plotly.NET

// Assume we have this type from previous work
type ExperimentSummary = { 
    ExperimentId: string
    Accuracy: float 
    LossHistory: float list 
}

let generateReport (data: ExperimentSummary) =
    // 1. Create the chart
    let lossChart = 
        Chart.Line(data.LossHistory)
        |> Chart.withTitle "Training Convergence"
        |> GenericChart.toChartHTML // Get the raw HTML/JS string

    // 2. Inject into an HTML template
    let htmlContent = sprintf """
    <html>
    <head>
        <title>Experiment Report: %s</title>
        <style>
            body { font-family: sans-serif; padding: 20px; }
            .metric { font-size: 20px; color: #333; }
            .success { color: green; font-weight: bold; }
        </style>
    </head>
    <body>
        <h1>Experiment Result: %s</h1>
        <hr/>
        <p class="metric">Final Accuracy: <span class="success">%.2f%%</span></p>
        
        <h2>Loss Curve</h2>
        <div id="chart-container">
            %s 
        </div>
        
        <p><i>Generated by F# Reporting Pipeline on %s</i></p>
    </body>
    </html>
    """ data.ExperimentId data.ExperimentId (data.Accuracy * 100.0) lossChart (System.DateTime.Now.ToString())

    // 3. Write to disk
    let filename = sprintf "Report_%s.html" data.ExperimentId
    System.IO.File.WriteAllText(filename, htmlContent)
    printfn "Report generated: %s" filename
```

### Why This Matters

By "codifying" the report generation:

1.  **Reproducibility:** The report is always generated exactly the same way.
2.  **Scalability:** You can run 1,000 experiments and generate 1,000 HTML reports automatically.
3.  **Metadata:** You can embed git hashes, random seeds, and timestamps directly into the footer of every visual artifact.

-----

## Recap

  * **Plotly.NET** is the standard for F\# data visualization. It is declarative, type-safe, and generates interactive HTML.
  * **The Pipeline Style:** Building charts in F\# uses the `|>` operator (`Chart.Point |> Chart.withTitle`), avoiding the mutable state confusion of imperative plotting libraries.
  * **Polyglot Notebooks:** Use VS Code with the Polyglot Notebooks extension for a Jupyter-like experience with full compiled language safety.
  * **Automated Reporting:** Don't screenshot plots. Write functions that accept data and return complete HTML reports.

-----

## Exercises

### 1\. Basic Plotting

Create a script that generates a sine wave.

  * Generate X values from 0 to $2\pi$ in 100 steps.
  * Compute Y values as $\sin(x)$.
  * Use Plotly.NET to plot a red line with markers.
  * Label the X-axis "Radians" and the Y-axis "Amplitude".

### 2\. The Dashboard Generator

Write a function `createDashboard` that accepts a list of integers.

  * It should generate **two** charts: a Histogram of the values and a BoxPlot of the values.
  * Combine them using `Chart.Grid` (look up `Chart.combine` or `Chart.Grid` in Plotly.NET documentation) so they appear side-by-side.
  * Show the result.

### 3\. Notebook vs. Script (Conceptual)

**True or False:** In F\# Polyglot Notebooks, variables defined in one code block are accessible in subsequent code blocks, just like in Python Jupyter notebooks, but their types are fixed once defined.