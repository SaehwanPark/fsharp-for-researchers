# 🧪 Module 2 Mini-Project: The "Monte Carlo" Supply Chain

## Project Brief

**Context:**
You are a supply chain analyst. You have a dataset of pharmaceutical inventory. For each item, you know the current stock level and the historical daily demand (mean and standard deviation).

Your stakeholders want to know: **"Which items are at risk of running out of stock in the next 30 days?"**

Since daily demand varies, you cannot just use the average. You must run a **Monte Carlo Simulation** (1,000 runs per item) to calculate the *probability* of a stockout.

**Your Goal:**
Write an F\# script that ingests inventory parameters, handles dirty data gracefully using `Result` types, runs a probabilistic simulation using `MathNet.Numerics`, and outputs a risk report.

### Input Data (`inventory.csv`)

Create a file named `inventory.csv` with this content (note the bad lines):

```csv
ItemID,CurrentStock,AvgDailyDemand,StdDevDemand
MED-101,500,12.5,3.0
MED-102,100,5.0,1.5
ERR-999,NULL,10.0,2.0
MED-103,25,2.0,5.0
MED-104,-5,10.0,1.0
MED-105,200,6.0,0.5
```

### Requirements & Constraints

1.  **Working with Data (Chapter 5):**
      * Load the CSV manually or using a lightweight helper.
      * Filter out "garbage" data (e.g., negative stock, nulls) but *log* the failures.
2.  **Numerical Workflows (Chapter 6):**
      * Use **MathNet.Numerics** (via NuGet) to generate random numbers from a **Normal Distribution** for the daily demand.
      * Ensure **reproducibility** by initializing your random source with a specific seed.
3.  **Modeling & Algorithms (Chapter 7):**
      * Implement the simulation logic: For each item, simulate 30 days of demand. If stock drops to $\le 0$ at any point, that run counts as a "Stockout."
      * Repeat 1,000 times per item.
4.  **Working with Uncertainty (Chapter 8):**
      * Do **not** use `try/catch` blocks for data validation. Use the **`Result<T, E>`** type pattern to model row parsing.
      * Split your data into `validItems` and `errors` using `List.partition` or similar.

### Output Expectations

1.  A printout of parsing errors (e.g., "Row 3 failed: Invalid Stock").
2.  A table showing only "High Risk" items (where Stockout Probability \> 20%).

### Hints

  * Reference the package at the top of your script: `#r "nuget: MathNet.Numerics"`.
  * A "Stockout" is a binary state (0 or 1) for a single simulation run. The probability is `(Sum of Stockouts) / Total Runs`.
  * Recall that `List.map` + `List.choose` or `List.partition` are powerful ways to separate `Ok` results from `Error` results.

## Solution

[See this](solution.md)