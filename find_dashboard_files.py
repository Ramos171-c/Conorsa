import os

root_dir = r"C:\Users\Isaac\.gemini\antigravity\scratch\frederman"

for root, dirs, files in os.walk(root_dir):
    if any(x in root for x in [".git", ".vs", "bin", "obj"]):
        continue
    for file in files:
        if "dashboard" in file.lower() or "metrics" in file.lower() or "analytics" in file.lower() or "report" in file.lower():
            print(os.path.join(root, file))
