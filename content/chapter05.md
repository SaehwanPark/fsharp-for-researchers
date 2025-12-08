# Chapter 5 — Working with Data

In the previous chapters, we built pipelines using standard lists and immutable records. This "List of Records" approach is excellent for complex domain logic where every row is a strictly defined entity.

However, data science often deals with "rectangular" data—CSVs, Parquet files, and spreadsheets—where the schema might be messy, values are missing, and we need to perform bulk operations like "fill all NaNs in column B."

In the Python ecosystem, **Pandas** is the undisputed king. In F\#, we have a powerful duo: **FSharp.Data** for intelligent data access and **Deedle** for DataFrame operations. This chapter transitions you from simple collections to robust tabular data handling.

-----

## 5.1 Loading Structured and Semi-Structured Data

### The "Magic" of Type Providers

In Python, when you do `df = pd.read_csv("data.csv")`, you don't know if the column "Price" exists or if it is numeric until you run the code. If you typo the column name as `df["price"]`, the script crashes at runtime.

F\# offers a unique feature called **Type Providers**. A Type Provider looks at your data source (a CSV file, a JSON snippet, or a SQL database) *while you are typing* and generates types for you in the background.

### Using the CSV Provider

To use this, we need the `FSharp.Data` package.

```bash
dotnet add package FSharp.Data
```

Imagine a file `stock_prices.csv`:

```csv
Date,Open,Close,Volume
2024-01-01,150.0,152.5,10000
2024-01-02,152.5,151.0,8500
```

In F\#, we define a type representing this file structure:

```fsharp
open FSharp.Data

// The compiler reads the file sample to understand the schema
type Stocks = CsvProvider<"stock_prices.csv">

let data = Stocks.Load("stock_prices.csv")

// ACCESS WITH INTELLISENSE!
// As you type 'row.', the editor suggests 'Open', 'Close', 'Volume'.
// It knows 'Volume' is an int and 'Open' is a decimal/float.
for row in data.Rows do
    printfn "Date: %A, Volume: %d" row.Date row.Volume
```

If the CSV column name changes in the file, your code stops compiling immediately. This tight feedback loop eliminates the "schema drift" bugs common in Python scripts.

-----

## 5.2 DataFrames and Typed Collections

While Type Providers are great for loading data, sometimes you need the power of a **DataFrame** for slicing, dicing, and statistical alignment. The standard library for this in F\# is **Deedle**.

### Installing Deedle

```bash
dotnet add package Deedle
```

### Deedle vs. Pandas

Deedle works similarly to Pandas but enforces types where possible.

```fsharp
open Deedle

// Load a CSV into a Frame
let df = Frame.ReadCsv("stock_prices.csv")

// Slicing Columns
// In Python: df["Close"]
// In Deedle:
let closePrices: Series<int, float> = df?Close
```

### Series and Indexes

Deedle is built on the **Series**. A `Series<K, V>` maps keys (index) to values.

  * **Python:** Index is implicit or secondary.
  * **F\# (Deedle):** You usually explicitly set the index to perform time-series alignment.

<!-- end list -->

```fsharp
// Set the 'Date' column as the index
let timeSeries = 
    df 
    |> Frame.indexRowsDate "Date"

// Now we can do time-aware lookups
let specificDay = timeSeries.Rows.[DateTime(2024, 01, 01)]
```

-----

## 5.3 Aggregation and Grouped Operations

The "Split-Apply-Combine" strategy is central to data analysis. We can do this with raw Lists (from Chapter 4) or with Deedle Frames.

### Approach A: List.groupBy (Native F\#)

Best for complex custom logic.

```fsharp
type Transaction = { Region: string; Amount: float }
let transactions = [ 
    { Region = "US"; Amount = 100.0 }
    { Region = "EU"; Amount = 50.0 }
    { Region = "US"; Amount = 75.0 } 
]

let summary =
    transactions
    |> List.groupBy (fun t -> t.Region)
    |> List.map (fun (region, txs) -> 
        let total = txs |> List.sumBy (fun t -> t.Amount)
        region, total
    )
// Result: [("US", 175.0); ("EU", 50.0)]
```

### Approach B: Deedle Aggregation

Best for statistical speed and handling missing data.

```fsharp
// Assuming 'df' has "Region" and "Amount" columns
let stats = 
    df
    |> Frame.groupRowsByString "Region"
    |> Frame.getCol "Amount"
    |> Stats.sum
```

Deedle handles `NaN` values automatically during aggregation, whereas standard `List` operations require you to filter them out explicitly (as seen in Chapter 4).

-----

## 5.4 Joining, Merging, and Relational Thinking

Merging data is where types save the day. In Pandas, merging on a column with slightly mismatched types (e.g., string "123" vs integer `123`) results in an empty dataframe or weird duplicates.

### Typed Joins

F\# strongly encourages explicit keys.

```fsharp
// Dataset A: Users
type User = { UserId: int; Name: string }
let users = [{ UserId = 1; Name = "Alice" }; { UserId = 2; Name = "Bob" }]

// Dataset B: Orders
type Order = { OrderId: int; UserId: int; Amount: float }
let orders = [{ OrderId = 101; UserId = 1; Amount = 50.0 }]

// Doing a Join
let joined = 
    query {
        for u in users do
        join o in orders on (u.UserId = o.UserId)
        select (u.Name, o.Amount)
    }
    |> Seq.toList
```

F\# supports **LINQ** (Language Integrated Query) via the `query` keyword, which looks like SQL. This allows for declarative joins that are readable and type-checked. If you try to join an integer `UserId` with a string `UserId`, it won't compile.

-----

## 5.5 Running a Mild EDA in F\#

Let’s put it all together. We will load a CSV, infer its schema, print summary stats, and find a correlation.

**Scenario:** We have `weather.csv` with `Temp` and `Humidity`.

```fsharp
open FSharp.Data
open Deedle

// 1. Define the Schema (Type Provider)
type Weather = CsvProvider<"weather.csv">

[<EntryPoint>]
let main argv =
    // 2. Load Data
    let df = Frame.ReadCsv("weather.csv")

    // 3. Quick Stats (Describe)
    printfn "--- Summary Statistics ---"
    df 
    |> Stats.levelMean 
    |> Series.mapValues (printfn "Mean: %.2f") 
    |> ignore

    // 4. Clean Data (Drop missing)
    let cleanDf = df |> Frame.dropSparseRows

    // 5. Calculate Correlation
    let temp = cleanDf?Temp : Series<int, float>
    let humidity = cleanDf?Humidity : Series<int, float>
    
    let corr = Stats.corr temp humidity
    printfn "Correlation (Temp vs Humidity): %.4f" corr

    0 // Exit code
```

### Contrast with Python

In a Python script, you would print `df.describe()`. In F\# with Deedle, we have to be slightly more explicit about *which* stats we want, but the resulting code is compiled into a binary that can be deployed anywhere without worrying if the target machine has the right version of `numpy` installed.

-----

## Recap

1.  **Type Providers:** `FSharp.Data` allows you to treat CSV/JSON files as strongly typed objects, catching schema errors while you write code.
2.  **Deedle:** The F\# answer to Pandas. It provides Series and DataFrames for statistical operations and time-series alignment.
3.  **Grouping:** You can use native `List.groupBy` for domain object logic or Deedle's grouping for statistical aggregations.
4.  **Query Syntax:** F\# supports SQL-like syntax (`query { ... }`) for performing readable, type-safe joins between collections.
5.  **Access:** Dynamic operator `?` in Deedle allows flexible column access similar to Python, while still maintaining type safety boundaries.

-----

## Exercises

**1. Type Provider Safety (True/False)**
If you delete a column from your CSV file but forget to update your F\# code that references it, what happens when you try to run/compile the project?

  * A) It runs but returns `null` for that column.
  * B) It fails to compile with an error saying the field is missing.
  * C) It throws a runtime exception when the file is loaded.

**2. Deedle Indexing (Multiple Choice)**
In Deedle, a `Series` is best described as:

  * A) A standard generic List.
  * B) A key-value mapping (Index -\> Value) capable of handling missing data.
  * C) A mutable array of floating point numbers.

**3. The Grouping Pipeline (Applied)**
Given a list of records: `[{ Team="A"; Score=10 }; { Team="B"; Score=5 }; { Team="A"; Score=20 }]`.
Write a short F\# pipeline using `List.groupBy` to compute the total score per team.
*(Hint: You will need `List.map` and `List.sumBy` after grouping.)*
