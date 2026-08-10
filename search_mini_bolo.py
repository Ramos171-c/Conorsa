import json

with open(r"C:\Users\Isaac\.gemini\antigravity\scratch\frederman\db_products_inventory.json", "r", encoding="utf-8-sig") as f:
    db_products = json.load(f)

for p in db_products:
    name = p["ProductName"].upper()
    code = p["InternalCode"].upper()
    if "BOLO" in name or "MINI" in name or code in ["CA028", "CA029"]:
        print(f"Code: {code} | Name: {p['ProductName']} | Stock: {p['CurrentPhysicalStock']}")
