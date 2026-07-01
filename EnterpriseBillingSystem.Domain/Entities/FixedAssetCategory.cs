using System.Collections.Generic;
using EnterpriseBillingSystem.Domain.Common;

namespace EnterpriseBillingSystem.Domain.Entities;

public class FixedAssetCategory : GlobalAuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>Código contable de la cuenta de Activo Fijo (ej: 1310)</summary>
    public string AssetAccountCode { get; set; } = string.Empty;

    /// <summary>Código contable de Depreciación Acumulada (ej: 1310.1)</summary>
    public string AccumulatedDepreciationAccountCode { get; set; } = string.Empty;

    /// <summary>Código contable de Gasto de Depreciación (ej: 5200)</summary>
    public string DepreciationExpenseAccountCode { get; set; } = string.Empty;

    /// <summary>Vida útil estándar en meses para esta categoría</summary>
    public int UsefulLifeMonths { get; set; }

    public bool IsActive { get; set; } = true;

    // Navegaciones
    public virtual ICollection<FixedAsset> FixedAssets { get; set; } = new List<FixedAsset>();
}
