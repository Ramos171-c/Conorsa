using System;

namespace EnterpriseBillingSystem.Wpf.Models;

public class PosPaymentDto
{
    public Guid PaymentMethodId { get; set; }
    public string PaymentMethodName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? ReferenceNumber { get; set; }
}
