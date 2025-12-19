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

### The F# Approach

F# excels at defining data schemas. By using **Record Types** and the `LoadColumn` attribute, you bridge the gap between raw data and the type system.

```fsharp
#r "nuget: Microsoft.ML"

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

let ctx = MLContext()
// Data is NOT loaded yet; this is a lazy transformation
let dataView = ctx.Data.LoadFromTextFile<HousingData>("real_estate.csv", hasHeader=true)

```

> **Mental Model Shift: Schema-First vs. Data-First**
> In Python/Pandas, you often load data and "discover" its shape at runtime. In F#, you define the **Schema** first. This "Schema-First" approach feels restrictive at first but prevents the common "KeyError" or "NoneType" crashes that plague Python production pipelines.

---

## 13.2 Enriching the Pipeline with Deedle

In previous chapters, we used **Deedle** for exploratory data analysis (EDA). In a real-world ML workflow, you use Deedle to clean and join data before passing it to ML.NET for training.

### The Python Equivalent (Pandas)

```python
# Cleaning data in Pandas
df = df.dropna()
df = df[df['Price'] > 0]
# Convert to numpy for training
training_data = df.to_numpy()

```

### The F# Approach: The "Deedle-to-ML" Bridge

Since ML.NET requires `IDataView` and Deedle uses `Frame`, you can convert a Deedle Frame into an array of F# records, which ML.NET can then consume.

```fsharp
#r "nuget: Deedle"

open Deedle

// 1. Use Deedle for complex joins or filtering
let rawFrame = Frame.ReadCsv("housing_data.csv")
let cleanedFrame = 
    rawFrame 
    |> Frame.filterRowValues (fun row -> row?Price > 0.0)
    |> Frame.fillMissingWith 0.0

// 2. Convert Deedle Frame to F# Records for ML.NET
let trainingRecords = cleanedFrame.GetRowsAs<HousingData>()

// 3. Load into ML.NET
let dataFromDeedle = ctx.Data.LoadFromEnumerable(trainingRecords)

```

> **Mental Model Shift: Immutability over In-Place Mutation**
> In Python, `df.dropna(inplace=True)` modifies the existing object. In F# and Deedle, every transformation (`Frame.filterRowValues`) returns a **new** frame. This "chaining" of immutable states ensures that your raw data remains untouched and prevents side effects where one part of your code accidentally breaks another's data.

---

## 13.3 Mathematical Customization with Math.NET

While ML.NET provides high-level trainers, you may occasionally need to perform custom mathematical operations—such as calculating a custom similarity matrix—before feeding features into a model.

### The Python Equivalent (NumPy)

```python
import numpy as np

# Custom normalization
mean = np.mean(X, axis=0)
std = np.std(X, axis=0)
X_scaled = (X - mean) / std

```

### The F# Approach: Functional Preprocessing

You can use **Math.NET Numerics** for heavy-duty linear algebra and custom statistical preprocessing.

```fsharp
#r "nuget: MathNet.Numerics"
#r "nuget: MathNet.Numerics.FSharp"

open MathNet.Numerics.LinearAlgebra

let scaleFeatures (data: float32[]) =
    let v = Vector<float32>.Build.Dense(data)
    let mean = v.Mean()
    let std = v.StandardDeviation()
    // Functional 'pipe' application to return a raw array
    (v - mean) / std |> _.ToArray()

```

By combining these, your architecture follows a clean flow: **Deedle** for cleaning, **Math.NET** for custom math, and **ML.NET** for efficient training and deployment.

---

## 13.4 Building and Deploying the Pipeline

ML.NET follows a **Lazy Pipeline Pattern**. You define a chain of **Estimators**, which only execute when you call `.Fit()`.

### The Python Equivalent (scikit-learn Pipeline)

```python
from sklearn.pipeline import Pipeline
from sklearn.linear_model import SGDRegressor

pipeline = Pipeline([
    ('scaler', StandardScaler()),
    ('regressor', SGDRegressor())
])
model = pipeline.fit(X, y)

```

### The F# Approach: Composable Transformers

In F#, the pipeline is a series of functions applied to data. This "Composer" pattern is fundamentally functional.

```fsharp
// 1. Build the pipeline (Estimator Chain)
let pipeline = 
    ctx.Transforms.Concatenate("Features", "Size", "Bedrooms")
    .Append(ctx.Regression.Trainers.Sdca(labelColumnName = "Price"))

// 2. Train (Fit) the model
let model = pipeline.Fit(dataView)

// 3. Evaluate and Save
let metrics = ctx.Regression.Evaluate(model.Transform(dataView), "Price")
ctx.Model.Save(model, dataView.Schema, "HousingModel.zip")

```

> **Mental Model Shift: The Pipeline as a "Recipe"**
> In Python, you often feel like you are "running" steps one by one. In ML.NET/F#, you are building a **Recipe** (the Pipeline). Nothing happens—no data is read, no math is performed—until `Fit()` is called. This allows the .NET runtime to optimize memory and CPU usage across the entire chain.

---

## Recap & Take-Home Messages

### The Modern .NET ML Stack

| Library | Role | Mental Model |
| --- | --- | --- |
| **Deedle** | Data Wrangling | Immutable Transformations |
| **Math.NET** | Custom Math | Pure Functions |
| **ML.NET** | Training & Deployment | Lazy Pipeline Execution |

### Key Takeaways

1. **Streaming vs. RAM:** ML.NET's `IDataView` allows you to handle datasets that would crash a Python script by streaming from disk.
2. **The "Safety First" Tax:** Defining schemas up front takes more time than Python's `read_csv`, but it eliminates entire classes of runtime errors.
3. **No Interop Tax:** Because Deedle, Math.NET, and ML.NET are all native .NET libraries, data flows between them with zero performance overhead.

---

## Exercises

1. **The Bridge:** Create a small Deedle Frame and convert it to an `IDataView` using `LoadFromEnumerable`. What happens if your record types don't match the column names exactly?
2. **Feature Engineering:** Use Math.NET to calculate the Z-score of a feature before feeding it into ML.NET. How does F#'s pipe operator (`|>`) make this cleaner than nested Python calls?
3. **Lazy Execution:** Explain why an error in your CSV file (like a text string in a number column) might not be caught until the very last line of your script calls `pipeline.Fit(data)`.
