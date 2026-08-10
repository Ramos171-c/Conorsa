import os
import re

root_dir = r"C:\Users\Isaac\.gemini\antigravity\scratch\frederman"
keywords = ["EnCamino", "EnProceso", "PhysicalStock", "ReservedStock", "CommittedStock", "IsDeleted", "Inventory", "OrderStatus"]

print("=== SEARCHING C# FILES FOR KEYWORDS ===")
matches = {}
for root, dirs, files in os.walk(root_dir):
    if ".git" in root or ".vs" in root or "bin" in root or "obj" in root:
        continue
    for file in files:
        if file.endswith(".cs"):
            path = os.path.join(root, file)
            try:
                with open(path, "r", encoding="utf-8") as f:
                    lines = f.readlines()
                for i, line in enumerate(lines, 1):
                    for kw in keywords:
                        if kw.lower() in line.lower():
                            if path not in matches:
                                matches[path] = []
                            matches[path].append((i, kw, line.strip()))
            except Exception as e:
                pass

for path, occurrences in matches.items():
    rel = os.path.relpath(path, root_dir)
    print(f"\n--- FILE: {rel} ({len(occurrences)} matches) ---")
    for line_num, kw, text in occurrences[:10]: # Print first 10 per file
        print(f"  L{line_num} [{kw}]: {text}")
