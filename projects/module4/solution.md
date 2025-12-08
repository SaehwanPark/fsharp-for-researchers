## Solution Design

### 1\. High-Level Strategy

We will use the **Strategy Pattern**.

1.  **Domain Layer:** Pure F\# types. No knowledge of Python.
2.  **Service Layer (Interfaces):** Defines *what* we need (scoring), not *how*.
3.  **Infrastructure Layer (Adapters):**
      * Adapter A: The Mock (Pure F\#).
      * Adapter B: The Python Bridge (Python.NET).
4.  **Application Layer:** Wiring it together.

### 2\. The Python "Model"

To ensure this runs without you needing to `pip install` complex libraries, we will inject a simple Python function string that acts as our "Advanced AI":

```python
def get_score(text):
    text = text.lower()
    if "good" in text or "excellent" in text or "superb" in text: return 0.9
    if "bad" in text or "terrible" in text or "abysmal" in text: return 0.1
    return 0.5
```