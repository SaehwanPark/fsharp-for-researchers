# Chapter 6 — Numerical Workflows

For many researchers, Python *is* NumPy. The ability to manipulate arrays, perform broadcasting, and execute linear algebra operations efficiently is the bedrock of modern scientific computing.

In the .NET ecosystem, this role is filled by **Math.NET Numerics**. While it shares the same mathematical capabilities as NumPy, it differs philosophically. NumPy is dynamic and permissive (often broadcasting shapes implicitly), whereas Math.NET is strict and explicit. This chapter bridges that gap, showing you how to translate your numerical intuition into safe, high-performance F\# code.

-----

## 6.1 Math.NET: Vectors and Matrices

The core library for numerical computing in F\# is `MathNet.Numerics`. It provides the dense and sparse vector/matrix implementations you expect.

### Installation

```bash
dotnet add package MathNet.Numerics
dotnet add package MathNet.Numerics.FSharp
```

### Creating Vectors and Matrices

In NumPy, everything is an `ndarray`. In Math.NET, we distinguish between `Vector<T>` and `Matrix<T>`.

```fsharp
open MathNet.Numerics.LinearAlgebra

// 1. Creating a Vector (Dense)
let v1 = vector [ 1.0; 2.0; 3.0 ]
let v2 = Vector<double>.Build.Dense(3, 1.0) // [1.0; 1.0; 1.0]

// 2. Creating a Matrix
// 2x3 Matrix (2 rows, 3 cols)
let m = matrix [ [ 1.0; 2.0; 3.0 ]
                 [ 4.0; 5.0; 6.0 ] ]

// 3. Basic Arithmetic
let v3 = v1 + v2        // Element-wise addition
let dot = v1 * v2       // Dot product (scalar)
let result = m * v1     // Matrix-Vector multiplication
```

### Slicing and Indexing

F\# uses special syntax for indexing that is slightly different from Python's square brackets.

  * **Python:** `m[0, 1]`
  * **F\#:** `m.[0, 1]` (Note the dot before the bracket)

<!-- end list -->

```fsharp
// Slicing: Get the first row
let row0 = m.Row(0)

// Get a sub-matrix (Rows 0-1, Cols 0-1)
let subM = m.[0..1, 0..1]
```

### The "Broadcasting" Difference

NumPy is famous for implicit **broadcasting**—automatically expanding smaller arrays to match larger ones (e.g., adding a row vector to every row of a matrix).

Math.NET takes a hybrid approach:

1.  **Scalar Broadcasting:** Supported. You **can** add, subtract, or multiply a Vector/Matrix by a scalar value (e.g., `v + 5.0`).
2.  **Rank Expansion:** **Not Supported.** You cannot add a `Vector` to a `Matrix` implicitly.

In NumPy, `matrix + vector` automatically adds the vector to every row. In F\#, this raises a compile-time or runtime error. This strictness is a feature: it prevents "silent dimensionality bugs" where you accidentally add a bias term along the wrong axis.

```fsharp
let m = matrix [[1.0; 2.0]; [3.0; 4.0]]
let v = vector [10.0; 20.0]

// 1. Scalar Broadcasting (Works!)
let m2 = m + 100.0 

// 2. Matrix + Vector (Fails in F#, Works in Python)
// let m3 = m + v // Error: The type 'Vector<float>' does not match 'Matrix<float>'

// The Correct F# Approach for Row-wise Addition:
let m3 = m.MapRows(fun row -> row + v)
```

-----

## 6.2 Determinism and Reproducibility

One of the biggest crises in science is **reproducibility**. A major culprit is the Global Random Seed.

### The Problem with Global State

In Python, you often see this at the top of a script:

```python
import numpy as np
np.random.seed(42) # Global mutation!
```

If you import a library that *also* sets the seed, or if you run your tests in parallel, your random stream becomes unpredictable.

### The F\# Approach: Explicit Random Sources

In functional programming, randomness is treated as a dependency. We pass the random source *into* the function.

```fsharp
#r "nuget: MathNet.Numerics"
#r "nuget: MathNet.Numerics.FSharp"

open MathNet.Numerics.Random
open MathNet.Numerics.Distributions

// create a stable source (Mersenne Twister)
let rng = MersenneTwister(343)

// pass it explicitly
let generateNoise (randomSource: System.Random) count =
  // generate standard normal samples
  // use the static method: Normal.Samples (rnd, mean, stdDev)
  Normal.Samples(randomSource, 0.0, 1.0)
  |> Seq.take count
  |> Seq.toList

// usage
let run1 = generateNoise rng 1
```

By passing `randomSource` as an argument, you guarantee that `generateNoise` behaves exactly the same way every time it is called with that specific generator state, regardless of what other threads are doing.

-----

## 6.3 Optimization and Solvers

Research isn't just about multiplying matrices; it's about solving for parameters. Math.NET includes solvers for roots, linear systems, and optimization.

### Finding Roots

Let's find $x$ where $x^2 - 4 = 0$.

```fsharp
open MathNet.Numerics

// Function: f(x) = x^2 - 4
let f x = x**2.0 - 4.0

// Find root starting guess at x=1.0 using Newton-Raphson
let root = FindRoots.OfFunction(f, 1.0) 
// Returns roughly 2.0
```

### Linear Regression (Least Squares)

Fitting a line ($y = mx + c$) is a linear algebra operation: $Ax = B$.

```fsharp
// Data points
let x = vector [ 1.0; 2.0; 3.0 ]
let y = vector [ 2.0; 4.0; 6.5 ] // Roughly 2x

// Design Matrix (Column of x's, Column of 1's for intercept)
let designMatrix = Matrix<double>.Build.DenseOfColumnVectors(x, Vector<double>.Build.Dense(3, 1.0))

// Solve for parameters [slope; intercept]
// Uses QR decomposition by default for stability
let p = designMatrix.Solve(y)

printfn "Slope: %.4f, Intercept: %.4f" p.[0] p.[1]
```

-----

## 6.4 Numerical Stability and Precision

Scientific code is rife with "floating point gremlins." F\# helps mitigate some of these through its type system, though IEEE 754 float behavior remains universal.

### The "Float Equality" Trap

Never compare floats directly.

```fsharp
let a = 0.1 + 0.2
// if a = 0.3 then ... // This is usually False!
```

Math.NET provides a safe way to check "almost equal":

```fsharp
if Precision.AlmostEqual(a, 0.3, 0.0001) then
    printfn "Equal within tolerance"
```

### Preventing Type Mixing

In Python, adding an integer to a float upgrades the integer. In F\#, you must cast explicitly. This seems tedious until it saves you.
If you are working on a high-precision physics simulation, you might use `float` (64-bit double). If you accidentally try to mix in a `float32` (single precision) variable, F\# will throw a compile error. This prevents precision loss from creeping in silently—a common issue in GPU-accelerated code translation.

-----

## 6.5 Monte Carlo Simulation Project

Let’s build a **Geometric Brownian Motion (GBM)** simulator to model stock prices. This combines Vectors, Randomness, and Reproducibility.

### The Model

$$S_t = S_{t-1} \times e^{(\mu - \frac{\sigma^2}{2})dt + \sigma \sqrt{dt} Z_t}$$

### The Implementation

```fsharp
open MathNet.Numerics.LinearAlgebra
open MathNet.Numerics.Distributions
open MathNet.Numerics.Random

module MonteCarlo =
    
    // Configuration Record (Type Safety)
    type SimConfig = {
        S0: float       // Initial Price
        Mu: float       // Drift
        Sigma: float    // Volatility
        Dt: float       // Time step
        Steps: int      // Number of steps
    }

    // The Simulation Function
    let runSimulation (rng: System.Random) (config: SimConfig) =
        // Pre-allocate a vector for speed
        let prices = Vector<double>.Build.Dense(config.Steps)
        prices.[0] <- config.S0

        let drift = (config.Mu - 0.5 * config.Sigma**2.0) * config.Dt
        let vol = config.Sigma * sqrt(config.Dt)
        
        // Loop for time steps
        for t in 1 .. (config.Steps - 1) do
            let shock = Normal.Sample(rng, 0.0, 1.0) // Z_t
            let change = drift + vol * shock
            prices.[t] <- prices.[t-1] * exp(change)
            
        prices

    // Running it
    let run =
        let rng = MersenneTwister(123) // Reproducible Seed
        let config = { S0=100.0; Mu=0.05; Sigma=0.2; Dt=1.0/252.0; Steps=252 }
        
        // Run 1000 simulations
        let results = 
            [1..1000] 
            |> List.map (fun _ -> runSimulation rng config)
        
        // Calculate average final price
        let finalPrices = results |> List.map (fun v -> v.[config.Steps - 1])
        let avgFinal = List.average finalPrices
        
        printfn "Average Final Price after 1000 runs: %.2f" avgFinal
```

### What We Achieved

1.  **Explicit State:** The `rng` is passed in. We can replay any specific simulation if we know the seed state.
2.  **Type Safety:** We can't swap `Mu` and `Steps` because they are named fields in a Record.
3.  **Performance:** We used a mutable Vector (`prices.[t] <- ...`) *inside* the function for raw speed, but the function returns the result safely. This is a common pattern: **Locally Mutable, Globally Immutable.**

-----

## Recap

1.  **Math.NET Numerics** is the F\# equivalent of NumPy. Use `vector` and `matrix` builders.
2.  **Indexing:** Use `.[row, col]` syntax.
3.  **Broadcasting:** Is not implicit. You must align dimensions manually, which prevents silent errors.
4.  **Reproducibility:** Avoid global `Random` instances. Pass `System.Random` (or `MersenneTwister`) as an argument to your simulation functions.
5.  **Performance Pattern:** It is idiomatic to use mutable arrays/vectors *inside* a computationally heavy function for performance, as long as the mutation doesn't leak out to the rest of the program.

-----

## Exercises

**1. Broadcasting Nuances (Short Answer)**
In Python (NumPy), if you have a 2x2 matrix `m` and a length-2 vector `v`, the operation `m + v` will add the vector to every row of the matrix.
In F\# (Math.NET), the code `m + v` will result in a type error.
**Question:** Write the F\# code to explicitly add vector `v` to every row of matrix `m`.
*(Hint: Look for a function on the matrix types called `.MapRows` or similar).*


**2. Determinism (Multiple Choice)**
Why is passing `rng: System.Random` as a function argument better than creating `let rng = System.Random()` inside the function?

  * A) It saves memory.
  * B) It allows the caller to control the seed, ensuring the function output is reproducible.
  * C) It makes the function run faster on GPUs.

**3. Linear Algebra (Applied)**
Using `MathNet.Numerics`, create two 2x2 matrices:
$$A = \begin{bmatrix} 1 & 2 \\ 3 & 4 \end{bmatrix}, \quad B = \begin{bmatrix} 1 & 0 \\ 0 & 1 \end{bmatrix}$$
Write the code to compute $C = A \times B$ and print the element at row 1, column 1 (bottom right).
