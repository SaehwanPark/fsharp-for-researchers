# Chapter 8 — Working With Uncertainty

In scientific computing, failure is a data point. A sensor might time out, a row in a CSV might be malformed, or an API request might hang.

In Python, the standard tool for this is the **Exception**. You wrap code in `try...except` blocks. The problem with exceptions is that they are **invisible** in the function signature.
`def load_data(path):` tells you nothing about what happens if the file is corrupt. Does it return `None`? Does it raise `ValueError`? Does it raise `FileNotFoundError`? You have to read the source code (or trust the documentation) to know.

F\# treats errors as **values**. Just as `Option` handles missing data, the `Result` type handles operations that can fail. This forces you to acknowledge failure modes as part of your domain model, not just as annoying interruptions.

-----

## 8.1 Result and Error Types

### The `Result` Type

The `Result<'Success, 'Error>` type has two cases:

1.  `Ok(value)`: The operation succeeded.
2.  `Error(reason)`: The operation failed, containing details about why.

### Exceptions vs. Results

Let's look at calculating the log of a number. Mathematically, $\log(x)$ is undefined for $x \leq 0$.

**Python (Exceptions):**

```python
import math

def safe_log(x):
    if x <= 0:
        raise ValueError("Must be positive")
    return math.log(x)

# The caller doesn't know this can crash just by looking at the name
val = safe_log(-5) # CRASH!
```

**F\# (Result Types):**

```fsharp
let safeLog x =
    if x <= 0.0 then
        Error "Input must be positive"
    else
        Ok (log x)

// The compiler forces you to handle the error
match safeLog -5.0 with
| Ok val -> printfn "Log is %f" val
| Error msg -> printfn "Math failed: %s" msg
```

This changes the mindset from "I hope this works" to "I have a plan for when this doesn't work."

-----

## 8.2 Handling Data Failures Gracefully

When processing a dataset of 10,000 items, you rarely want the whole pipeline to crash just because item \#4,200 is bad. You want to keep the good ones and log the bad ones.

### The "Railway Oriented" Pattern

We can use `Result` to build a "two-track" system: data flows on the Green track (Success) or switches to the Red track (Error).

### Example: Parsing a Batch

Imagine parsing a list of strings into integers.

```fsharp
let rawData = ["10"; "20"; "NotANumber"; "40"]

// 1. Define a safe parser
let parse (s: string) =
    match System.Int32.TryParse s with
    | true, v -> Ok v
    | false, _ -> Error (sprintf "Invalid integer: '%s'" s)

// 2. Map the parser over the list
let results = rawData |> List.map parse
// results: [Ok 10; Ok 20; Error "Invalid integer..."; Ok 40]

// 3. Separate Successes and Failures
let (successes, failures) = 
    results 
    |> List.partition (fun r -> 
        match r with Ok _ -> true | Error _ -> false
    )

// 4. Unwrap the 'Ok' values to get clean ints
let cleanData = 
    successes 
    |> List.map (fun (Ok v) -> v) // Compiler knows this is safe due to partition logic? 
                                  // Actually, a safer way is usually explicit matching or List.choose

// Safer Idiomatic Way:
let validNumbers = 
    results 
    |> List.choose (function 
        | Ok v -> Some v 
        | Error _ -> None)

let errorLogs = 
    results 
    |> List.choose (function 
        | Ok _ -> None 
        | Error msg -> Some msg)
```

Now you have `validNumbers` to feed into your model, and `errorLogs` to save to a "BadData.log" file for review.

-----

## 8.3 Validating Assumptions in Code

Research code is full of assumptions: "Probabilities sum to 1.0", "Mass cannot be negative".
Instead of writing asserts everywhere, we can use **Private Constructors** to create types that *guarantee* these rules.

### The "Smart Constructor" Pattern

```fsharp
module Domain =
    // 1. Define the type, but keep its internals PRIVATE
    // Users can see 'Probability', but cannot write 'Probability 5.0'
    type Probability = private Probability of float

    // 2. Define a module to create it
    module Probability =
        // The ONLY way to create a Probability
        let create p =
            if p >= 0.0 && p <= 1.0 then
                Ok (Probability p)
            else
                Error (sprintf "%f is not a valid probability (0-1)" p)

        // Helper to get the float back out
        let value (Probability p) = p

// Usage
open Domain

let p1 = Probability.create 0.5  // Returns Ok (Probability 0.5)
let p2 = Probability.create 1.5  // Returns Error "1.5 is not valid..."
```

If you have a function `simulate (p: Probability)`, you **know** for a fact that `p` is between 0 and 1. You don't need to check it inside the function. The validation happened at the gate.

-----

## 8.4 Tracing and Logging Scientific Pipelines

Debugging functional pipelines can be tricky because you don't have intermediate variables to inspect in a debugger easily. A common pattern is the **"Tee"** function (named after the plumbing pipe that splits flow).

### The `tee` Function

It performs a side effect (printing, logging, plotting) and passes the data through unchanged.

```fsharp
// Define a helper
let tee f x =
    f x
    x

// Usage in a pipeline
let data = 
    [1..5]
    |> List.map (fun x -> x * 10)
    |> tee (fun xs -> printfn "Step 1 (Scaled): %A" xs) // Peek at data
    |> List.filter (fun x -> x > 20)
    |> tee (fun xs -> printfn "Step 2 (Filtered): %A" xs) // Peek again
    |> List.sum
```

This allows you to "audit" your data transformation steps without breaking the flow or declaring temporary variables.

-----

## 8.5 Building a Resilient Data Ingestion Framework

Let's combine these concepts into a robust ingestor for a hypothetical experiment.
**Scenario:** Read a CSV line. It has a numeric `Value` and a `Category`.
**Rules:**

1.  `Value` must be numeric.
2.  `Value` cannot be negative.
3.  `Category` cannot be empty.

<!-- end list -->

```fsharp
type RawRow = { RawValue: string; RawCategory: string }

type ValidRecord = { Value: float; Category: string }

// Validation Logic
let validate (row: RawRow) : Result<ValidRecord, string> =
    // 1. Check Category
    if System.String.IsNullOrWhiteSpace(row.RawCategory) then
        Error "Missing Category"
    else
        // 2. Check Numeric Parse
        match System.Double.TryParse(row.RawValue) with
        | false, _ -> Error (sprintf "Value '%s' is not a number" row.RawValue)
        | true, v ->
            // 3. Check Domain Rule
            if v < 0.0 then 
                Error (sprintf "Value %.2f is negative (impossible)" v)
            else
                Ok { Value = v; Category = row.RawCategory }

// The Batch Processor
let processBatch (rows: RawRow list) =
    let results = rows |> List.map validate
    
    let validRecords = results |> List.choose (function Ok r -> Some r | _ -> None)
    let errors = results |> List.choose (function Error e -> Some e | _ -> None)
    
    // Return a Summary Report
    {| 
        ProcessedCount = rows.Length
        SuccessCount = validRecords.Length
        Failures = errors 
        CleanData = validRecords
    |}

// Example Run
let batch = [
    { RawValue = "10.5"; RawCategory = "A" }
    { RawValue = "-5.0"; RawCategory = "B" } // Error: Negative
    { RawValue = "High"; RawCategory = "C" } // Error: Not a number
]

let report = processBatch batch

printfn "Successfully loaded %d records." report.SuccessCount
printfn "Errors found: %A" report.Failures
```

This code is **resilient**. It doesn't crash on bad data. It separates concerns (Validation vs. Aggregation) and provides full visibility into what went wrong.

-----

## Recap

1.  **Results vs Exceptions:** Use `Result<'T, 'Error>` for recoverable errors (validation, parsing). Use Exceptions only for catastrophic system failures (out of memory).
2.  **Railway Oriented Programming:** Chain operations that might fail. If one step fails, the error propagates down the "Error track" automatically.
3.  **Smart Constructors:** Use private types to enforce domain invariants (like `Probability` or `PositiveInt`) so valid data is guaranteed by the type system.
4.  **Teeing:** Use a `tee` function to inject logging or visualization into the middle of a pipeline without disrupting the data flow.
5.  **Resilience:** Design pipelines that categorize data into "Success" and "Failure" buckets rather than crashing on the first invalid row.

-----

## Exercises

**1. Result Type Basics (Short Answer)**
Rewrite this Python function signature to an F\# signature using `Result`.
*Python:* `def parse_age(age_str) -> int` (Raises ValueError if negative or not a number)
*F\#:* `val parseAge : string -> ____________`

**2. The Smart Constructor (Multiple Choice)**
You created a private type `TemperatureK` (Kelvin) that must be $\geq 0$. Why is the constructor private?

  * A) To save memory.
  * B) To prevent users from bypassing the validation logic (e.g., creating -50 K).
  * C) Because private types run faster.

**3. Error Aggregation (Applied)**
You have `val results : Result<int, string> list`.
Write a function that returns `true` if *all* items are `Ok`, and `false` if *any* item is `Error`.
*(Hint: `List.forall` or `List.exists`)*
