namespace EnterpriseBillingSystem.Domain.Enums;

public enum MovementType
{
    Entry = 1,              // Entrada (e.g. compras, devoluciones)
    Exit = 2,               // Salida (e.g. ventas, mermas)
    PositiveAdjustment = 3,  // Ajuste positivo
    NegativeAdjustment = 4,  // Ajuste negativo
    TransferOut = 5,        // Transferencia - Salida de bodega origen
    TransferIn = 6,         // Transferencia - Entrada a bodega destino
    Sale = 7,               // Salida por venta (factura confirmada)
    SaleReversal = 8        // Reversión de venta (anulación de factura)
}
