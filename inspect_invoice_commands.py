import os

sales_dir = r"C:\Users\Isaac\.gemini\antigravity\scratch\frederman\EnterpriseBillingSystem.Application\Sales\Commands"

files_to_print = [
    "CreateSalesInvoiceCommand.cs",
    "PostSalesInvoiceCommand.cs",
    "CancelSalesInvoiceCommand.cs"
]

for filename in files_to_print:
    filepath = os.path.join(sales_dir, filename)
    print(f"\n=======================================================")
    print(f"FILE: {filename}")
    print(f"=======================================================")
    if os.path.exists(filepath):
        with open(filepath, "r", encoding="utf-8-sig", errors="ignore") as f:
            print(f.read())
    else:
        print(f"FILE NOT FOUND: {filepath}")
