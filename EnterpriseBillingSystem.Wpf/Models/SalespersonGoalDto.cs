using System;

namespace EnterpriseBillingSystem.Wpf.Models;

public class SalespersonGoalDto
{
    public int Rank { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public decimal Goal { get; set; }
    public decimal Sales { get; set; }
    public double ProgressPercentage { get; set; }
    public string SalesStatusColor { get; set; } = "#E65100";
    public int CustomerGoal { get; set; }
    public int CustomersRegistered { get; set; }
    public double CustomerProgressPercentage { get; set; }
    public string CustomerStatusColor { get; set; } = "#E65100";
    public int TotalOrders { get; set; }
    public decimal AverageTicket { get; set; }
    public string TopProduct { get; set; } = "Varios";

    // Non-persistent base values for dynamic calculation
    public decimal BaseSales { get; set; }
    public int BaseOrders { get; set; }
}
