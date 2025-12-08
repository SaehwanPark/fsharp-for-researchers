# Chapter 3 — Thinking in Types

In Python, types are often viewed as "hints" or metadata—nice to have, but optional. You might stick a type hint on a function argument, but the interpreter won't stop you if you pass a string into an integer field.

In F\#, types are not just safety checks; they are **modeling tools**. They allow you to describe the "shape" of your world so precisely that invalid states become impossible to represent. Instead of writing code to *check* for errors (e.g., `if variable < 0: raise ValueError`), you design types that prevent the error from existing in the first place.

This chapter introduces the "Algebraic Type System," the superpower of functional programming. We will move beyond simple integers and strings to model complex scientific realities.

-----

## 3.1 Tuples, Records, and Discriminated Unions

Data usually comes in two logical flavors: **"AND"** relationships (a data point has an X coordinate *and* a Y coordinate) and **"OR"** relationships (a result is a Success *or* a Failure).

### Tuples: Ad-Hoc Grouping

Tuples are simple containers for grouping values without the overhead of defining a class. They are identical to Python tuples but are often used more aggressively for returning multiple values from functions.

```fsharp
// A Tuple of (float, float, string)
let reading = (12.5, 45.3, "Sensor_A")

// Deconstructing a tuple
let (temp, humidity, id) = reading
```

### Records: Named Data ("AND" Logic)

When data has a fixed structure, we use **Records**. Unlike Python dictionaries, Records have a fixed set of fields. If you miss a field, the code won't compile.

```fsharp
type ExperimentConfig = {
    TrialCount: int
    LearningRate: float
    DatasetPath: string
}

// Creating an instance
let config = { 
    TrialCount = 100
    LearningRate = 0.05
    DatasetPath = "./data/batch1.csv" 
}
```

  * **Why this matters:** In Python, if you mistype a dictionary key (`config["LearningRates"]`), you find out at runtime. In F\#, the compiler catches the typo immediately.

### Discriminated Unions: Choice ("OR" Logic)

This is the feature that sets F\# apart from Python, C\#, and Java. A **Discriminated Union (DU)** allows a value to be one of several distinct cases, where each case can hold *different* data.

Imagine modeling a biological sample. It might be:

1.  A blood sample (volume in ml).
2.  A tissue biopsy (weight in mg).
3.  A swab (just a location string).

In Python, you might create a generic class with many `None` fields. In F\#, we model this explicitly:

```fsharp
type Sample =
    | Blood of float          // Holds a float (volume)
    | Tissue of float         // Holds a float (weight)
    | Swab of string          // Holds a string (location)

// Usage
let sample1 = Blood 5.5
let sample2 = Swab "Nasal"
```

The `Sample` type is **one** type, but it can take three shapes. You cannot treat a `Swab` like `Blood`—the compiler prevents you from accessing the `float` volume data unless you prove you have a `Blood` sample.

-----

## 3.2 Options and Missingness

### The Billion Dollar Mistake

Tony Hoare, the inventor of the null reference, calls it his "billion-dollar mistake." In Python, almost any variable can be `None`. This leads to defensive coding everywhere: `if x is not None: ...`.

### The `Option` Type

F\# does not allow nulls for standard types. If a value might be missing, you **must** wrap it in an `Option`.

An `Option` has only two cases (it is actually just a Discriminated Union\!):

1.  `Some(value)`: The data exists.
2.  `None`: The data is missing.

<!-- end list -->

```fsharp
// A function that might fail to find a value
let divide top bottom =
    if bottom = 0.0 then None
    else Some (top / bottom)

let result = divide 10.0 0.0 // result is None
```

### Forcing You to Handle It

You cannot use an `Option<float>` as a `float`. You cannot add 5 to it. You *must* unwrap it first. This ensures you never accidentally perform math on a missing value.

-----

## 3.3 Units of Measure and Dimensional Safety

Scientific computing is plagued by unit conversion errors. F\# allows you to annotate numeric types with units. These units are erased during compilation, so there is **zero performance cost**—it is purely a design-time safety check.

### Defining Units

```fsharp
[<Measure>] type m      // Meters
[<Measure>] type s      // Seconds
[<Measure>] type kg     // Kilograms

let distance = 100.0<m>
let time = 9.8<s>

// Compiler infers velocity as float<m/s>
let velocity = distance / time 

// let error = distance + time 
// COMPILE ERROR: Unit mismatch. 'm' is not 's'.
```

This prevents the class of errors where a simulation accidentally adds a position (meters) to a velocity (meters/second).

-----

## 3.4 Pattern Matching as a Decision System

We have defined our types; now we need to work with them. **Pattern Matching** is the "switch statement" evolved. It allows you to deconstruct types based on their shape.

### Replacing `if-elif-else`

Let's handle the `Sample` type we defined in section 3.1. We want to get a string description of the sample.

```fsharp
let describeSample (s: Sample) =
    match s with
    | Blood volume -> 
        sprintf "Blood sample of %.1f ml" volume
    | Tissue weight -> 
        sprintf "Tissue biopsy of %.1f mg" weight
    | Swab location -> 
        sprintf "Swab taken from %s" location
```

### Exhaustiveness Checking

The "killer feature" of pattern matching is **exhaustiveness**.
If you later add a fourth case to `Sample` (e.g., `| Saliva`), the compiler will give you a **warning** on every single `match` expression in your codebase that handles `Sample`, telling you that you forgot to handle the `Saliva` case.

This makes refactoring research code incredibly safe. You can evolve your data model and let the compiler tell you exactly which functions need to be updated.

-----

## 3.5 Case Study: Modeling a Research Dataset

Let's combine Records, DUs, and Options to model a realistic dataset: a series of sensor readings from a lab experiment.

### The Domain

We have a device that attempts to read a chemical concentration.

1.  Sometimes it reads successfully.
2.  Sometimes the sensor is saturated (value is too high to read).
3.  Sometimes the sensor is offline.

### The Model

```fsharp
// 1. Define the possible states of a single reading
type ReadingStatus =
    | Valid of float
    | Saturated
    | Offline

// 2. Define the record for a full data point with metadata
type DataPoint = {
    Timestamp: System.DateTime
    SensorId: string
    Status: ReadingStatus  // Nesting the DU inside the Record
}

// 3. Let's process a list of these points
let calculateAverage (points: DataPoint list) =
    // Extract only the Valid values
    let validValues = 
        points 
        |> List.choose (fun p -> 
            match p.Status with
            | Valid v -> Some v    // Keep the value
            | Saturated -> None    // Ignore
            | Offline -> None      // Ignore
        )
    
    if validValues.IsEmpty then 0.0
    else List.average validValues
```

### Contrast with Python

In Python, `Status` might just be a float, where `-1.0` means offline and `999.0` means saturated. If you forget to filter out `-1.0`, your average calculation will be completely wrong, but the code will run without errors.
In F\#, `Saturated` is a distinct type case. You *cannot* accidentally include it in a mathematical average because it isn't a number—it's a state.

-----

## Recap

  * **Records** define fixed data shapes ("AND" logic), ensuring you don't miss fields.
  * **Discriminated Unions (DUs)** define distinct choices ("OR" logic), allowing a single type to represent different data structures safely.
  * **Options** replace `null`/`None`. They force you to handle the "missing" case explicitly.
  * **Pattern Matching** allows you to branch logic based on the shape of data, with the compiler checking that you covered every possibility.
  * **Units of Measure** provide compile-time checking for physical dimensions, preventing unit conversion bugs.

-----

## Exercises

**1. Modeling Choices (Multiple Choice)**
You are building a login system. A user can be either `Guest`, `Registered` (with a username), or `Admin` (with a username and access level). Which F\# type best models this?

  * A) A Record with `Username` and `AccessLevel` fields that can be null.
  * B) A Discriminated Union with cases `Guest`, `Registered of string`, and `Admin of string * int`.
  * C) A Tuple of `(string, string, int)`.

**2. The Option Fix (Code Correction)**
The following code fails to compile because it tries to add an `int` to an `Option<int>`.

```fsharp
let maybeValue = Some 10
let result = maybeValue + 5 // Error!
```

Rewrite the code using `match` or `Option.map` to correctly add 5 to the value *if it exists*, returning a new Option.

**3. Pattern Matching Exhaustiveness (Short Answer)**
If you have a DU `type Color = Red | Green | Blue`, and you write a match expression that only handles `Red` and `Green`, what will the F\# compiler do?

  * A) Throw an error at runtime when `Blue` appears.
  * B) Automatically return `null` for `Blue`.
  * C) Issue a warning at compile-time stating that the match is incomplete.