import json

with open(r"C:\Users\Isaac\.gemini\antigravity\scratch\frederman\all_db_products.json", "r", encoding="utf-8-sig") as f:
    products = json.load(f)

print("=== ALL DIAPER RELATED PRODUCTS ===")
for p in products:
    code = p["InternalCode"] or ""
    name = p["Name"] or ""
    desc = p["Description"] or ""
    
    if any(k in name.upper() or k in desc.upper() or k in code.upper() for k in ["PAÑAL", "PANAL", "OSITO", "LUCAS", "MIDDAY", "CALSON", "PEGA", "TA006", "TO012", "TO0"]):
        print(f"Code: {code:<10} | Name: {name:<65} | Desc: {desc}")
