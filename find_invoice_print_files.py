import os

root_dir = r"C:\Users\Isaac\.gemini\antigravity\scratch\frederman"

for root, dirs, files in os.walk(root_dir):
    if any(x in root for x in [".git", ".vs", "bin", "obj"]):
        continue
    for file in files:
        if "invoice" in file.lower() or "print" in file.lower() or "pdf" in file.lower() or "ticket" in file.lower() or "receipt" in file.lower():
            print(os.path.join(root, file))
