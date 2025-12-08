# Chapter 13 — Interoperability and Ecosystem Bridges

## 13.1 Using Python Libraries via Python.NET

One of the biggest hesitations researchers have when leaving Python is the "Ecosystem Fear." You might love F\#'s type system, but if your specific field relies on a niche library like `BioPython`, `Astropy`, or a specific Hugging Face tokenizer that has no .NET equivalent, switching languages feels impossible.

The good news is that you don't have to choose. You can embed a running Python instance *inside* your F\# application using **Python.NET**.

### How It Works

Unlike calling a Python script via the command line (which involves slow startup times and file parsing), Python.NET loads the Python DLL (dynamic link library) directly into the memory space of your F\# process. You can instantiate Python objects, call their methods, and read their attributes as if they were F\# objects.

### Setting Up the Bridge

You need the `Python.Runtime` package. You also need to tell it where your Python installation lives (specifically the shared library file, e.g., `python310.dll` or `libpython3.10.so`).

```fsharp
// Set environment variable before loading the runtime
// (On Linux/Mac, this points to libpython3.x.so)
System.Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", @"C:\Python39\python39.dll")

#r "nuget: Python.Included, 3.11.4" // Auto-downloads a local Python env
#r "nuget: Python.Runtime.NETStandard, 3.0.1"

open Python.Runtime

// Initialize the Python engine
PythonEngine.Initialize()
```

### Example: Using NumPy from F\#

Let's say you want to use NumPy's `fft` (Fast Fourier Transform) because you trust it and don't want to learn a .NET equivalent yet.

```fsharp
// The 'Py.GIL()' block is mandatory to acquire the Global Interpreter Lock
using (Py.GIL()) (fun _ ->
    // Import numpy just like in Python
    let np = Py.Import("numpy")
    
    // Create a Python list from F# data
    let fsharpData = [| 1.0; 2.0; 1.0; -1.0; 1.5 |]
    let pyArray = np.InvokeMethod("array", fsharpData)
    
    // Call a numpy function
    let fftResult = np.InvokeMethod("fft.fft", pyArray)
    
    // Print the raw Python object representation
    printfn "FFT Result (Python Object): %s" (fftResult.ToString())
    
    // Convert back to F# types if needed
    // (Note: This requires explicit casting logic for complex types)
)
```

### When to Use This

  * **Pro:** Access to 100% of the Python ecosystem (Pandas, Matplotlib, Scikit-learn).
  * **Con:** You lose static type safety at the boundary (everything is a `PyObject`).
  * **Con:** Performance overhead when crossing the language boundary frequently (e.g., inside a tight loop).
  * **Strategy:** Use F\# for your core logic and data pipelines. Use Python.NET only for specific library calls where no .NET alternative exists.

-----

## 13.2 Calling C\#, ML.NET, and TorchSharp

While Python interop is a useful escape hatch, your primary ecosystem is **.NET**. F\# can consume any library written in C\# seamlessly. You generally don't even notice they are written in a different language.

### ML.NET: Classical Machine Learning

For tabular data tasks (regression, classification, clustering), **ML.NET** is the Microsoft-supported open-source framework. It is faster than Scikit-learn for many tasks because it streams data rather than loading it all into memory.

```fsharp
open Microsoft.ML
open Microsoft.ML.Data

// 1. Define input/output data schema
type HousingData = {
    [<LoadColumn(0)>] Size: float32
    [<LoadColumn(1)>] Price: float32
}

type Prediction = {
    [<ColumnName("Score")>] PredictedPrice: float32
}

// 2. Create the ML Context
let ctx = MLContext()

// 3. Build a pipeline (Load -> Normalize -> Train)
let dataPath = "houses.csv"
let dataView = ctx.Data.LoadFromTextFile<HousingData>(dataPath, hasHeader=true, separatorChar=',')

let pipeline =
    ctx.Transforms.NormalizeMinMax("Size")
    .Append(ctx.Regression.Trainers.Sdca(labelColumnName="Price", featureColumnName="Size"))

// 4. Train
let model = pipeline.Fit(dataView)
```

### TorchSharp: Deep Learning

For Deep Learning, you might assume you are stuck with Python/PyTorch. However, PyTorch is actually a C++ library (`libtorch`) with a Python wrapper.

**TorchSharp** provides .NET bindings to that same C++ library. It is **not** a wrapper around Python; it talks directly to the GPU native code. This means you get PyTorch performance with F\# type safety.

```fsharp
open TorchSharp
open TorchSharp.Tensor

// Define a tensor on the GPU
let device = if torch.cuda_is_available() then torch.CUDA else torch.CPU
let x = torch.randn([| 100L; 100L |], device=device)

// Perform tensor operations
let y = x * 2.0 + 1.0
let mean = y.mean()

printfn "Mean value: %f" (mean.ToDouble())
```

**Why this matters:** You can port PyTorch research code almost line-for-line to F\#, but now your data loaders are thread-safe, and your tensor dimensions can be clearer.

-----

## 13.3 Integrating with Native and Rust Code

Sometimes you need raw speed that even managed languages (C\#, F\#, Java) cannot provide, or you need to interface with a legacy physics engine written in C or Fortran.

### Platform Invoke (P/Invoke)

F\# uses the `DllImport` attribute to call functions exported from native shared libraries (`.dll` on Windows, `.so` on Linux, `.dylib` on macOS).

### Example: Calling a Rust Function

Suppose you have a Rust function that performs a heavy simulation step:

```rust
// Rust: libsimulation.rs
#[no_mangle]
pub extern "C" fn run_simulation(iterations: i32, start_val: f64) -> f64 {
    // ... highly optimized logic ...
    start_val + (iterations as f64)
}
```

You compile this to `simulation.dll`. Now you call it from F\#:

```fsharp
open System.Runtime.InteropServices

module NativeSim =
    // Define the signature ensuring types match C-standards
    [<DllImport("simulation.dll", CallingConvention = CallingConvention.Cdecl)>]
    extern double run_simulation(int iterations, double startVal)

// Usage
let result = NativeSim.run_simulation(1000, 5.5)
printfn "Result from Rust: %f" result
```

### When to Care

Most data science work does not require this. However, if you are building a custom physics simulation or a high-frequency trading engine, the ability to drop down to C/Rust for the "hot path" while keeping the "business logic" in clean F\# is a powerful architecture.

This is exactly how libraries like NumPy work under the hood (Python logic wrapping C code). In F\#, you can build these hybrid systems yourself.

-----

## Recap

  * **Don't Reinvent the Wheel:** If a library exists in Python and has no .NET equivalent, use **Python.NET** to bridge it.
  * **F\# + C\# = One Ecosystem:** You can use any NuGet package (ML.NET, MathNet, TorchSharp) natively. You are not limited to "F\# specific" libraries.
  * **Deep Learning:** TorchSharp offers a native, high-performance path to using PyTorch tensors without Python.
  * **Native Power:** `DllImport` allows you to bind to C, C++, or Rust libraries for performance-critical kernels.

-----

## Exercises

### 1\. Interop Strategy

You are tasked with building a system that:

1.  Reads data from a high-speed sensor (driver provided in C++).
2.  Cleans the data (complex logic).
3.  Uses a specific pre-trained Hugging Face Transformer model (available only in Python) for sentiment analysis.

Describe which interoperability method (Native, Pure F\#, or Python.NET) you would use for each step and why.

### 2\. The Wrapper (Code Reading)

Examine the following snippet. What is likely to go wrong if `my_python_lib.dll` is not in the system path?

```fsharp
[<DllImport("my_python_lib.dll")>]
extern int calculate_magic_number(int input);
```

  * A. The compiler will fail.
  * B. It will return 0.
  * C. The program will crash with a `DllNotFoundException` at runtime.

### 3\. TorchSharp Tensors

**True or False:** TorchSharp requires you to install Python and PyTorch on the machine to work. (Hint: Review section 13.2).