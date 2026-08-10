import json

with open(r"C:\Users\Isaac\.gemini\antigravity\scratch\frederman\db_products_inventory.json", "r", encoding="utf-8-sig") as f:
    db_products = json.load(f)

db_dict = {p["InternalCode"].strip().upper(): p for p in db_products}

# User inventory counts from images:
# Format: (code, boxes, units, text_raw)
counted_raw = [
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

    # Image 2
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
    ("MINI BOLO", 0, 175, "175 Unidades"), # Search by name or SKU
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

print("=== CHECKING MATCHES & CONVERSION FACTORS ===")
for code, b, u, text in counted_raw:
    p = db_dict.get(code)
    if not p:
        # Search by name if code is mini bolo or not found
        matches = [x for x in db_products if code in x["ProductName"].upper() or code in x["InternalCode"].upper()]
        if len(matches) == 1:
            p = matches[0]
            print(f"Matched '{code}' -> {p['InternalCode']} ({p['ProductName']})")
        else:
            print(f"NOT FOUND: '{code}' (Matches: {[m['InternalCode'] for m in matches]})")
            continue

    print(f"Code: {p['InternalCode']:<8} | DB Stock: {p['CurrentPhysicalStock']:<5} | Counted text: {text:<25} | Presentations: {p['Presentations']}")
