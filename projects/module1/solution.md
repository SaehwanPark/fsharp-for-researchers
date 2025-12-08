## Solution Design

### 1\. High-Level Strategy

We will build the solution as a classic **ETL (Extract, Transform, Load)** pipeline, a pattern emphasized in Chapter 4, but scaled down for a script.

1.  **Extract:** Split the string by newlines.
2.  **Transform:**
      * **Parse:** Convert strings to Domain Types (`SensorReading`). Failures become `None`.
      * **Filter:** Apply business logic (`isValid`).
3.  **Load/Compute:** Aggregate the clean data to find specific statistics.

### 2\. Domain Types

We need to capture the distinct nature of the sensors. A Discriminated Union is perfect here because a "Reading" isn't just a number; it's a number *contextualized* by what it represents.

  * `Measure` types for safety.
  * `SensorData` DU: `Temperature`, `Acidity`, `Battery`.

### 3\. File Structure

We will use a single `.fsx` script.

  * **Section 1:** Type Definitions.
  * **Section 2:** Helper functions (Parsing).
  * **Section 3:** Business Logic (Validation).
  * **Section 4:** The Pipeline.