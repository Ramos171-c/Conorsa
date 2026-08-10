import os

root_dir = r"C:\Users\Isaac\.gemini\antigravity\scratch\frederman\EnterpriseBillingSystem.Wpf"

for root, dirs, files in os.walk(root_dir):
    if any(x in root for x in [".git", ".vs", "bin", "obj"]):
        continue
    for file in files:
        if file.endswith(".xaml") or file.endswith(".cs"):
            path = os.path.join(root, file)
            with open(path, "r", encoding="utf-8", errors="ignore") as f:
                content = f.read()
                if "ProductName" in content or "Description" in content or "ProductCode" in content:
                    print(path)
