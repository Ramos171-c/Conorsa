using System;

namespace EnterpriseBillingSystem.Wpf.Models;

public class PaymentMethodDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsCash { get; set; }
    public bool IsActive { get; set; }
}
