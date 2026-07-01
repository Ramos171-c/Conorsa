using System;

namespace EnterpriseBillingSystem.Domain.Common;

public interface IGlobalAuditable
{
    string? CreatedBy { get; set; }
    DateTime CreatedOnUtc { get; set; }
    string? LastModifiedBy { get; set; }
    DateTime? LastModifiedOnUtc { get; set; }
}
