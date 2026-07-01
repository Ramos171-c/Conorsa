using System;

namespace EnterpriseBillingSystem.Wpf.Models;

public class ProductSearchResultDto
{
    public Guid Id { get; set; }
    public string InternalCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DefaultSalePrice { get; set; }
    public Guid DefaultUnitOfMeasureId { get; set; }
    public string DefaultUnitOfMeasureCode { get; set; } = "UND";
    public decimal TaxPercentage { get; set; } = 16.00m;
    public bool TrackInventory { get; set; }
    public decimal AvailableStock { get; set; }
}
