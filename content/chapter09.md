# Chapter 9 — Asynchronous and Parallel Computing

## 9.1 Why Parallelism Matters in Research

Research code often follows a predictable lifecycle: you write a script to simulate a single scenario or process a single file. It works perfectly. Then, the scope changes. Suddenly, you need to run that simulation 10,000 times for a Monte Carlo analysis, or parse 500GB of log files.

In a synchronous (single-threaded) world, your runtime scales linearly: if one run takes 1 second, 10,000 runs take nearly 3 hours. Yet, while you wait, your modern laptop CPU—likely equipped with 8 to 16 cores—sits mostly idle, utilizing only 10-15% of its capacity.

### The Python Concurrency Hurdle

Coming from Python, you may have encountered the **Global Interpreter Lock (GIL)**. The GIL ensures that only one thread executes Python bytecode at a time. To bypass this for CPU-bound tasks, Python developers rely on the `multiprocessing` library. While effective, `multiprocessing` has overhead: it must spin up separate processes and serialize (pickle) data back and forth between them.

### The F\# Advantage

F\# runs on the .NET runtime (CLR), which was designed from the ground up to be multi-threaded.

1.  **No GIL:** Threads can execute code in parallel on different cores within the same process.
2.  **Shared Memory:** Because threads share memory, you don't pay the heavy cost of serializing data to pass it between workers (though you must be careful with *what* you share).
3.  **Immutability:** Remember Chapter 4? The emphasis on immutable data structures wasn't just for code cleanliness. Immutable data is **thread-safe by default**. You can read the same large dataset from 12 threads simultaneously without fear of corruption or complex locking mechanisms.

In this chapter, we will split our focus into two distinct concepts:

  * **Asynchronous Programming (I/O Bound):** Waiting efficiently (e.g., downloading files, querying databases) without blocking the CPU.
  * **Parallel Programming (CPU Bound):** Using multiple cores to calculate results faster (e.g., matrix multiplication, simulations).

-----

## 9.2 `async` and Computation Expressions

In scientific workflows, you often have to wait for the outside world. Maybe you are scraping web data, querying a SQL database, or waiting for a remote API to return model weights.

If you write this synchronously, your program halts entirely while waiting for the network. F\# handles this using the `async { ... }` computation expression.

### The Async Block

The `async` block allows you to write code that *looks* sequential but executes asynchronously. The magic keyword is `let!`.

  * `let`: "Assign this value now."
  * `let!`: "Start this asynchronous operation, release the thread to do other work, and come back here when the result is ready."

<!-- end list -->

```fsharp
open System
open System.IO

// A simulated function mimicking a network request or heavy I/O
let downloadData (url: string) =
    async {
        printfn "Starting download for %s on thread %d..." url Threading.Thread.CurrentThread.ManagedThreadId
        // Simulate a 1-second delay (non-blocking)
        do! Async.Sleep 1000 
        return sprintf "Content of %s" url
    }
```

### Composing Async Workflows

In Python, you might loop through URLs and await them. In F\#, we typically create a list of *pending* tasks and then decide how to run them.

> **Note:** In F\#, an `async` block is a **specification** of work. It does not start running immediately when defined. You must explicitly start it. This is a "cold" task model, unlike the "hot" task model often seen in JavaScript or Python promises.

```fsharp
let urls = ["dataset_A"; "dataset_B"; "dataset_C"]

// 1. Create a list of Async<string> computations (nothing runs yet)
let tasks = 
    urls 
    |> List.map downloadData

// 2. Combine them to run in parallel
let parallelWork = Async.Parallel tasks

// 3. Actually execute the work and wait for the result
// RunSynchronously is typically used only at the "entry point" of your script
let results = 
    parallelWork 
    |> Async.RunSynchronously

printfn "Downloaded: %A" results
```

**Key Takeaway:** If you had run these sequentially, it would take 3 seconds. Using `Async.Parallel`, it takes roughly 1 second, as they wait simultaneously.

### Python vs. F\# Comparison

| Concept | Python (`asyncio`) | F\# (`async`) |
| :--- | :--- | :--- |
| **Defining Scope** | `async def my_func():` | `let myFunc = async { ... }` |
| **Waiting** | `result = await my_func()` | `let! result = myFunc` |
| **Execution** | Starts immediately when called | Starts only when passed to a runner (e.g., `Async.Start`, `RunSynchronously`) |

-----

## 9.3 Parallel Library and Map-Reduce Patterns

While `async` is great for waiting, **Parallel** programming is about crunching numbers. This is where F\# shines in data science.

The .NET standard library includes the **Task Parallel Library (TPL)**, but F\# wraps this in an incredibly convenient module: `Array.Parallel`.

### The `Array.Parallel.map` Powerhouse

This is arguably the most useful function for high-performance data processing in F\#. It has the exact same signature as `List.map` or `Array.map`, but it automatically distributes the work across available CPU cores.

#### Example: Monte Carlo Simulation

Let's imagine a CPU-intensive function that estimates $\pi$ by throwing random darts at a circle.

```fsharp
let runSimulation (iterations: int) =
    let rnd = System.Random() // Note: System.Random is not thread-safe! See section 9.4
    let mutable inside = 0
    for _ in 1 .. iterations do
        let x = rnd.NextDouble()
        let y = rnd.NextDouble()
        if (x*x + y*y) <= 1.0 then
            inside <- inside + 1
    (float inside / float iterations) * 4.0

// We need a thread-safe way to handle randomness (discussed later), 
// but for a quick CPU demo, let's assume a pure calculation.
let heavyCalculation x =
    // Simulate complex math
    let result = Math.Sqrt(float x) * Math.Sin(float x)
    System.Threading.Thread.Sleep(10) // Artificial CPU burn
    result
```

Now, let's process 1,000 inputs.

```fsharp
let inputs = [| 1 .. 1000 |]

// Sequential: Uses 1 core
let serialResults = 
    inputs 
    |> Array.map heavyCalculation

// Parallel: Uses all cores
let parallelResults = 
    inputs 
    |> Array.Parallel.map heavyCalculation
```

If you time these operations, `Array.Parallel.map` will typically provide a speedup proportional to your core count (minus a small overhead for partitioning the work).

### Pitfall: Mutation in Parallel

This is the most common bug researchers encounter when switching to parallel processing.

**Bad Code (Do Not Do This):**

```fsharp
let mutable counter = 0
let inputs = [| 1 .. 1000 |]

inputs 
|> Array.Parallel.iter (fun _ -> 
    // RACE CONDITION!
    // Multiple threads try to read/write 'counter' at the same time.
    counter <- counter + 1 
)

// Counter will likely NOT be 1000. It might be 985 or 992.
```

**The Functional Fix:**
Do not mutate shared state. Return values and aggregate them.

```fsharp
let count = 
    inputs
    |> Array.Parallel.map (fun _ -> 1)
    |> Array.sum
```

### Map-Reduce Pattern

The pattern above is essentially **Map-Reduce**, a cornerstone of big data processing:

1.  **Map:** Transform data in parallel (`Array.Parallel.map`).
2.  **Reduce:** Aggregate the results (`Array.sum`, `Array.max`, or `Array.fold`).

-----

## 9.4 Designing a Parallel Experiment Runner

Let's combine these concepts into a "Production Research" pattern. We want to run an experiment with varying parameters (Hyperparameter Tuning), execute them in parallel, and collect the results safely.

### 1\. Define the Experiment Domain

We use Records to define our inputs and outputs clearly.

```fsharp
type ExperimentParams = {
    Id: int
    LearningRate: float
    Epochs: int
}

type ExperimentResult = {
    Params: ExperimentParams
    Accuracy: float
    DurationSeconds: float
}
```

### 2\. Isolate the Worker Function

This function should be self-contained. If it needs random numbers, it should instantiate its own generator or accept a seed, ensuring thread safety.

```fsharp
let runExperiment (p: ExperimentParams) : ExperimentResult =
    let sw = System.Diagnostics.Stopwatch.StartNew()
    
    // Simulate work based on parameters
    let noise = System.Random(p.Id).NextDouble() * 0.05
    let simulatedAccuracy = 0.8 + (p.LearningRate * 0.1) + noise
    
    // Simulate CPU time
    System.Threading.Thread.Sleep(50) 
    
    sw.Stop()
    
    { Params = p
      Accuracy = simulatedAccuracy
      DurationSeconds = sw.Elapsed.TotalSeconds }
```

### 3\. The Parallel Runner

We generate the parameter grid and execute.

```fsharp
// Generate 100 variations
let parameterGrid = 
    [| for i in 1 .. 100 -> 
        { Id = i; LearningRate = 0.01 * float i; Epochs = 10 } |]

// Execute
printfn "Starting experiments on %d cores..." System.Environment.ProcessorCount

let results =
    parameterGrid
    |> Array.Parallel.map runExperiment

// Aggregation / Analysis
let bestResult = 
    results 
    |> Array.maxBy (fun r -> r.Accuracy)

printfn "Best Accuracy: %.4f with LR: %.2f" 
    bestResult.Accuracy 
    bestResult.Params.LearningRate
```

### Advanced: Throttling Parallelism

Sometimes `Array.Parallel.map` is *too* aggressive. If your experiment consumes 4GB of RAM and you have 16 cores, launching 16 simultaneous experiments might crash your machine (Out Of Memory).

In these cases, we combine `Async` with a degree of parallelism limit.

```fsharp
let throttledRunner maxDegree parallelTasks =
    Async.Parallel(parallelTasks, maxDegreeOfParallelism = maxDegree)

let runSafeExperiments =
    parameterGrid
    |> Array.map (fun p -> async { return runExperiment p }) // Wrap in async
    |> throttledRunner 4 // Only run 4 at a time
    |> Async.RunSynchronously
```

This pattern gives you the best of both worlds: full CPU utilization up to a safe limit you define.

-----

## Recap

  * **Concurrency vs. Parallelism:** Use `async { ... }` for I/O-bound waiting (file/network). Use `Array.Parallel` for CPU-bound calculation.
  * **Cold Execution:** F\# `Async` computations do not start until explicitly triggered (e.g., via `Async.RunSynchronously`).
  * **Immutability is Key:** Parallelism is easy when functions are pure. Avoid mutating global variables inside parallel blocks to prevent race conditions.
  * **Scaling:** `Array.Parallel.map` is often a drop-in replacement for `Array.map` that instantly utilizes all cores.
  * **Throttling:** Use `Async.Parallel` with `maxDegreeOfParallelism` when memory or resource constraints prevent you from using every core at once.

-----

## Exercises

### 1\. Conceptual Check (True/False)

  * **A.** Replacing `List.map` with `List.Parallel.map` works exactly the same way as `Array.Parallel.map`.
  * **B.** Variables defined inside an `async` block are automatically shared between all threads.
  * **C.** You should use `Array.Parallel.map` when downloading 10,000 files from a web server.

### 2\. The Race Condition

The following code attempts to find the total number of even numbers in a large array, but it produces inconsistent results. Rewrite it using a functional, thread-safe approach (hint: map-reduce).

```fsharp
let numbers = [| 1 .. 100000 |]
let mutable evenCount = 0

// The Buggy Version
numbers 
|> Array.Parallel.iter (fun n ->
    if n % 2 = 0 then evenCount <- evenCount + 1
)
```

### 3\. Build a File Processor

Create a script that:

1.  Generates a list of 20 dummy filenames (e.g., "file\_1.txt", "file\_2.txt").
2.  Creates a function `processFile` that takes a filename, sleeps for 500ms (simulating reading), and returns the string "Processed [filename]".
3.  Uses `Async.Parallel` to process all of them.
4.  Print the total time taken. It should be roughly 500ms, not 10 seconds.