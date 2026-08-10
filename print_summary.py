import json

with open("inventory_comparison_report.json", "r", encoding="utf-8") as f:
    report = json.load(f)

coincide = [r for r in report if r["status"] == "COINCIDE"]
diff = [r for r in report if r["status"] == "DIFERENCIA"]

print(f"Total productos en conteo físico: {len(report)}")
print(f"Productos sin diferencia (coinciden exactamente): {len(coincide)}")
print(f"Productos con diferencia (requieren actualización): {len(diff)}")
print("\n--- DETALLE DE DIFERENCIAS ---")
for r in diff:
    sign = "+" if r["diff"] > 0 else ""
    print(f"[{r['code']}] {r['name'][:40]:<40} | Conteo: {r['counted_text']:<22} (Total: {r['total_counted']} un) | Sistema: {r['db_stock']} un | Dif: {sign}{r['diff']} un")
