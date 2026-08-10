import json

with open(r"C:\Users\Isaac\.gemini\antigravity\scratch\frederman\db_products_inventory.json", "r", encoding="utf-8-sig") as f:
    db_products = json.load(f)

db_dict = {p["InternalCode"].strip().upper(): p for p in db_products}

# List of all 65 counted items:
# (code, boxes, extra_units, text_raw)
counted_items = [
    ("GA041", 0, 16, "16 Unidades"),
    ("CA010", 0, 11, "11 Unidades"),
    ("GA005", 1, 19, "1 caja 19 Unidades"),
    ("GA006", 0, 19, "19 Unidades"),
    ("GA011", 0, 12, "12 Unidades"),
    ("GA010", 0, 13, "13 Unidades"),
    ("GA008", 0, 17, "17 Unidades"),
    ("GA024", 0, 20, "20 Unidades"),
    ("GA025", 0, 22, "22 Unidades"),
    ("GA016", 0, 29, "29 Unidades"),
    ("GA017", 0, 41, "41 Unidades"),
    ("GA018", 0, 1, "1 Unidades"),
    ("GA002", 0, 59, "59 Unidades"),
    ("GA001", 0, 3, "3 Unidades"),
    ("GA013", 1, 0, "1 caja"),
    ("GA014", 1, 1, "1 caja + 1 unidad"),
    ("GA015", 0, 5, "5 unidades"),
    ("GA022", 0, 4, "4 Unidades"),
    ("GA021", 0, 8, "8 Unidades"),
    ("GA020", 0, 6, "6 Unidades"),
    ("CA011", 1, 1, "1 caja + 1 Unidades"),
    ("MA006", 0, 14, "14 Unidades"),
    ("MA007", 0, 10, "10 Unidades"),
    ("MA003", 0, 10, "10 Unidades"),
    ("MA002", 0, 7, "7 Unidades"),
    ("CA024", 0, 4, "4 Unidades"),
    ("CA022", 0, 3, "3 Unidades"),
    ("CA005", 1, 4, "1 caja y 4 unidades"),
    ("CA021", 0, 12, "12 Unidades"),
    ("CA039", 0, 7, "7 Unidades"),
    ("CA001", 0, 7, "7 Unidades"),
    ("CA015", 0, 6, "6 Unidades"),
    ("CA014", 0, 7, "7 Unidades"),
    ("GA012", 0, 3, "3 Unidades"),
    ("MA001", 0, 1, "1 Unidades"),
    ("CA038", 0, 4, "4 Unidades"),
    ("CA040", 0, 8, "8 Unidades"),
    ("CA035", 0, 18, "18 Unidades"),
    ("CA016", 0, 22, "22 Unidades"),
    ("CA034", 0, 13, "13 Unidades"),
    ("CA030", 0, 2, "2 Unidades"),
    ("CA006", 0, 11, "11 Unidades"),
    ("GA023", 0, 21, "21 Unidades"),
    ("CA049", 0, 9, "9 Unidades"),
    ("CA048", 0, 17, "17 Unidades"),
    ("CA004", 0, 17, "17 Unidades"),
    ("CA047", 0, 14, "14 Unidades"),
    ("CA036", 0, 1, "1 Unidades"),
    ("CA033", 2, 6, "2 cajas 6 unidades"),
    ("MINI BOLO", 0, 175, "175 Unidades"),
    ("CA002", 1, 1, "1 caja 1 Unidades"),
    ("CA012", 0, 10, "10 Unidades"),
    ("CA013", 0, 8, "8 Unidades"),
    ("CA043", 1, 16, "1 caja y 16 Unidades"),
    ("CA017", 0, 2, "2 Unidades"),
    ("TA010", 0, 2, "2 Unidades"),
    ("CA027", 0, 16, "16 Unidades"),
    ("CA025", 0, 18, "18 Unidades"),
    ("CA026", 0, 15, "15 Unidades"),
    ("TA005", 0, 5, "5 Unidades"),
    ("TA004", 0, 22, "22 Unidades"),
    ("TA001", 11, 0, "11 caja"),
    ("TA002", 0, 9, "9 Unidades"),
    ("TA003", 0, 4, "4 Unidades"),
    ("TA006", 23, 8, "23 cajas y 8 Unidades"),
]

report = []

for code, b, u, text in counted_items:
    if code == "MINI BOLO":
        # Check CA028 and CA029
        p28 = db_dict.get("CA028")
        p29 = db_dict.get("CA029")
        db_stock = (p28["CurrentPhysicalStock"] if p28 else 0) + (p29["CurrentPhysicalStock"] if p29 else 0)
        units_per_box = 1
        total_counted = u
        diff = total_counted - db_stock
        report.append({
            "code": "CA028/CA029",
            "name": "CANDY MINI BOLO SURTIDO (NIÑA / NIÑO)",
            "counted_text": text,
            "box_factor": 1,
            "total_counted": total_counted,
            "db_stock": db_stock,
            "diff": diff,
            "status": "DIFERENCIA" if diff != 0 else "COINCIDE"
        })
        continue

    p = db_dict.get(code)
    if not p:
        print(f"Error: {code} not found in DB!")
        continue

    # Extract conversion factor for "Caja" from Presentations
    box_factor = 1
    pres_str = p.get("Presentations") or ""
    # Format of Presentations: "Caja (Factor: 24.0000, Base: 0) | Unidad (Factor: 1.0000, Base: 1)"
    if "Caja (Factor: " in pres_str:
        try:
            part = pres_str.split("Caja (Factor: ")[1].split(",")[0].strip()
            box_factor = float(part)
        except Exception as e:
            box_factor = 1
    elif "CAJA (Factor: " in pres_str.upper():
        try:
            part = pres_str.upper().split("CAJA (FACTOR: ")[1].split(",")[0].strip()
            box_factor = float(part)
        except Exception as e:
            box_factor = 1

    total_counted = (b * box_factor) + u
    db_stock = p["CurrentPhysicalStock"]
    diff = total_counted - db_stock

    report.append({
        "code": p["InternalCode"],
        "name": p["ProductName"],
        "counted_text": text,
        "box_factor": int(box_factor) if box_factor.is_integer() else box_factor,
        "total_counted": int(total_counted) if total_counted.is_integer() else total_counted,
        "db_stock": int(db_stock) if db_stock.is_integer() else db_stock,
        "diff": int(diff) if diff.is_integer() else diff,
        "status": "DIFERENCIA" if diff != 0 else "COINCIDE"
    })

print(f"Total items analyzed: {len(report)}")
with open("inventory_comparison_report.json", "w", encoding="utf-8") as out:
    json.dump(report, out, ensure_ascii=False, indent=2)

print("Saved report to inventory_comparison_report.json")
