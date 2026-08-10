import os
import re

root_dir = r"C:\Users\Isaac\.gemini\antigravity\scratch\frederman"

matches = []

for root, dirs, files in os.walk(root_dir):
    if any(x in root for x in [".git", ".vs", "bin", "obj", "publish"]):
        continue
    for file in files:
        if file.endswith(".cs"):
            path = os.path.join(root, file)
            try:
                with open(path, "r", encoding="utf-8-sig", errors="ignore") as f:
                    lines = f.readlines()
                for idx, line in enumerate(lines, 1):
                    if "IsDeleted" in line:
                        rel = os.path.relpath(path, root_dir)
                        matches.append((rel, idx, line.strip()))
            except Exception as e:
                pass

print(f"Total IsDeleted lines found: {len(matches)}")
for rel, idx, text in matches[:40]:
    print(f"{rel}:{idx} -> {text}")
