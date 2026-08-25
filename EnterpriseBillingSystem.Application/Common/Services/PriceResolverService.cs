using System;
using EnterpriseBillingSystem.Application.Common.Interfaces;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Application.Common.Services;

public class PriceResolverService : IPriceResolverService
{
    public decimal ResolveUnitPrice(ProductPresentation presentation, SellerCategory sellerCategory, CustomerPricingType customerPricingType)
    {
        if (presentation == null) throw new ArgumentNullException(nameof(presentation));

        // Regla 1: Vendedor Costo -> Utiliza exclusivamente ProductPresentation.Cost + 2% lineal
        if (sellerCategory == SellerCategory.Cost)
        {
            return Math.Round(presentation.Cost * 1.02m, 2);
        }

        // Regla 2: Vendedor Detalle -> Evalúa el tipo de cliente (Retail, SemiWholesale, Wholesale)
        return customerPricingType switch
        {
            CustomerPricingType.Retail => presentation.RetailPrice,
            CustomerPricingType.SemiWholesale => presentation.SemiWholesalePrice,
            CustomerPricingType.Wholesale => presentation.WholesalePrice,
            _ => presentation.RetailPrice
        };
    }

    public bool IsPresentationAvailableForSeller(ProductPresentation presentation, SellerCategory sellerCategory, bool isAdmin)
    {
        if (presentation == null) return false;
        if (!presentation.IsActive || !presentation.AllowSale) return false;

        // El Administrador tiene visibilidad total de todas las presentaciones activas
        if (isAdmin)
        {
            return true;
        }

        // Validación por canal comercial del vendedor
        return sellerCategory switch
        {
            SellerCategory.Detail => presentation.AllowDetailChannel,
            SellerCategory.Cost => presentation.AllowCostChannel,
            _ => presentation.AllowDetailChannel
        };
    }
}
