using System;
using System.Collections.Generic;
using EnterpriseBillingSystem.Domain.Common;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Domain.Entities;

public class FixedAsset : AuditableEntity
{
    /// <summary>Número correlativo único del activo (ej: ACT-2026-0001)</summary>
    public string AssetNumber { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid FixedAssetCategoryId { get; set; }
    public virtual FixedAssetCategory Category { get; set; } = null!;

    /// <summary>Factura de compra que originó el activo (opcional)</summary>
    public Guid? PurchaseInvoiceId { get; set; }
    public virtual PurchaseInvoice? PurchaseInvoice { get; set; }

    public DateTime AcquisitionDate { get; set; }
    public decimal AcquisitionCost { get; set; }
    public decimal ResidualValue { get; set; }

    /// <summary>Vida útil real del activo en meses</summary>
    public int UsefulLifeMonths { get; set; }

    /// <summary>Fecha desde la que comienza a depreciar</summary>
    public DateTime DepreciationStartDate { get; set; }

    public decimal AccumulatedDepreciation { get; set; }

    /// <summary>Valor libro actual = AcquisitionCost - AccumulatedDepreciation + revalorizaciones</summary>
    public decimal CurrentBookValue { get; set; }

    public FixedAssetStatus Status { get; set; } = FixedAssetStatus.Active;

    public string? Location { get; set; }
    public string? SerialNumber { get; set; }
    public string? Notes { get; set; }

    public DateTime? LastDepreciationDate { get; set; }

    /// <summary>Control de concurrencia optimista</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Navegaciones
    public virtual ICollection<FixedAssetTransaction> Transactions { get; set; } = new List<FixedAssetTransaction>();
}
