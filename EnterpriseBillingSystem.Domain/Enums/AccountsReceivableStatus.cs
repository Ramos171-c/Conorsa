namespace EnterpriseBillingSystem.Domain.Enums;

public enum AccountsReceivableStatus
{
    Pending = 1,        // Pendiente de pago sin abonos
    PartiallyPaid = 2,  // Con abonos parciales, saldo > 0
    Paid = 3,           // Completamente pagada, saldo = 0
    Overdue = 4,        // Vencida (hoy > DueDate y saldo > 0)
    Cancelled = 5       // Anulada (por anulación de factura)
}
