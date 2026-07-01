using System;

namespace EnterpriseBillingSystem.Wpf.Models;

public class ActiveCashSessionDto
{
    public Guid Id { get; set; }
    public string SessionNumber { get; set; } = string.Empty;
    public Guid CashRegisterId { get; set; }
    public string CashRegisterName { get; set; } = string.Empty;
    public string OpenedByUserName { get; set; } = string.Empty;
    public decimal OpeningAmount { get; set; }
    public DateTime OpenedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
