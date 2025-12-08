// ==========================================
// 1. Domain Modeling (Chapters 1 & 3)
// ==========================================

open System

// Define Units of Measure to prevent mixing up data types
[<Measure>] type degC
[<Measure>] type pH
[<Measure>] type percent

// A Discriminated Union to represent valid sensor types
type SensorValue =
  | Temperature of float<degC>
  | Acidity of float<pH>
  | Battery of float<percent>

// A Record to hold the full context of a reading
type TelemetryRecord = {
  Timestamp : DateTime
  Reading : SensorValue
}

// ==========================================
// 2. Parsing Logic (Chapters 2 & 3)
// ==========================================

// Helper to safely parse a float
let tryParseFloat (str: string) =
  match Double.TryParse(str) with
  | true, v -> Some v
  | _ -> None

// The Parser: String -> TelemetryRecord Option
// We return 'Option' because parsing might fail (messy data)
let parseLine (line: string) : TelemetryRecord option =
  let parts = line.Split(',')
  
  if parts.Length <> 3 then 
    None
  else
    let tsStr = parts.[0]
    let sensorType = parts.[1]
    let valueStr = parts.[2]

    // We use a computation expression-like approach using Option.bind 
    // or simple matching. Let's use matching for clarity (Chapter 3).
    match DateTime.TryParse tsStr, tryParseFloat valueStr with
    | (true, ts), Some v ->
      // Match the sensor ID to create the correct typed value
      match sensorType with
      | "TEMP" -> Some { Timestamp = ts; Reading = Temperature(v * 1.0<degC>) }
      | "PH" -> Some { Timestamp = ts; Reading = Acidity(v * 1.0<pH>) }
      | "BATTERY" -> Some { Timestamp = ts; Reading = Battery(v * 1.0<percent>) }
      | _ -> None // Unknown sensor
    | _ -> None // Parsing failed

// ==========================================
// 3. Business Logic (Chapters 3 & 4)
// ==========================================

// Rules for what constitutes "Real" data vs Sensor Glitches
let isValidRecord (record: TelemetryRecord) =
  match record.Reading with
  | Temperature t -> t >= -2.0<degC> && t <= 40.0<degC>
  | Acidity a -> a >= 0.0<pH> && a <= 14.0<pH>
  | Battery b -> b >= 0.0<percent> && b <= 100.0<percent>

// ==========================================
// 4. The Pipeline (Chapter 4)
// ==========================================

let rawData = """TIMESTAMP,SENSOR_ID,VALUE
2024-01-01T12:00:00,TEMP,14.5
2024-01-01T12:01:00,PH,8.1
2024-01-01T12:02:00,ERR_RX,null
2024-01-01T12:03:00,TEMP,14.8
2024-01-01T12:04:00,BATTERY,98.5
2024-01-01T12:05:00,TEMP,300.0
2024-01-01T12:06:00,PH,-2.0
2024-01-01T12:07:00,TEMP,14.6
2024-01-01T12:08:00,BATTERY,-5.0"""

printfn "--- Starting Processing Pipeline ---"

// Split lines
let lines = 
  rawData.Split([|'\n'; '\r'|], StringSplitOptions.RemoveEmptyEntries)
  // Skip CSV header
  |> Array.tail 

// Step 1: Parse Raw Text -> Domain Objects
// We use Array.choose to map AND filter out Nones simultaneously
let parsedRecords = 
  lines
  |> Array.choose parseLine

// Step 2: Filter Logic
// Remove physically impossible values
let validRecords = 
  parsedRecords
  |> Array.filter isValidRecord

// Step 3: Analysis
// Extract only temperatures to calculate average
let temps = 
  validRecords
  |> Array.choose (fun r -> 
      match r.Reading with
      | Temperature t -> Some t
      | _ -> None
  )

// Calculate Average
// Note: We strip the unit for display, or keep it if we had a generic average function
let avgTemp = 
  if temps.Length > 0 then
    temps |> Array.average
  else
    0.0<degC>

// ==========================================
// 5. Reporting
// ==========================================

printfn "Total Input Lines: %d" lines.Length
printfn "Successfully Parsed: %d" parsedRecords.Length
printfn "Valid (Clean) Records: %d" validRecords.Length
printfn "-----------------------------"
printfn "Average Ocean Temp: %.2f C" (float avgTemp)

// Example of Pattern Matching used for simple reporting
printfn "-----------------------------"
printfn "Detailed Log:"
validRecords
|> Array.iter (fun r -> 
  match r.Reading with
  | Temperature t -> printfn "[TEMP] %.1f C at %s" (float t) (r.Timestamp.ToShortTimeString())
  | Acidity pH -> printfn "[PH  ] %.1f    at %s" (float pH) (r.Timestamp.ToShortTimeString())
  | Battery b -> printfn "[BATT] %.1f %%   at %s" (float b) (r.Timestamp.ToShortTimeString())
)