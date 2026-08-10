using System;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Application.Common.Interfaces;

public interface IPriceResolverService
{
    /// <summary>
    /// Resuelve el precio de venta exacto según la categoría del vendedor, la presentación y el tipo de cliente.
    /// </summary>
    /// <param name="presentation">Presentación del producto seleccionada.</param>
    /// <param name="sellerCategory">Categoría del vendedor (Detail o Cost).</param>
    /// <param name="customerPricingType">Perfil de precios del cliente (Retail, SemiWholesale, Wholesale).</param>
    /// <returns>Precio unitario resultante para la venta.</returns>
    decimal ResolveUnitPrice(ProductPresentation presentation, SellerCategory sellerCategory, CustomerPricingType customerPricingType);

    /// <summary>
    /// Determina si una presentación está disponible para ser vendida según la categoría del vendedor.
    /// </summary>
    /// <param name="presentation">Presentación a evaluar.</param>
    /// <param name="sellerCategory">Categoría del vendedor.</param>
    /// <param name="isAdmin">Indica si el usuario evaluado tiene rol de Administrador o SuperAdmin.</param>
    /// <returns>True si la presentación está permitida para el canal del vendedor; false en caso contrario.</returns>
    bool IsPresentationAvailableForSeller(ProductPresentation presentation, SellerCategory sellerCategory, bool isAdmin);
}
