# Chapter 13 — High-Performance Machine Learning with ML.NET

While Python is the language of research, **ML.NET** is the framework for **production**. In 2025, as .NET 10 optimizes AI workloads with enhanced SIMD (Single Instruction, Multiple Data) support and faster JIT compilation, ML.NET has become the premier choice for F# developers building scalable, type-safe machine learning systems.

Unlike Python's `scikit-learn`, which often requires loading entire datasets into memory, ML.NET uses a **streaming architecture**. This allows F# applications to train on datasets larger than RAM, making it ideal for enterprise-grade data science.

---

## 13.1 The Core Architecture: MLContext and IDataView

Every ML.NET workflow begins with the **`MLContext`**. Think of this as the "singleton" environment that holds your logging, configuration, and entry points for all ML tasks.

The primary data structure is the **`IDataView`**. It is a schema-aware, lazy-loading data container. Because it is immutable and thread-safe, it aligns perfectly with F#'s functional paradigms.

### The Python Equivalent (scikit-learn)

In Python, we typically use a Pandas DataFrame or a NumPy array, which reside entirely in memory.

```python
import pandas as pd
from sklearn.model_selection import train_test_split

# Load entire CSV into RAM
df = pd.read_csv("real_estate.csv")
X = df[['Size', 'Bedrooms']]
y = df['Price']

```

### The F# Approach: Defining the Data Contract

F# excels at defining data schemas. By using **Record Types** and specific attributes, you bridge the gap between raw data and the type system.

```fsharp
open Microsoft.ML
open Microsoft.ML.Data

[<CLIMutable>]
type HousingData = {
    [<LoadColumn(0)>] Size: float32
    [<LoadColumn(1)>] Bedrooms: float32
    [<LoadColumn(2)>] Price: float32 
}

[<CLIMutable>]
type PricePrediction = {
    [<ColumnName("Score")>] PredictedPrice: float32
}

```

### Understanding the Attributes (Decorators)

Unlike Python's dynamic duck-typing, ML.NET uses these attributes to understand how to map raw data to your types:

* **`[<CLIMutable>]`**: F# records are immutable by default, meaning they lack a parameterless constructor. This attribute tells the compiler to generate a "hidden" mutable version that ML.NET can use to hydrate the record via reflection while you treat it as immutable in your code.
* **`[<LoadColumn(n)>]`**: This tells the `TextLoader` exactly which index in your CSV corresponds to this property. It is the F# equivalent of selecting a column by index in a Pandas `iloc` call.
* **`[<ColumnName("Name")>]`**: ML.NET's internal engines look for specific column names like "Score" for predictions or "Features" for input vectors. Use this to map your friendly F# field names to the specific strings ML.NET expects.

---

## 13.2 Enriching the Pipeline with Deedle

In a real-world ML workflow, you use **Deedle** to clean and join data before passing it to ML.NET for training.

### The Python Equivalent (Pandas)

```python
# Cleaning data in Pandas
df = df.dropna()
df = df[df['Price'] > 0]
training_data = df.to_numpy()

```

### The F# Approach: The "Deedle-to-ML" Bridge

You can convert a Deedle Frame into an array of F# records, which ML.NET can then consume as an enumerable.

```fsharp
open Deedle

// 1. Use Deedle for complex structural manipulation
let rawFrame = Frame.ReadCsv("housing_data.csv")
let cleanedFrame = 
    rawFrame 
    |> Frame.filterRowValues (fun row -> row?Price > 0.0)
    |> Frame.fillMissingWith 0.0

// 2. Convert Frame to Records for ML.NET
// Get the rows as a Series
let trainingSeries = cleanedFrame.GetRowsAs<HousingData>()
// Convert the Series values into a sequence to be compatible with LoadFromEnumerable below
let trainingRecords = trainingSeries.Values 

// 3. Hydrate ML.NET's streaming view
let ctx = MLContext()
let dataFromDeedle = ctx.Data.LoadFromEnumerable(trainingRecords)

```

> **Mental Model Shift: Immutability over In-Place Mutation**
> In Python, `df.dropna(inplace=True)` modifies the object. In F# and Deedle, every transformation returns a **new** frame. This prevents side effects where one part of your code accidentally breaks another's data.

---

## 13.3 Mathematical Customization with Math.NET

While ML.NET provides high-level trainers, you may need **Math.NET Numerics** for custom linear algebra or statistical preprocessing.

### The Python Equivalent (NumPy)

```python
import numpy as np
X_scaled = (X - np.mean(X)) / np.std(X)

```

### The F# Approach: Functional Preprocessing

```fsharp
open MathNet.Numerics.LinearAlgebra

let scaleFeatures (data: float32[]) =
    let v = Vector<float32>.Build.Dense(data)
    let mean = v.Mean()
    let std = v.StandardDeviation()
    (v - mean) / std |> _.ToArray()

```

By combining these, your architecture flows from **Deedle** (cleaning) to **Math.NET** (custom math) and finally to **ML.NET** (training).

---

## 13.4 Building and Deploying the Pipeline

ML.NET follows a **Lazy Pipeline Pattern**. You define a chain of **Estimators**, which only execute when you call `.Fit()`.

### The F# Approach: Composable Transformers

```fsharp
// 1. Build the Recipe (Estimator Chain)
let pipeline = 
    ctx.Transforms.Concatenate("Features", "Size", "Bedrooms")
    .Append(ctx.Regression.Trainers.Sdca(labelColumnName = "Price"))

// 2. Train (Fit) the model
let model = pipeline.Fit(dataFromDeedle)

// 3. Save as a standalone artifact
ctx.Model.Save(model, dataFromDeedle.Schema, "HousingModel.zip")

```

> **Mental Model Shift: The Pipeline as a "Recipe"**
> In ML.NET/F#, you are building a **Recipe** (the Pipeline). No data is read and no math is performed until `Fit()` is called. This allows the .NET runtime to optimize memory and CPU usage across the entire chain.

---

## Recap & Take-Home Messages

### The Modern .NET ML Stack

| Library | Role | Mental Model |
| --- | --- | --- |
| **Deedle** | Data Wrangling | Immutable Transformations |
| **Math.NET** | Custom Math | Pure Functions |
| **ML.NET** | Training & Deployment | Lazy Pipeline Execution |

### Key Takeaways

1. **Streaming vs. RAM:** ML.NET's `IDataView` handles datasets that would crash Python by streaming from disk.
2. **The "Safety First" Tax:** Defining schemas with attributes like `[<LoadColumn>]` prevents runtime crashes.
3. **No Interop Tax:** Deedle, Math.NET, and ML.NET are all native .NET libraries; data flows between them with zero performance overhead.

---

## Exercises

1. **The Bridge:** Convert a Deedle Frame to an `IDataView`. What happens if you forget the `[<CLIMutable>]` attribute on your record?
2. **Attribute Mapping:** Use `[<ColumnName("Label")>]` to map a field called `MarketValue` so that an ML.NET trainer recognizes it as the target.
3. **Lazy Execution:** Explain why an error in your CSV might not be caught until you call `pipeline.Fit(data)`.
