# Chapter 7 — Modeling and Algorithms

In the previous chapters, we relied on existing libraries like `MathNet.Numerics` and `Deedle` to do the heavy lifting. But research often operates at the bleeding edge where off-the-shelf libraries don't yet exist. You have to write the algorithm yourself.

In Python, writing a custom algorithm usually involves a `while` loop, several mutable variables (counters, accumulators), and a lot of `index += 1`. In F\#, we approach algorithms differently: we use **recursion** for iteration and **types** to encode the logic itself.

This chapter is about "Algorithmic Design": how to structure complex logic so that it is readable, correct, and performant.

-----

## 7.1 Algorithmic Thinking with Functions

### The End of the Stack Overflow

If you try to write a recursive function in Python that runs 10,000 times, it will crash with `RecursionError`. Python is not designed for recursion; it prefers loops.

F\# (and most functional languages) relies on **Tail Call Optimization (TCO)**. If the recursive call is the *very last thing* a function does, the compiler rewrites it into a loop behind the scenes. This allows you to write elegant recursive logic without blowing up the memory.

### Recursive Lists vs. Loops

Let’s implement a custom algorithm: "Run Length Encoding" (compressing consecutive duplicates: `["a"; "a"; "b"]` → `[("a", 2); ("b", 1)]`).

**Python (Imperative):**

```python
def run_length(data):
    if not data: return []
    result = []
    current_val = data[0]
    count = 1
    for i in range(1, len(data)):
        if data[i] == current_val:
            count += 1
        else:
            result.append((current_val, count))
            current_val = data[i]
            count = 1
    result.append((current_val, count))
    return result
```

**F\# (Recursive):**
We think in terms of "Head" (first element) and "Tail" (rest of the list).

```fsharp
let runLengthEncoding list =
    // Inner helper function to carry state
    // 'acc' is the accumulator (result so far)
    let rec loop current count remaining acc =
        match remaining with
        | [] -> 
            // End of list: add the final group and reverse result
            (current, count) :: acc |> List.rev
        
        | head :: tail ->
            if head = current then
                // Same value: Keep recursing, increment count
                loop current (count + 1) tail acc
            else
                // New value: Save previous group, start new count
                loop head 1 tail ((current, count) :: acc)

    match list with
    | [] -> []
    | head :: tail -> loop head 1 tail []
```

While the F\# code looks verbose initially, it avoids mutable indices (`i`) and explicit bounds checking (`len(data)`). The pattern matching handles the "empty list" and "next item" logic safely.

-----

## 7.2 Modeling Choices Using DU Variants

Scientific algorithms often have "variants." For example, a clustering algorithm might use *Euclidean* distance or *Manhattan* distance.

In Python, you typically pass a string string flag:

```python
def cluster(data, method="euclidean"):
    if method == "euclidean": ...
    elif method == "manhattan": ...
    else: raise ValueError("Unknown method")
```

This is fragile. A typo (`"Euclidean"`) crashes the program at runtime.

In F\#, we model these variants as **Discriminated Unions**. This turns "configuration" into a type-safe contract.

```fsharp
type DistanceMetric =
    | Euclidean
    | Manhattan
    | Minkowski of p: float // Parameterized variant!

let calculateDistance metric (p1: float[]) (p2: float[]) =
    match metric with
    | Euclidean ->
        Array.map2 (fun x y -> (x - y) ** 2.0) p1 p2 
        |> Array.sum |> sqrt
    | Manhattan ->
        Array.map2 (fun x y -> abs (x - y)) p1 p2 
        |> Array.sum
    | Minkowski p ->
        Array.map2 (fun x y -> (abs (x - y)) ** p) p1 p2 
        |> Array.sum 
        |> (fun s -> s ** (1.0 / p))

// Usage
let d = calculateDistance (Minkowski 3.0) point1 point2
```

This approach allows different variants to carry their own specific parameters (like `p` for Minkowski), something impossible with simple string flags.

-----

## 7.3 Implementing Logistic Regression (Conceptual Version)

Let's build a tiny machine learning trainer to see how functional state updates work. We won't use a class with `self.weights`. Instead, we will define a `train` function that takes old weights and returns new weights.

### 1\. The Model

```fsharp
open MathNet.Numerics.LinearAlgebra

type Model = {
    Weights: Vector<float>
    Bias: float
}

type HyperParams = {
    LearningRate: float
    Epochs: int
}
```

### 2\. The Predict Function (Pure)

```fsharp
let sigmoid x = 1.0 / (1.0 + exp(-x))

let predict (model: Model) (features: Vector<float>) =
    let z = (model.Weights * features) + model.Bias
    sigmoid z
```

### 3\. The Update Step

This corresponds to one iteration of Gradient Descent.

```fsharp
let updateStep (params_: HyperParams) (model: Model) (x: Vector<float>) (y: float) =
    let y_pred = predict model x
    let error = y_pred - y
    
    // Gradients
    let dw = x * error
    let db = error
    
    // Return NEW model (Immutable update)
    { 
        Weights = model.Weights - (dw * params_.LearningRate)
        Bias = model.Bias - (db * params_.LearningRate) 
    }
```

### 4\. The Training Loop (Fold)

We use `Seq.fold` (similar to `reduce` in Python) to iterate through the dataset and evolve the model.

```fsharp
let train (params_: HyperParams) (initialModel: Model) (dataset: (Vector<float> * float) seq) =
    // Run for N epochs
    let finalModel = 
        [1 .. params_.Epochs]
        |> List.fold (fun currentModel epoch ->
            // Inside each epoch, fold over the dataset
            dataset 
            |> Seq.fold (fun m (x, y) -> updateStep params_ m x y) currentModel
        ) initialModel
        
    finalModel
```

Notice there are **no loops** and **no mutation**. We fold the `updateStep` function over the data, accumulating the state of the model.

-----

## 7.4 Prototype → Structured → Engineered Progression

Research code evolves. F\# supports this evolution better than scripting languages because types provide a scaffold for refactoring.

### Phase 1: The Prototype (Script)

Everything is in `Experiment.fsx`. Hardcoded values.

```fsharp
let w = vector [0.5; 0.5]
let lr = 0.01
// ... messy calculation ...
```

### Phase 2: Structured (Types)

We introduce Records for configuration and DUs for choices (as seen in 7.2). Code moves to `Model.fs`.

```fsharp
type Config = { LR: float; ... }
let train config data = ...
```

### Phase 3: Engineered (Modules & Visibility)

We hide internal implementation details. We add explicit validation.

```fsharp
module LogisticRegression =
    
    // Opaque type: Users can't manually mess with weights
    type TrainedModel = private { W: Vector<float>; B: float }

    // Public API
    let train (config: Config) (data: Data) : TrainedModel =
        // ... implementation ...
        
    let predict (model: TrainedModel) (input: Vector<float>) =
        // ... implementation ...
```

By making the record `private`, we ensure that no one can create a `TrainedModel` with garbage weights manually. They *must* use the `train` function. This guarantees the integrity of your research outputs.

-----

## 7.5 Benchmarking Performance

You wrote your algorithm. Is it fast?

In Python, you might use generic `time.time()` calls. In F\#, we can use precise measuring tools.

### Simple Timing

If you are running a script (`.fsx`), you can use the `#time` directive.

```fsharp
#time "on"
let result = train myConfig myData
#time "off"
```

*Output:* `Real: 00:00:01.254, CPU: 00:00:01.250, GC gen0: 5, gen1: 1`

### Diagnosing Memory (GC)

The output above includes **GC (Garbage Collection)** stats.

  * `gen0`: Short-lived objects (very cheap).
  * `gen2`: Long-lived objects (expensive to clean).

If your algorithm triggers many `gen2` collections, it means you are holding onto memory too long. In functional programming, we prefer creating many short-lived objects (immutable updates) which are cleaned up cheaply in `gen0`.

### Optimization Tip: Arrays vs. Lists

For deep algorithmic recursion or heavy math:

  * **Lists (`[1; 2]`)**: Great for structure, pattern matching, and small-to-medium data. Memory scattered (linked list).
  * **Arrays (`[|1; 2|]`)**: Great for raw speed and locality of reference. Memory contiguous.

If `runLengthEncoding` (from 7.1) runs too slow on 10 million items, switching the internal logic to use Arrays and a `while` loop (encapsulated inside the function) is a valid optimization. F\# allows you to write imperative code *locally* for performance while keeping the external API pure.

-----

## Recap

1.  **Recursion over Loops:** Use recursive functions with pattern matching to handle complex iteration logic.
2.  **Tail Call Optimization (TCO):** F\# compiles tail-recursive functions into safe loops, avoiding stack overflows.
3.  **Variants as Types:** Use Discriminated Unions (e.g., `Euclidean | Manhattan`) to model algorithmic choices instead of string flags.
4.  **Fold for State:** Machine learning training loops can often be expressed as a `fold` operation that accumulates the model state over the dataset.
5.  **Performance Awareness:** Use `#time` to check GC pressure. Prefer Arrays over Lists for high-performance numerical kernels.

-----

## Exercises

**1. Tail Recursion (True/False)**
The following function is tail-recursive:

```fsharp
let rec factorial n =
    if n <= 1 then 1
    else n * factorial (n - 1)
```

*(Hint: Is the recursive call the absolute last step, or is there a multiplication happening after it returns?)*

**2. Modeling Variants (Multiple Choice)**
You are implementing an optimizer. It can be "SGD" (needs a learning rate) or "Adam" (needs learning rate, beta1, beta2). How should you model this?

  * A) A class `Optimizer` with fields `lr`, `beta1`, `beta2` (nullable).
  * B) A Discriminated Union `Optimizer` with cases `SGD of float` and `Adam of float * float * float`.
  * C) Two separate boolean flags `isSGD` and `isAdam`.

**3. The Fold Update (Applied)**
Write a function `sumOfSquares` using `List.fold`. It should take a list of floats `[x1; x2; ...]` and return $\sum x^2$.
*(Hint: The accumulator starts at 0.0).*
