import json

# Proposal breakdown for the 4 diaper families:
families = [
    {
        "parent_code": "TA007",
        "family_name": "BOLSON DE PAÑALES CALSON OSITO",
        "packaging": "U/E: 1/4",
        "cost": 487.50,
        "retail_unit": 560.34,
        "retail_caja": 2241.38,
        "wholesale_unit": 541.67,
        "wholesale_caja": 2166.67,
        "sizes": ["S", "M", "L", "XL", "XXL", "3XL", "4XL"],
        "proposed_skus": [
            ("TA007-S", "BOLSON DE PAÑALES CALSON OSITO - TALLA S"),
            ("TA007-M", "BOLSON DE PAÑALES CALSON OSITO - TALLA M"),
            ("TA007-L", "BOLSON DE PAÑALES CALSON OSITO - TALLA L"),
            ("TA007-XL", "BOLSON DE PAÑALES CALSON OSITO - TALLA XL"),
            ("TA007-XXL", "BOLSON DE PAÑALES CALSON OSITO - TALLA XXL"),
            ("TA007-3XL", "BOLSON DE PAÑALES CALSON OSITO - TALLA 3XL"),
            ("TA007-4XL", "BOLSON DE PAÑALES CALSON OSITO - TALLA 4XL"),
        ]
    },
    {
        "parent_code": "TA008",
        "family_name": "BOLSON DE PAÑALES PEGA PEGA OSITO",
        "packaging": "U/E: 1/4",
        "cost": 487.50,
        "retail_unit": 560.34,
        "retail_caja": 2241.38,
        "wholesale_unit": 541.67,
        "wholesale_caja": 2166.67,
        "sizes": ["S", "M", "L", "XL", "XXL", "XXXL"],
        "proposed_skus": [
            ("TA008-S", "BOLSON DE PAÑALES PEGA PEGA OSITO - TALLA S"),
            ("TA008-M", "BOLSON DE PAÑALES PEGA PEGA OSITO - TALLA M"),
            ("TA008-L", "BOLSON DE PAÑALES PEGA PEGA OSITO - TALLA L"),
            ("TA008-XL", "BOLSON DE PAÑALES PEGA PEGA OSITO - TALLA XL"),
            ("TA008-XXL", "BOLSON DE PAÑALES PEGA PEGA OSITO - TALLA XXL"),
            ("TA008-XXXL", "BOLSON DE PAÑALES PEGA PEGA OSITO - TALLA XXXL"),
        ]
    },
    {
        "parent_code": "TA011",
        "family_name": "PAQUETE DE PAÑAL NIÑO MIDDAY BEAR CALSON",
        "packaging": "U/E: 1*4*50",
        "cost": 487.50,
        "retail_unit": 560.34,
        "retail_caja": 2241.38,
        "wholesale_unit": 541.67,
        "wholesale_caja": 2166.67,
        "sizes": ["M", "L", "XL", "XXL", "XXXL", "4XL", "5XL", "6XL"],
        "proposed_skus": [
            ("TA011-M", "PAQUETE DE PAÑAL NIÑO MIDDAY BEAR CALSON - TALLA M"),
            ("TA011-L", "PAQUETE DE PAÑAL NIÑO MIDDAY BEAR CALSON - TALLA L"),
            ("TA011-XL", "PAQUETE DE PAÑAL NIÑO MIDDAY BEAR CALSON - TALLA XL"),
            ("TA011-XXL", "PAQUETE DE PAÑAL NIÑO MIDDAY BEAR CALSON - TALLA XXL"),
            ("TA011-XXXL", "PAQUETE DE PAÑAL NIÑO MIDDAY BEAR CALSON - TALLA XXXL"),
            ("TA011-4XL", "PAQUETE DE PAÑAL NIÑO MIDDAY BEAR CALSON - TALLA 4XL"),
            ("TA011-5XL", "PAQUETE DE PAÑAL NIÑO MIDDAY BEAR CALSON - TALLA 5XL"),
            ("TA011-6XL", "PAQUETE DE PAÑAL NIÑO MIDDAY BEAR CALSON - TALLA 6XL"),
        ]
    },
    {
        "parent_code": "TA012",
        "family_name": "PAÑAL LUCAS SUPER SET",
        "packaging": "U/E: 1*4*50",
        "cost": 250.00,
        "retail_unit": 287.36,
        "retail_caja": 1149.43,
        "wholesale_unit": 277.78,
        "wholesale_caja": 1111.11,
        "sizes": ["S", "M", "L", "XL", "XXL"],
        "proposed_skus": [
            ("TA012-S", "PAÑAL LUCAS SUPER SET - TALLA S"),
            ("TA012-M", "PAÑAL LUCAS SUPER SET - TALLA M"),
            ("TA012-L", "PAÑAL LUCAS SUPER SET - TALLA L"),
            ("TA012-XL", "PAÑAL LUCAS SUPER SET - TALLA XL"),
            ("TA012-XXL", "PAÑAL LUCAS SUPER SET - TALLA XXL"),
        ]
    }
]

print("=== PROPOSAL SUMMARY FOR DIAPER SKUS ===")
total_skus = 0
for fam in families:
    print(f"\n--- FAMILIA {fam['parent_code']}: {fam['family_name']} ({len(fam['proposed_skus'])} tallas) ---")
    print(f"Costo: C${fam['cost']:,.2f} | Unidad: C${fam['retail_unit']:,.2f} (Mayoreo: C${fam['wholesale_unit']:,.2f}) | Caja: C${fam['retail_caja']:,.2f} (Mayoreo: C${fam['wholesale_caja']:,.2f})")
    for sku, name in fam["proposed_skus"]:
        total_skus += 1
        print(f"  * Código: {sku:<12} | Nombre: {name}")

print(f"\nTotal nuevos productos por talla a crear: {total_skus} SKUs")
