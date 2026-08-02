$webroot = "C:\Users\Isaac\.gemini\antigravity\scratch\frederman\EnterpriseBillingSystem.WebApi\wwwroot"
Get-ChildItem -Recurse $webroot | Where-Object { $_.Extension -in '.png','.jpg','.jpeg','.webp' } | Select-Object FullName, Name, Length | Select-Object -First 30
