import os

root_dir = r"C:\Users\Isaac\.gemini\antigravity\scratch\frederman"

for root, dirs, files in os.walk(root_dir):
    if any(x in root for x in [".git", ".vs", "bin", "obj", "publish"]):
        continue
    cs_files = [f for f in files if f.endswith(".cs")]
    if cs_files:
        rel = os.path.relpath(root, root_dir)
        print(f"=== {rel} ({len(cs_files)} .cs files) ===")
        for f in cs_files:
            print(f"  {f}")
