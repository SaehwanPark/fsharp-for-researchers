# Preface

Welcome to a thin-ish book written for the researcher who is tired of debugging runtime errors at 2:00 AM.

If you are reading this, you are likely comfortable with Python. You have probably spent years mastering NumPy arrays, wrangling pandas DataFrames, and generating Matplotlib charts. You know the power of the Python ecosystem—the immense library of tools that allows you to go from a blank file to a working prototype in an afternoon.

But you also know the shadow side of that speed.

You know the anxiety of returning to a project six months later, only to find that the script no longer runs because a library updated silently. You know the frustration of waiting four hours for a simulation to finish, only for it to crash in the final minute because of a `TypeError: 'NoneType' object is not subscriptable` that could have been caught instantly by a compiler. You know the difficulty of refactoring a 5,000-line "god script" that holds your lab’s entire methodology together, fearing that one wrong move will silently corrupt your results.

This book is about fixing those problems.

## Why F#?

In the search for "better" scientific software, researchers often face a dilemma.
* **C++** offers speed but demands you manage memory manually and write verbose boilerplate.
* **Julia** offers mathematical elegance but retains the dynamic typing risks that plague production pipelines.
* **Rust** offers incredible safety but forces you to fight the borrow checker when you just want to multiply two matrices.

**F#** sits in a unique "Goldilocks" zone. It is a functional-first language that is concise, expressive, and incredibly safe, yet it runs on the battle-tested, high-performance .NET runtime. It allows you to model your domain so effectively that "illegal states" become impossible to represent. It catches bugs before your code ever runs. And perhaps most importantly for you: **it feels like scripting.**

F# allows you to write code that looks as clean as Python but behaves as robustly as C#.

## Who This Book Is For

This book is written specifically for **Python-proficient scientists, engineers, and data analysts**.

* I do **not** assume you know functional programming.
* I do **not** assume you know C# or .NET.
* I **do** assume you understand variables, loops, functions, and the basic workflow of data analysis (load $\rightarrow$ clean $\rightarrow$ model $\rightarrow$ plot).

If you are a researcher looking to move from "scripts that run once" to "systems that run forever," this book is for you.

## How This Book Is Organized

We will follow the natural evolution of a research project:

* **Part I: Foundations** translates your existing Python knowledge into F#. You will learn how to read code, use the interactive REPL, and why types are your best friend.
* **Part II: Scientific Patterns** focuses on the work you do every day: parsing CSVs, running simulations, and performing numerical analysis—but doing it safely.
* **Part III: Engineering** teaches you how to structure, test, and parallelize your code. This is where you transition from "coder" to "research engineer."
* **Part IV: Architecture** looks at the big picture: interoperability with ML.Net, designing reproducible systems, and building tools that last.

## A Note to the Reader

Learning a new language is an investment. It requires a temporary slowdown in your productivity as you rewire your brain to think in "pipelines" and "types" rather than "loops" and "objects."

I promise you the investment is worth it. Once you experience the peace of mind that comes from a compiler that guides you, refactoring that is instantaneous and safe, and a domain model that exactly matches your scientific reality, you will wonder how you ever managed without it.

Let’s begin.

**Sae-Hwan Park, PhD**
*December 2025*

---

## A Note on the Writing Process

This book emerged from an intensive self-study journey into F#. During the 
learning phase, I used AI tools (Google Gemini) to generate practice problems 
and code exercises, which I then solved, debugged, and corrected. This 
iterative process of validating and fixing AI-generated content deepened my 
understanding far more than passive reading could.

The concepts, pedagogical structure, and all explanatory prose are my own, 
drawn from years of research engineering experience. During the drafting 
process, I used AI as a proofreading tool—similar to working with a technical 
reviewer or copy editor. I selectively incorporated suggestions that improved 
clarity while rejecting changes that didn't align with the book's voice or 
technical accuracy.

All code examples have been manually verified, and all errors remain my 
responsibility.

**A word on AI-assisted learning:** If you're exploring F# (or any technical 
topic), I encourage you to use AI tools as study partners. But remember: the 
value comes from the validation process. When you find and fix AI's mistakes, 
that's where real learning happens.
