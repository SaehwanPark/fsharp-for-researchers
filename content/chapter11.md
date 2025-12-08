# Chapter 11 — Packaging, Testing, and Distribution

## 11.1 Project Structure for Maintainability

In the early stages of research, a folder full of scripts (`.py` or `.fsx`) is sufficient. But as a project grows—perhaps you are collaborating with three other researchers or running this code on a remote cluster—scripts become fragile. They rely on implicit global state, specific file paths, or the exact order in which they are run.

Transitioning from "scripts" to "software" requires structure. In the .NET ecosystem, the unit of organization is the **Project** (`.fsproj`), not just the file.

### The "Onion" of Research Software

A robust scientific project typically separates **Pure Logic** (the math/science) from **Orchestration** (loading files, printing reports).

**Recommended Directory Layout:**

```text
/MyResearchProject
  /src
     /CoreModel      <-- Library: Pure math, domain types (No I/O)
     /DataIngestion  <-- Library: Parsers, cleaning logic
     /Experiments    <-- Console App: Runs the actual simulations
  /tests
     /CoreTests      <-- Unit checks for the math
  /data              <-- (Ignored by git) Raw inputs
  /scripts           <-- .fsx files for quick plotting/exploration
  MyResearch.sln     <-- Solution file linking the projects
```

### Modules vs. Namespaces

In Python, files map 1:1 to modules. In F\#, you explicitly define organization using `module` or `namespace` keywords.

  * **Namespaces:** logical grouping (like `System.Math`). Good for libraries.
  * **Modules:** groupings of functions and values. Good for specific functionality.

<!-- end list -->

```fsharp
// File: Epidemiology.fs
namespace Research.Models

module SIR =
    type Params = { Beta: float; Gamma: float }
    
    // Pure function: easy to test, easy to package
    let step params state = ...
```

This structure ensures that your core scientific logic is decoupled from how you load data or where you save plots.

-----

## 11.2 Unit and Property-Based Testing

Scientific code is notoriously hard to test. "Is this simulation result correct?" is often a question of scientific validity, not just software correctness. However, the *components* of your simulation must be correct.

### Unit Testing (The Standard Approach)

This mirrors `pytest`. You check specific inputs against expected outputs.

```fsharp
open Xunit // Standard .NET testing library

[<Fact>]
let ``Logistic function returns 0.5 at x=0`` () =
    let result = Math.logistic 0.0
    Assert.Equal(0.5, result)
```

### Property-Based Testing (The F\# Superpower)

For scientific work, **Property-Based Testing (PBT)** is often more valuable than unit testing. Instead of writing 50 assertions for specific numbers, you define a **Property** (a mathematical truth) and ask the computer to try to break it with random inputs.

**The Concept:**
Don't test that `add(2, 2) == 4`. Test that `add(x, y) == add(y, x)` for *any* `x` and `y`.

**Tool:** `FsCheck`

#### Example: Testing a Normalization Function

Imagine you wrote a function to normalize a vector. A property of this function is that the length of the result should always be 1.0 (within floating-point tolerance).

```fsharp
open FsCheck
open FsCheck.Xunit

// The function under test
let normalize (v: float[]) =
    let mag = v |> Array.sumBy (fun x -> x * x) |> sqrt
    if mag = 0.0 then v else v |> Array.map (fun x -> x / mag)

// The Property
[<Property>]
let ``Normalized vector always has magnitude of 1.0 (unless zero vector)`` (v: float[]) =
    let result = normalize v
    let newMag = result |> Array.sumBy (fun x -> x * x) |> sqrt
    
    // Constraint: Skip the zero-vector case for this specific rule
    let isZeroVector = v |> Array.forall (fun x -> x = 0.0)
    
    if isZeroVector then 
        true // Trivial pass
    else 
        // Check invariant
        abs (newMag - 1.0) < 1e-10
```

When you run this, FsCheck generates hundreds of random arrays—empty ones, massive ones, arrays with `NaN`, arrays with negative numbers—and reports the exact input that breaks your code. This discovers edge cases (like division by zero) that you would likely forget to write manual tests for.

-----

## 11.3 Reproducible Build Environments

"It works on my machine" is the bane of reproducible research. In Python, you manage this with `requirements.txt` or Conda environments. In F\#, we use the **Project File** (`.fsproj`).

### Explicit Dependencies

Your `.fsproj` file is the source of truth. It lists the exact version of every library your code needs.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MathNet.Numerics" Version="5.0.0" />
    <PackageReference Include="Plotly.NET" Version="4.2.0" />
  </ItemGroup>
</Project>
```

### Locking Dependencies

For strict reproducibility (ensuring that `5.0.0` doesn't silently upgrade to `5.0.1` on a colleague's machine if you used floating versions), .NET supports **Lock Files**.

Run this once:

```bash
dotnet restore --use-lock-file
```

This generates a `packages.lock.json` file. Commit this to Git. Now, anyone who clones your repository is guaranteed to use the *exact* same binary dependencies you did, down to the hash.

-----

## 11.4 Packaging and Sharing Research Software

Eventually, you may want to share your simulation core so other researchers can use it without copying and pasting files.

### The Artifact: NuGet Packages

In Python, you build a "Wheel" for PyPI. In .NET, you build a "Package" (`.nupkg`) for NuGet.

You don't need complex setup scripts. You just add metadata to your `.fsproj`:

```xml
<PropertyGroup>
    <PackageId>MyLab.EpidemiologyCore</PackageId>
    <Version>1.0.0</Version>
    <Authors>Dr. Jane Doe</Authors>
    <Company>University of Research</Company>
</PropertyGroup>
```

Then run:

```bash
dotnet pack -c Release
```

### Local Feeds (Private Sharing)

You don't have to publish to the public NuGet.org gallery. You can simply put this `.nupkg` file on a shared network drive or a private GitHub Package registry.

Your colleagues can add that folder as a "Source," and they can install your library just like any standard package:

```bash
dotnet add package MyLab.EpidemiologyCore
```

This promotes a **Library-First** mentality: solve the hard science problems once, package them, and then reuse that package in 50 different experiment scripts.

-----

## Recap

  * **Projects over Scripts:** Move code into `.fsproj` structures to separate concerns (Data vs. Model vs. UI).
  * **Property-Based Testing:** Use `FsCheck` to verify mathematical invariants ($f(a,b) = f(b,a)$) rather than just checking single data points. This finds edge cases automatically.
  * **Reproducibility:** Use `<PackageReference>` with exact versions and lock files to guarantee your code runs years from now.
  * **Packaging:** `dotnet pack` turns your research code into a reusable library, encouraging modular science rather than copy-paste reuse.

-----

## Exercises

### 1\. Refactoring Challenge

Take a "god script" (a hypothetical single file that loads CSVs, defines a math function, and plots it) and sketch out how you would divide it into the directory structure described in section 11.1. Which parts go into `src/Core` vs `src/App`?

### 2\. Property Thinking

For a function `reverse(list)`, which of the following is a valid **property** to test?

  * A. `reverse([1; 2])` should equal `[2; 1]`.
  * B. `reverse(reverse(list))` should equal `list`.
  * C. `reverse` should run in under 10ms.

### 3\. Dependency Management

**True or False:** In F\#, simply having the code files is enough to run the project; you do not need the `.fsproj` file to restore dependencies.