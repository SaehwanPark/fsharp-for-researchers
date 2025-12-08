## Solution Design

### 1\. High-Level Strategy

We will build a "Resilient Numerical Pipeline."

1.  **Ingest:** Read lines -\> Parse to `Result<Item, string>`.
2.  **Segregate:** Split Results into `ValidItems` and `Errors`. Report Errors.
3.  **Compute:** Define a function `simulateRisk : Item -> float` that runs the inner Monte Carlo loop.
4.  **Report:** Filter items by risk threshold and print.

### 2\. Domain Types

  * `Item`: Record with ID, Stock, Mean, StdDev.
  * `SimulationConfig`: Record to hold settings (Days=30, Runs=1000, Seed=123).
  * `ParsingError`: String describing why a row failed.

### 3\. Key Libraries

  * `MathNet.Numerics.Distributions.Normal`: To sample demand values like `dist.Sample()`.
