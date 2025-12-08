# Chapter 14 — Designing Research Systems That Last

## 14.1 Scientific Software Architecture Patterns

By now, you know *how* to write F\# code. But knowing how to lay bricks doesn't make you an architect. In research, "architecture" often feels like a dirty word—something for enterprise Java developers, not for scientists running simulations.

However, the lack of architecture is exactly why so many research projects rot. You return to a project six months later, run the main script, and it crashes because a file path changed, or a library updated, or you simply forgot that `variable_x` was supposed to be in log-scale.

### The Functional Core and Imperative Shell

The most effective pattern for scientific software is the **Functional Core, Imperative Shell** (also known as Onion Architecture or Hexagonal Architecture).

  * **The Core (Pure Logic):** This contains your math, your physics, and your domain types (`Patient`, `SimulationParams`). It contains **zero** dependencies on the outside world. No file I/O, no database calls, no random number generation (unless seeds are passed in), and no plotting.
  * **The Shell (Orchestration):** This handles the messy reality. It reads CSVs, parses command-line arguments, connects to databases, and saves charts.

#### Why This Matters for Science

1.  **Testability:** You can test your mathematical core in milliseconds because you don't need to load a 10GB dataset to run a unit test.
2.  **Portability:** Today your shell is a Console App. Tomorrow it might be a Web API or a Notebook. The Core doesn't care.

**Bad Architecture (Mixed Concerns):**

```fsharp
// Bad: Math and I/O are tangled
let runSimulation (filePath: string) =
    let lines = System.IO.File.ReadAllLines(filePath) // I/O
    let data = lines |> Array.map float
    let result = data |> Array.map (fun x -> x * 2.0) // Math
    System.IO.File.WriteAllText("out.txt", string result) // I/O
```

**Good Architecture (Separated):**

```fsharp
// The Core (File: Simulation.fs)
module Core =
    let calculate (data: float[]) =
        data |> Array.map (fun x -> x * 2.0)

// The Shell (File: Program.fs)
module Shell =
    let execute inputPath outputPath =
        let lines = System.IO.File.ReadAllLines(inputPath)
        let data = lines |> Array.map float
        // Call the Core
        let result = Core.calculate data 
        System.IO.File.WriteAllText(outputPath, string result)
```

-----

## 14.2 Data Contracts and Provenance

In data science, "Reproducibility" is the gold standard. But reproducibility is impossible if you don't know where your data came from or what shape it's supposed to be in.

### Types as Contracts

In dynamic languages, the contract between two pieces of code is often implicit: *"I promise to pass a dictionary with a 'price' key."*
In F\#, types are explicit contracts. If you change a type definition in `Domain.fs`, the compiler forces you to update every single consumer of that type. This prevents the "silent drift" that breaks Python pipelines.

### Provenance: The Chain of Custody

Research results should answer: **Who** ran this? **When**? Using **what** logic? Based on **which** input?

Designing for provenance means your system outputs shouldn't just be "the answer." They should be "the answer + the context."

**The "Signed Result" Pattern:**
Every significant output from your system should be wrapped in a record that captures its history.

```fsharp
type Provenance = {
    CodeVersion: string  // e.g., Git Commit Hash
    Timestamp: System.DateTime
    User: string
    Parameters: SimulationConfig
}

type SignedResult<'T> = {
    Metadata: Provenance
    Value: 'T
}
```

When you design your systems this way from Day 1, you never have to ask, *"Is this `results_final_v2.csv` the one from the new algorithm or the old one?"* You simply open the file and read the metadata.

-----

## 14.3 When to Use Types and When Not To

A common pitfall for new F\# developers is **"Type Paralysis."** You discover the power of the type system and try to model *everything*.

  * "I need a type for `PositiveInteger`."
  * "I need a type for `NonEmptyString`."
  * "I need a type for `Celsius` and `Fahrenheit`."

While powerful, over-typing can kill the exploratory momentum essential for research.

### Phase 1: Exploration (Low Typing)

When you are just exploring a dataset in a script or notebook, be pragmatic. Use primitive types (`float`, `string`, `int`). Use tuples/anonymous records instead of formal `type` definitions.

  * *Goal:* Speed of iteration.
  * *Tool:* `.fsx` scripts, Tuples, Anonymous Records `{| X = 1; Y = 2 |}`.

### Phase 2: Consolidation (Medium Typing)

Once you find a logic pattern that works and you want to reuse it, formalize the data structures. Introduce Records and Unions.

  * *Goal:* Correctness and readability.
  * *Tool:* Define `type Patient = ...` in a module.

### Phase 3: library/Production (High Typing)

If you are building a library for other people (or your future self) to use, or if the cost of a bug is very high (e.g., clinical data), enforce strict constraints. Use Single-Case Unions (`type UserId = UserId of Guid`) and smart constructors to prevent invalid states.

  * *Goal:* Safety and impossible-to-misuse APIs.
  * *Tool:* Signature files (`.fsi`), access modifiers (`private`), Units of Measure.

**Guideline:** Start loose. Tighten the types only when the code's purpose solidifies.

-----

## 14.4 Guidelines for Building Sustainable Tooling

Research software is often ephemeral. But sometimes, a script evolves into a tool used by the whole lab. Treating it like a software product—even a small one—pays dividends.

### 1\. The "Pit of Success"

Design your APIs so that the *easiest* way to use them is the *correct* way.

  * **Bad:** A function `calc(a, b, c)` where `b` is optional but if it's null the program crashes.
  * **Good:** A function where required parameters are explicit, and optional ones use `Option<T>`.

### 2\. Failure is a Result, Not an Exception

As discussed in Chapter 8, avoid throwing exceptions for expected data issues. If a parser fails, return `Error`. This forces the user of your tool to decide how to handle it, rather than having their overnight simulation crash 90% of the way through because of one bad row.

### 3\. Embed Documentation

You don't need a separate PDF manual. Use XML documentation comments (`///`) above your functions. Modern editors (VS Code, Rider, Visual Studio) will show these tooltips when a user hovers over the function.

```fsharp
/// <summary>
/// Calculates the kinetic energy of a particle.
/// </summary>
/// <param name="mass">Mass in kilograms.</param>
/// <param name="velocity">Velocity in m/s.</param>
/// <returns>Energy in Joules.</returns>
let kineticEnergy mass velocity = 
    0.5 * mass * (velocity ** 2.0)
```

### 4\. Versioning Matters

If you change the behavior of your simulation, bump the version number. If you don't, you risk invalidating previous scientific conclusions without realizing it.

-----

## Recap

  * **Functional Core, Imperative Shell:** Isolate your math from your I/O. This makes your science testable and your code portable.
  * **Provenance:** Data without context is noise. Wrap your results in metadata (timestamp, version, config) to ensure reproducibility.
  * **Pragmatic Typing:** Don't over-engineer early. Move from loose types (tuples/primitives) to strict types (records/unions/units of measure) as the project matures.
  * **Sustainable Tooling:** Write code for humans. Use documentation comments and explicit types to help your colleagues (and future self) understand how to use your tools correctly.

-----

## Exercises

### 1\. Architecture Review

You are reviewing a colleague's code. They have a function `calculateStats` that:

1.  Connects to a database to get data.
2.  Computes the median.
3.  Prints the result to the console.

Identify the architectural flaw based on the "Functional Core, Imperative Shell" principle. How would you refactor it?

### 2\. Designing Provenance

Define an F\# Record type named `ExperimentMeta` that captures the necessary context to make a simulation reproducible. It should include fields for the software version, the user who ran it, and the random seed used.

### 3\. The "Type Fatigue" Check

**Scenario:** You are writing a quick 50-line script to plot a CSV file once.
**Question:** Is it worth defining 5 different Discriminated Unions and Units of Measure for this task? Why or why not?