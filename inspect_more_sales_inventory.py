import os

root_dir = r"C:\Users\Isaac\.gemini\antigravity\scratch\frederman"

files_to_read = [
    r"EnterpriseBillingSystem.Application\Sales\Commands\UpdateSalesOrderStatusCommand.cs",
    r"EnterpriseBillingSystem.Application\Sales\Commands\CleanupZeroStockEnCaminoOrdersCommand.cs",
    r"EnterpriseBillingSystem.Application\Sales\Commands\ResetInventoryAndOrdersCommand.cs",
    r"EnterpriseBillingSystem.WebApi\Controllers\SalesOrdersController.cs",
    r"EnterpriseBillingSystem.WebApi\Controllers\InventoryController.cs",
    r"EnterpriseBillingSystem.WebApi\Controllers\SystemController.cs",
]

for rel in files_to_read:
    filepath = os.path.join(root_dir, rel)
    print(f"\n=======================================================")
    print(f"FILE: {rel}")
    print(f"=======================================================")
    if os.path.exists(filepath):
        with open(filepath, "r", encoding="utf-8-sig", errors="ignore") as f:
            print(f.read())
    else:
        print("FILE NOT FOUND!")
