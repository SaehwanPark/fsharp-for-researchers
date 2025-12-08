# 🛠️ Module 1 Mini-Project: The "DeepSea" Sensor Validator

## Project Brief

**Context:**
You are a research engineer for an oceanography lab. You have a deployable buoy ("DeepSea-1") that transmits telemetry data strings. However, the transmission is noisy: some lines are corrupted, some sensor values drift into physically impossible ranges (e.g., negative battery percentage), and the data format is messy.

**Your Goal:**
Write an F\# script (`.fsx`) that ingests a raw multiline string of sensor data, parses it into a type-safe domain model, filters out "impossible" readings, and calculates the average temperature of the valid data.

### Input Data

Use the following raw string in your script:

```text
TIMESTAMP,SENSOR_ID,VALUE
2024-01-01T12:00:00,TEMP,14.5
2024-01-01T12:01:00,PH,8.1
2024-01-01T12:02:00,ERR_RX,null
2024-01-01T12:03:00,TEMP,14.8
2024-01-01T12:04:00,BATTERY,98.5
2024-01-01T12:05:00,TEMP,300.0
2024-01-01T12:06:00,PH,-2.0
2024-01-01T12:07:00,TEMP,14.6
2024-01-01T12:08:00,BATTERY,-5.0
```

### Requirements & Constraints

1.  **Domain Modeling (Chapter 3):**
      * Use **Units of Measure** for Temperature (Celsius), pH (dimensionless), and Battery (Percent).
      * Use a **Discriminated Union** to model the `SensorReading`. It should handle cases for Temperature, pH, and Battery.
      * Use a **Record** or **Tuple** to hold the parsed timestamp and the reading.
2.  **Safety & Logic (Chapter 3):**
      * Implement a validation function.
          * *Rule 1:* Water Temp must be between -2.0°C and 40.0°C.
          * *Rule 2:* pH must be between 0.0 and 14.0.
          * *Rule 3:* Battery must be between 0.0% and 100.0%.
3.  **The Pipeline (Chapter 4):**
      * Do not use `for` loops. Use the **Pipe Operator (`|>`)** and List/Array module functions (e.g., `Map`, `Filter`, `Choose`).
      * You must parse the raw string into a list of `Option` types (returning `Some` for valid lines, `None` for errors or unknown sensors).
4.  **Output:**
      * Print the number of valid records found vs. total lines.
      * Print the average Temperature of the *valid* records only.

### Hints

  * Remember that `Array.map` transforms data, but `Array.choose` transforms *and* filters out `None` values simultaneously.
  * You can define a helper function `tryParseFloat (s: string)` that returns `float option` to handle text parsing safely.

## Solution

[See this](solution.md)