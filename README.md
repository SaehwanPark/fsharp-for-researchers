![F# logo](https://fsharp.org/img/logo/fsharp512.png)

# 📘 F\# for Researchers

Author:

Sae-Hwan Park, PhD

Research Data Scientist
@ Penn Medicine

**A practical guide for scientists, data engineers, and ML practitioners transitioning from Python to F\#.**

This e-book bridges the gap between exploratory scripting and robust, reproducible research engineering. It demonstrates how F\#'s type system, immutability, and functional pipelines can eliminate entire categories of bugs common in scientific workflows without sacrificing the concise, iterative feel of scripting.

-----

## 📖 Table of Contents

You can read the book directly in this repository. The content is located in the `content/` directory.

### **Front Matter**

  * **[Preface](content/chapter00_preface.md)**
      * *Why this book exists, who it is for, and the promise of "scripting with safety."*

### **Part I — Foundations Through Familiarity**

  * **[Chapter 1: Why Another Language?](content/chapter01.md)**
      * *The limits of dynamic tooling, the "reproducibility crisis," and where F\# fits in the modern scientific landscape.*
  * **[Chapter 2: Getting Started Without Friction](content/chapter02.md)**
      * *Setting up the toolchain (Ionide, .NET SDK), the REPL, and writing your first data script.*
  * **[Chapter 3: Thinking in Types](content/chapter03.md)**
      * *Moving beyond "everything is a list." Using Records, Discriminated Unions, and Units of Measure to model reality.*
  * **[Chapter 4: From Imperative Scripts to Functional Pipelines](content/chapter04.md)**
      * *Replacing loops with the pipe operator (`|>`) and understanding immutability as a tool for correctness.*

  * **[Project: The DeepSea Sensor Validator](projects/module1/)**
      * **Scenario:** Cleaning and validating noisy telemetry data from ocean buoys.
      * **Key Concepts:** Domain modeling with Discriminated Unions, Units of Measure (`[<Measure>]`), and basic functional ETL pipelines using `Option` types.

### **Part II — Scientific Computing Patterns**

  * **[Chapter 5: Working with Data](content/chapter05.md)**
      * *Loading CSV/JSON, using DataFrames, and performing type-safe aggregations.*
  * **[Chapter 6: Numerical Workflows](content/chapter06.md)**
      * *Matrix math with Math.NET, deterministic simulation, and Monte Carlo methods.*
  * **[Chapter 7: Modeling and Algorithms](content/chapter07.md)**
      * *Algorithmic thinking, recursive models, and benchmarking performance.*
  * **[Chapter 8: Working With Uncertainty](content/chapter08.md)**
      * *Replacing exceptions with `Result` types and building resilient data ingestion pipelines.*

  * **[Project: Supply Chain Monte Carlo](projects/module2/)**
      * **Scenario:** Predicting inventory stockout risks using probabilistic simulation.
      * **Key Concepts:** Numerical computing with `MathNet.Numerics`, reproducible randomness, and robust error handling using the `Result` type (Railroad Oriented Programming).

### **Part III — Applied Research Engineering**

  * **[Chapter 9: Asynchronous and Parallel Computing](content/chapter09.md)**
      * *Scaling analysis to all cores using `Array.Parallel` and handling I/O with `async` workflows.*
  * **[Chapter 10: Visualization and Reporting](content/chapter10.md)**
      * *Using Plotly.NET for interactive charts and automating HTML report generation.*
  * **[Chapter 11: Packaging, Testing, and Distribution](content/chapter11.md)**
      * *Project structure, dependency management, and Property-Based Testing with FsCheck.*
  * **[Chapter 12: End-to-End Case Study](content/chapter12.md)**
      * *Building a complete clinical trial analysis pipeline: Ingestion -\> Validation -\> Modeling -\> Metadata.*

  * **[Project: GridRunner Hyperparameter Tuner](projects/module3/)**
      * **Scenario:** Optimizing a "black box" scientific model by running parallel experiments.
      * **Key Concepts:** Concurrency with `Async` workflows, parallel data processing, and generating interactive visualization reports (Heatmaps) using `Plotly.NET`.

### **Part IV — Advanced and Forward-Looking**

  * **[Chapter 13: High-Performance Machine Learning with ML.NET](content/chapter13.md)**
      * *Exploring the high-performance ML.NET framework, demonstrating how to integrate it with Deedle and Math.NET to build type-safe machine learning pipelines*
  * **[Chapter 14: Designing Research Systems That Last](content/chapter14.md)**
      * *Architectural patterns (Functional Core/Imperative Shell), data provenance, and API design.*

### **Back Matter**

  * **[Epilogue](content/chapter15_epilogue.md)**
      * *Reflections on the shift in mindset from "coding for an answer" to "engineering a system."*

-----

## 🛠️ Running the Examples

This book relies on the [.NET 8+ SDK](https://dotnet.microsoft.com/download) (all tested with `.NET 10`).

To run the examples locally:

1.  Clone this repository:
    ```bash
    git clone https://github.com/SaehwanPark/fsharp-for-researchers.git
    cd fsharp-for-research
    ```
2.  Navigate to the code samples (if applicable) or use the snippets provided in the markdown chapters with `dotnet fsi`.

## 🤝 Contributing

This is an open project. If you find a typo, a bug in a code snippet, or have a suggestion for a clearer explanation:

1.  Fork the repository.
2.  Create a branch for your fix (`git checkout -b fix/typo-chapter-3`).
3.  Submit a Pull Request.

## Acknowledgments

This book would not exist without the F# community's welcoming spirit and 
excellent documentation. Special thanks to the maintainers of MathNet.Numerics, 
Plotly.NET, and Deedle for building the scientific computing foundation that 
makes this work possible.

I'm grateful to the creators of Ionide and the .NET team for building tooling 
that makes F# accessible to newcomers.

**On AI assistance:** During the writing process, I used Google Gemini as a 
proofreading and feedback tool, similar to how one might work with a technical 
reviewer. The ideas, structure, examples, and technical content are my own, 
but AI helped me refine the presentation. All code has been manually validated.

Finally, thank you to every research staff who has struggled with fragile Python 
scripts. This book is for you.

## 📄 License

The text content of this book is licensed under the **Creative Commons Attribution 4.0 International (CC BY 4.0)**.
The code samples are licensed under the **MIT License**.
