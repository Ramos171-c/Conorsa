import os
import re

root_dir = r"C:\Users\Isaac\.gemini\antigravity\scratch\frederman"

def search_files(terms):
    results = {}
    for root, dirs, files in os.walk(root_dir):
        if any(ignore in root for ignore in [".git", ".vs", "bin", "obj", "publish"]):
            continue
        for file in files:
            if file.endswith(".cs") or file.endswith(".sql") or file.endswith(".ps1"):
                path = os.path.join(root, file)
                try:
                    with open(path, "r", encoding="utf-8-sig", errors="ignore") as f:
                        lines = f.readlines()
                    for idx, line in enumerate(lines, 1):
                        for term in terms:
                            if re.search(r'\b' + re.escape(term) + r'\b', line, re.IGNORECASE):
                                rel = os.path.relpath(path, root_dir)
                                if rel not in results:
                                    results[rel] = []
                                results[rel].append((idx, term, line.strip()))
                except Exception as e:
                    pass
    return results

print("=== 1. SEARCHING ORDER STATUS & INVENTORY MUTATIONS ===")
res = search_files(["EnCamino", "EnProceso", "PhysicalStock", "ReservedStock", "CommittedStock", "InventoryMovement", "DeductStock", "IsDeleted"])
for file, matches in res.items():
    print(f"\n--- {file} ({len(matches)} matches) ---")
    for line_num, term, text in matches[:15]:
        print(f"  L{line_num:4d} [{term}]: {text}")
