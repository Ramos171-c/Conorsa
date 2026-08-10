import os

root_dir = r"C:\Users\Isaac\.gemini\antigravity\scratch\frederman\EnterpriseBillingSystem.Application"

for root, dirs, files in os.walk(root_dir):
    cs_files = [f for f in files if f.endswith(".cs")]
    if cs_files:
        rel = os.path.relpath(root, root_dir)
        print(f"\n=== {rel} ===")
        for f in cs_files:
            print(f"  {f}")
