namespace EnterpriseBillingSystem.Domain.Enums;

/// <summary>
/// Categoría de vendedor para determinar el canal comercial y la regla de asignación de precios.
/// </summary>
public enum SellerCategory
{
    /// <summary>Vendedor Detalle — maneja los 3 precios de venta (Retail, SemiWholesale, Wholesale).</summary>
    Detail = 0,

    /// <summary>Vendedor Costo — maneja exclusivamente el precio Costo (ProductPresentation.Cost).</summary>
    Cost = 1
}
