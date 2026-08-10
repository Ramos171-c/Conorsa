import os

sales_dir = r"C:\Users\Isaac\.gemini\antigravity\scratch\frederman\EnterpriseBillingSystem.Application\Sales\Commands"
inventory_dir = r"C:\Users\Isaac\.gemini\antigravity\scratch\frederman\EnterpriseBillingSystem.Application\Inventory\Commands"

files_to_inspect = [
    os.path.join(sales_dir, "UpdateSalesOrderStatusCommand.cs"),
    os.path.join(sales_dir, "ConfirmSalesOrderCommand.cs"),
    os.path.join(sales_dir, "CreateSalesOrderCommand.cs"),
    os.path.join(sales_dir, "UpdateSalesOrderCommand.cs"),
    os.path.join(sales_dir, "CancelSalesOrderCommand.cs"),
    os.path.join(sales_dir, "ReturnSalesOrderCommand.cs"),
    os.path.join(sales_dir, "CreateSalesInvoiceCommand.cs"),
    os.path.join(inventory_dir, "AdjustInventoryCommand.cs"),
    os.path.join(inventory_dir, "TransferInventoryCommand.cs"),
]

for filepath in files_to_inspect:
    if os.path.exists(filepath):
        print(f"\n=======================================================")
        print(f"FILE: {os.path.basename(filepath)}")
        print(f"=======================================================")
        with open(filepath, "r", encoding="utf-8-sig", errors="ignore") as f:
            print(f.read())
    else:
        print(f"FILE NOT FOUND: {filepath}")
