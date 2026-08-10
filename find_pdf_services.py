import os

root_dir = r"C:\Users\Isaac\.gemini\antigravity\scratch\frederman"

for root, dirs, files in os.walk(root_dir):
    if any(x in root for x in [".git", ".vs", "bin", "obj"]):
        continue
    for file in files:
        if file.endswith(".cs"):
            path = os.path.join(root, file)
            with open(path, "r", encoding="utf-8", errors="ignore") as f:
                content = f.read()
                if "pdf" in content.lower() or "questpdf" in content.lower() or "print" in content.lower() or "imprimir" in content.lower() or "factura" in content.lower():
                    if "invoice" in file.lower() or "sales" in file.lower() or "pdf" in file.lower() or "printer" in file.lower():
                        print(path)
