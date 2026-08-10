import json

with open("inventory_comparison_report.json", "r", encoding="utf-8") as f:
    report = json.load(f)

coincide = [r for r in report if r["status"] == "COINCIDE"]
print("--- PRODUCTOS QUE COINCIDEN EXACTAMENTE ---")
for r in coincide:
    print(f"[{r['code']}] {r['name']} | Conteo: {r['total_counted']} un | Sistema: {r['db_stock']} un")
