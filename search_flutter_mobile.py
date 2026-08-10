import os

mobile_dir = r"C:\Users\Isaac\.gemini\antigravity\scratch\frederman\EnterpriseBillingSystem.Mobile\lib"

found = {}

for root, dirs, files in os.walk(mobile_dir):
    for f in files:
        if f.endswith(".dart"):
            path = os.path.join(root, f)
            with open(path, "r", encoding="utf-8", errors="ignore") as file:
                lines = file.readlines()
                for idx, line in enumerate(lines):
                    for kw in ["Detalle", "Mayorista", "SemiMayorista", "customerPricingType", "priceType", "pricing_type", "selectedPricingType", "retailPrice", "cost"]:
                        if kw in line:
                            rel_path = os.path.relpath(path, mobile_dir)
                            if rel_path not in found:
                                found[rel_path] = []
                            found[rel_path].append((idx + 1, line.strip()))

for k, v in found.items():
    print(f"=== File: {k} ===")
    for line_no, content in v[:10]:
        print(f"  L{line_no}: {content}")
