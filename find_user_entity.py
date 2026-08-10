import os

root_dir = r"C:\Users\Isaac\.gemini\antigravity\scratch\frederman\EnterpriseBillingSystem.Domain"

for root, dirs, files in os.walk(root_dir):
    for file in files:
        if "user" in file.lower():
            print(os.path.join(root, file))
