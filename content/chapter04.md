# Chapter 4 — From Imperative Scripts to Functional Pipelines

If you are coming from Python, your "muscle memory" for solving problems likely involves loops. To process a dataset, you create an empty list, write a `for` loop, add some `if` statements, and append results to the list. This is called **imperative programming**: you are giving the computer step-by-step orders on *how* to modify state.

F\# encourages **declarative programming**. Instead of managing the loop mechanics and temporary lists yourself, you describe *what* the data transformation looks like.

This chapter introduces the "Pipeline"—the architectural backbone of almost every F\# data workflow. We will move from "statements that change things" to "expressions that yield values."

-----

## 4.1 Expressions Over Statements

### Everything Returns a Value

In Python, there is a distinction between a **statement** (which does something, like `x = 5` or `if x:`) and an **expression** (which evaluates to a value, like `2 + 2`).

In F\#, almost everything is an expression.

### The `if` Expression

Consider setting a variable based on a condition.

**Python (Imperative Statement):**

```python
# You must declare 'status' first or rely on side effects
status = "" 
if value > 0.5:
    status = "High"
else:
    status = "Low"
```

**F\# (Expression):**

```fsharp
// The 'if' block itself RETURNS the string
let status = 
    if value > 0.5 then "High" 
    else "Low"
```

Because `if` returns a value, you are forced to handle the `else` branch. You cannot have a "dangling if" that returns a string sometimes and nothing other times (unless you return an `Option`, as learned in Chapter 3).

### Why This Matters for Science

This reduces "state drift." In the Python example, `status` exists in a partially initialized state before the logic runs. In F\#, `status` comes into existence only when the logic is complete and valid.

-----

## 4.2 The Pipe Operator as a Workflow Backbone

The symbol that defines F\# programming is the pipe: `|>`.

### The Problem: Inside-Out Code

Without pipes, applying multiple functions to data results in "nested" code that reads backwards. Imagine scaling a number, taking the log, and then rounding it.

**The "Math" Way (Reading Right-to-Left):**

```fsharp
let result = round(log(scale(10.0)))
```

To understand this, your brain has to find the innermost parenthesis (`10.0`), apply `scale`, then jump out to `log`, then `round`.

### The "Pipeline" Way (Reading Left-to-Right)

The `|>` operator simply takes the value on the left and passes it as the *last argument* to the function on the right.

```fsharp
let result = 
    10.0
    |> scale
    |> log
    |> round
```

This matches how we think about data processing: **Start with Data → Step 1 → Step 2 → Result.**

-----

## 4.3 Transformations: Map, Filter, Reduce

In scientific scripts, 90% of loops serve three purposes:

1.  **Transforming** items (e.g., C to F conversion).
2.  **Selecting** items (e.g., keep only valid samples).
3.  **Aggregating** items (e.g., sum or average).

In F\#, we replace explicit loops with **Higher-Order Functions** from the `List` module.

### Map (Transform)

Equivalent to Python's list comprehension `[f(x) for x in list]`.

```fsharp
let rawReadings = [1; 2; 3; 4]

// Python: [x * 10 for x in rawReadings]
let calibrated = 
    rawReadings 
    |> List.map (fun x -> x * 10)
// Result: [10; 20; 30; 40]
```

### Filter (Select)

Equivalent to `[x for x in list if condition]`.

```fsharp
// Python: [x for x in calibrated if x > 25]
let highValues = 
    calibrated 
    |> List.filter (fun x -> x > 25)
// Result: [30; 40]
```

### Reduce / Fold (Aggregate)

Equivalent to `sum()` or accumulators.

```fsharp
// Summing the list
let total = 
    highValues 
    |> List.sum
```

### Chaining It All Together

The power comes from composition. We can chain these operations without creating intermediate variable names (like `calibrated` or `highValues`) that pollute the global namespace.

```fsharp
let totalHighValue = 
    [1; 2; 3; 4]
    |> List.map (fun x -> x * 10)
    |> List.filter (fun x -> x > 25)
    |> List.sum
```

-----

## 4.4 Immutability and Predictable Behavior

You might ask: *Does `List.map` modify the original list?*

**No.** In F\#, standard lists are **immutable**.
When you run `rawReadings |> List.map ...`, F\# creates a *new* list. `rawReadings` remains exactly as it was.

### Why Not Mutate?

In complex research pipelines, mutation is a major source of bugs.

**The Mutable Trap (Python):**

```python
def clean_data(df):
    # Modifies df in place without warning!
    df.fillna(0, inplace=True) 

original_data = load_csv(...)
clean_data(original_data)
# original_data is now changed forever. 
# You cannot re-run the analysis with different parameters 
# unless you reload the file from disk.
```

**The Immutable Safety (F\#):**

```fsharp
let cleanData data =
    data |> List.map (fun x -> if x < 0 then 0 else x)

let originalData = [10; -5; 20]
let cleaned = cleanData originalData

// originalData is STILL [10; -5; 20]
// cleaned is [10; 0; 20]
```

This guarantees that you can reuse `originalData` for a different experiment in the same script without fear that a previous step "poisoned" it.

-----

## 4.5 Building the First Data Cleaning Pipeline

Let's combine Chapter 3 (Records/DUs) and Chapter 4 (Pipelines) to build a robust data cleaner.

### Scenario

We have a list of raw sensor log entries (strings).

1.  Parse the string to a float.
2.  Filter out failures.
3.  Convert units (Celsius to Kelvin).
4.  Filter out physical outliers (e.g., negative Kelvin is impossible).

<!-- end list -->

```fsharp
// 1. Domain Modeling
type Reading = { Value: float; Unit: string }

// 2. Helper Functions
let parse (input: string) =
    match System.Double.TryParse input with
    | true, v -> Some v
    | false, _ -> None

let toKelvin celsius = celsius + 273.15

let isPhysicallyPossible kelvin = kelvin >= 0.0

// 3. The Pipeline
let rawLogs = ["23.5"; "24.1"; "ERROR"; "-300.0"; "22.0"]

let cleanData =
    rawLogs
    // Step A: Parse strings -> Option<float>
    |> List.map parse
    
    // Step B: Remove parse failures (Keep only Some)
    |> List.choose id 
    
    // Step C: Convert C -> K
    |> List.map toKelvin
    
    // Step D: Validate Physics (Filter out < 0 K)
    |> List.filter isPhysicallyPossible
    
    // Step E: Wrap in Record
    |> List.map (fun k -> { Value = k; Unit = "K" })

// Result: List of Reading records. 
// "ERROR" is gone. "-300.0" (which becomes -26.85 K) is gone.
```

This code reads like a recipe. It handles errors gracefully (via `choose` and parsing), protects domain rules (via `filter`), and creates a clean output object.

-----

## Recap

  * **Expressions, Not Statements:** In F\#, control structures like `if` return values, promoting cleaner data flow.
  * **The Pipe (`|>`):** Passes data forward. It untangles nested function calls into linear pipelines.
  * **Map/Filter/Reduce:** The holy trinity of functional data processing. They replace explicit `for` loops.
  * **Immutability:** Transformations create new data rather than modifying old data. This makes scientific workflows reproducible and easier to debug.
  * **Composability:** Small, simple functions can be chained together to build complex data processing engines.

-----

## Exercises

**1. Expression Thinking (True/False)**
In F\#, the following code is valid and will compile:
`let x = if 10 > 5 then "Big"`
*(Hint: What about the `else` case?)*

**2. The Pipeline Refactor (Intermediate)**
Rewrite this nested code using the pipe operator `|>`:

```fsharp
let result = List.sum(List.filter (fun x -> x > 0) (List.map (fun x -> x * 2) data))
```

**3. Data Cleaning (Applied)**
You have a list of integers: `[-5; 10; 0; 25; -3]`.
Write a pipeline that:

1.  Removes all zeros.
2.  Converts negative numbers to positive (absolute value).
3.  Multiplies everything by 2.
    *(Expected Output: `[10; 20; 50; 6]`)*