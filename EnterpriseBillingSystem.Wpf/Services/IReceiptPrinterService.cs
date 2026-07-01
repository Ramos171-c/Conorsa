using System;
using System.Threading.Tasks;

namespace EnterpriseBillingSystem.Wpf.Services;

public interface IReceiptPrinterService
{
    Task PrintReceiptAsync(Guid invoiceId, string printerType);
}
