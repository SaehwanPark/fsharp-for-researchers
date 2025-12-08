# 🌉 Module 4 Mini-Project: The "Hybrid" Sentiment Bridge

## Project Brief

**Context:**
You are building a customer feedback system. You have a list of raw user comments. Your team uses a Python function (which acts as a "black box" model) to score the sentiment of these comments (0.0 to 1.0).

**Your Goal:**
Write an F\# script (`.fsx`) that safely interoperates with Python to score text.
Crucially, you must design this using **Dependency Injection**. You will define an interface (abstract type) for the analyzer, allowing you to switch between a "Real Python Bridge" and a "Mock Implementation" (for testing/dev) without changing your core pipeline.

### Input Data

```fsharp
let comments = [
  "The service was excellent and fast."
  "Terrible experience, would not recommend."
  "It was okay, average performance."
  "Abysmal failure, system crashed."
  "Superb! Highly optimized."
]
```

### Requirements & Constraints

1.  **Interoperability (Chapter 13):**
      * Use `Python.NET` (NuGet: `pythonnet`) to initialize the Python Engine within F\#.
      * Define a Python function (as a string literal in F\#) that calculates a simple score (e.g., checks for words like "good" vs "bad"). Execute this using the Python Engine.
      * **Do not** let `PyObject` types leak into your main pipeline.
2.  **Architecture (Chapter 14):**
      * Define a **Domain Type**: `SentimentResult` (Record).
      * Define an **Interface** (Abstract Type or Function Signature) called `ISentimentScorer`.
      * Implement two versions of this interface:
        1.  `PythonScorer`: Calls the actual Python engine.
        2.  `MockScorer`: Returns random/deterministic F\# values (useful when Python isn't installed or for unit testing).
3.  **The Pipeline:**
      * Write a `processBatch` function that takes the `ISentimentScorer` as an argument. This proves your logic is decoupled from the implementation.

### Hints

  * To use Python.NET:
    ```fsharp
    #r "nuget: pythonnet"
    open Python.Runtime
    ```
  * You might need to set `Runtime.PythonDLL` to your local python DLL path if auto-detection fails (e.g., `libpython3.x.so` or `python3x.dll`).
  * The "Anti-Corruption Layer" means converting the dynamic Python result to a specific F\# `float` or `Result` type *immediately* after the call.

## Solution

[See this](solution.md)