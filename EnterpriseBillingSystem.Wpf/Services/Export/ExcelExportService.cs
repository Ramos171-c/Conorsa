using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using EnterpriseBillingSystem.Wpf.Models;

namespace EnterpriseBillingSystem.Wpf.Services.Export;

public static class ExcelExportService
{
    public static void ExportConsolidationToExcel(IEnumerable<ConsolidatedProductDto> products, string filePath, string generalObservations = "")
    {
        var productList = products.ToList();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Consolidado de Compras");

        ws.ShowGridLines = true;

        // 1. Encabezado de la Empresa y Titulo del Reporte
        var titleRange = ws.Range("A1:L2");
        titleRange.Merge();
        titleRange.Value = "EMPRESA BILLING SYSTEM - REPORTE CONSOLIDADO DE COMPRAS Y VENTAS";
        titleRange.Style.Font.SetBold(true);
        titleRange.Style.Font.SetFontSize(16);
        titleRange.Style.Font.SetFontColor(XLColor.White);
        titleRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#1E293B")); // Slate Dark Navy
        titleRange.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        titleRange.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);

        var subtitleRange = ws.Range("A3:L3");
        subtitleRange.Merge();
        subtitleRange.Value = $"Bodega Principal Corporativa | Fecha: {DateTime.Now:dd/MM/yyyy HH:mm} | Detalle Exacto Financiero (Venta vs Compra)";
        subtitleRange.Style.Font.SetFontSize(10);
        subtitleRange.Style.Font.SetFontColor(XLColor.FromHtml("#94A3B8"));
        subtitleRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#0F172A"));
        subtitleRange.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        subtitleRange.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);

        // 2. Encabezados de la Tabla (Fila 5)
        int headerRow = 5;
        string[] headers = new[]
        {
            "Código",
            "Producto",
            "U.M.",
            "Cant. Solicitada",
            "Stock Deducido",
            "NETO A PEDIR",
            "Costo Unit. Compra",
            "TOTAL COMPRA ($)",
            "Precio Unit. Venta",
            "TOTAL VENTA ($)",
            "DIFERENCIA GANANCIA ($)",
            "Observaciones"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.SetBold(true);
            cell.Style.Font.SetFontSize(10);
            cell.Style.Font.SetFontColor(XLColor.White);
            cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#0F172A"));
            cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            cell.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            cell.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            cell.Style.Border.SetOutsideBorderColor(XLColor.FromHtml("#334155"));
        }
        ws.Row(headerRow).Height = 26;

        // 3. Filas de Datos
        int currentRow = 6;
        int dataStartRow = currentRow;

        foreach (var item in productList)
        {
            ws.Cell(currentRow, 1).Value = item.ProductCode;
            ws.Cell(currentRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Cell(currentRow, 1).Style.Font.SetBold(true);

            ws.Cell(currentRow, 2).Value = item.ProductName;
            ws.Cell(currentRow, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);

            ws.Cell(currentRow, 3).Value = item.UnitOfMeasure;
            ws.Cell(currentRow, 3).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Cantidad Solicitada (Bruta)
            ws.Cell(currentRow, 4).Value = item.TotalQuantity;
            ws.Cell(currentRow, 4).Style.NumberFormat.SetFormat("#,##0.00");
            ws.Cell(currentRow, 4).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

            // Stock Deducido de Inventario
            ws.Cell(currentRow, 5).Value = item.DeductedFromInventory;
            ws.Cell(currentRow, 5).Style.NumberFormat.SetFormat("#,##0.00");
            ws.Cell(currentRow, 5).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            if (item.DeductedFromInventory > 0)
            {
                ws.Cell(currentRow, 5).Style.Font.SetFontColor(XLColor.FromHtml("#15803D"));
                ws.Cell(currentRow, 5).Style.Font.SetBold(true);
            }

            // NETO A PEDIR (Resaltado)
            ws.Cell(currentRow, 6).Value = item.NetQuantityToOrder;
            ws.Cell(currentRow, 6).Style.NumberFormat.SetFormat("#,##0.00");
            ws.Cell(currentRow, 6).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            ws.Cell(currentRow, 6).Style.Font.SetBold(true);
            ws.Cell(currentRow, 6).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#FEF3C7")); // Soft Amber Fill

            // Costo Unitario de Compra
            ws.Cell(currentRow, 7).Value = item.UnitCost;
            ws.Cell(currentRow, 7).Style.NumberFormat.SetFormat("$#,##0.00");
            ws.Cell(currentRow, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

            // TOTAL COMPRA PROVEEDOR ($)
            ws.Cell(currentRow, 8).Value = item.TotalPurchaseCost;
            ws.Cell(currentRow, 8).Style.NumberFormat.SetFormat("$#,##0.00");
            ws.Cell(currentRow, 8).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            ws.Cell(currentRow, 8).Style.Font.SetBold(true);
            ws.Cell(currentRow, 8).Style.Font.SetFontColor(XLColor.FromHtml("#B45309"));

            // Precio Unitario Venta
            ws.Cell(currentRow, 9).Value = item.UnitPrice;
            ws.Cell(currentRow, 9).Style.NumberFormat.SetFormat("$#,##0.00");
            ws.Cell(currentRow, 9).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

            // TOTAL VENTA CLIENTE ($)
            ws.Cell(currentRow, 10).Value = item.NetSalesAmount;
            ws.Cell(currentRow, 10).Style.NumberFormat.SetFormat("$#,##0.00");
            ws.Cell(currentRow, 10).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            ws.Cell(currentRow, 10).Style.Font.SetBold(true);
            ws.Cell(currentRow, 10).Style.Font.SetFontColor(XLColor.FromHtml("#1E40AF"));

            // DIFERENCIA / GANANCIA ($)
            ws.Cell(currentRow, 11).Value = item.ProfitMarginAmount;
            ws.Cell(currentRow, 11).Style.NumberFormat.SetFormat("$#,##0.00");
            ws.Cell(currentRow, 11).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            ws.Cell(currentRow, 11).Style.Font.SetBold(true);
            ws.Cell(currentRow, 11).Style.Font.SetFontColor(XLColor.FromHtml("#15803D"));

            // Observaciones de la fila
            ws.Cell(currentRow, 12).Value = item.Observation;
            ws.Cell(currentRow, 12).Style.Font.SetItalic(true);
            ws.Cell(currentRow, 12).Style.Font.SetFontSize(9);

            // Zebra Striping & Borders
            var rowRange = ws.Range(currentRow, 1, currentRow, 12);
            if (currentRow % 2 == 1)
            {
                for (int col = 1; col <= 12; col++)
                {
                    if (col != 6) // Conservar el relleno de Neto a Pedir
                    {
                        ws.Cell(currentRow, col).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#F8FAFC"));
                    }
                }
            }

            foreach (var cell in rowRange.Cells())
            {
                cell.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                cell.Style.Border.SetOutsideBorderColor(XLColor.FromHtml("#E2E8F0"));
            }

            currentRow++;
        }

        int dataEndRow = currentRow - 1;

        // 4. RECALCAR EN LA PARTE DE ABAJO DEL EXCEL (Sección de Totales y Resumen Ejecutivo)
        currentRow += 2;

        var summaryTitleRange = ws.Range(currentRow, 1, currentRow, 12);
        summaryTitleRange.Merge();
        summaryTitleRange.Value = "RESUMEN EJECUTIVO Y TOTALES RECALCADOS DE CONSOLIDACIÓN (SIN QUE FALTE UN CENTAVO)";
        summaryTitleRange.Style.Font.SetBold(true);
        summaryTitleRange.Style.Font.SetFontSize(12);
        summaryTitleRange.Style.Font.SetFontColor(XLColor.White);
        summaryTitleRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#1E293B"));
        summaryTitleRange.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        summaryTitleRange.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        ws.Row(currentRow).Height = 24;
        currentRow++;

        // Fila Totales Solicitados (Brutos)
        ws.Range(currentRow, 1, currentRow, 5).Merge();
        ws.Cell(currentRow, 1).Value = "TOTAL PIEZAS SOLICITADAS POR CLIENTES (BRUTO):";
        ws.Cell(currentRow, 1).Style.Font.SetBold(true);
        ws.Cell(currentRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        ws.Cell(currentRow, 6).FormulaA1 = dataEndRow >= dataStartRow ? $"SUM(D{dataStartRow}:D{dataEndRow})" : "0";
        ws.Cell(currentRow, 6).Style.NumberFormat.SetFormat("#,##0.00");
        ws.Cell(currentRow, 6).Style.Font.SetBold(true);
        ws.Cell(currentRow, 6).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        ws.Cell(currentRow, 6).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#F1F5F9"));
        ws.Range(currentRow, 1, currentRow, 12).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
        currentRow++;

        // Fila Totales Deducidos de Inventario (Existencia en Bodega)
        ws.Range(currentRow, 1, currentRow, 5).Merge();
        ws.Cell(currentRow, 1).Value = "EXISTENCIA EN BODEGA APLICADA/DEDUCIDA (NO SE RE-PIDE):";
        ws.Cell(currentRow, 1).Style.Font.SetBold(true);
        ws.Cell(currentRow, 1).Style.Font.SetFontColor(XLColor.FromHtml("#15803D"));
        ws.Cell(currentRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        ws.Cell(currentRow, 6).FormulaA1 = dataEndRow >= dataStartRow ? $"SUM(E{dataStartRow}:E{dataEndRow})" : "0";
        ws.Cell(currentRow, 6).Style.NumberFormat.SetFormat("#,##0.00");
        ws.Cell(currentRow, 6).Style.Font.SetBold(true);
        ws.Cell(currentRow, 6).Style.Font.SetFontColor(XLColor.FromHtml("#15803D"));
        ws.Cell(currentRow, 6).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        ws.Cell(currentRow, 6).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#DCFCE7"));
        
        // Valor de Costo y Venta cubierto por el inventario disponible
        decimal invPurchaseVal = productList.Sum(p => p.InventoryDeductedPurchaseCost);
        decimal invSalesVal = productList.Sum(p => p.InventoryDeductedSalesAmount);
        ws.Cell(currentRow, 8).Value = invPurchaseVal;
        ws.Cell(currentRow, 8).Style.NumberFormat.SetFormat("$#,##0.00");
        ws.Cell(currentRow, 8).Style.Font.SetBold(true);
        ws.Cell(currentRow, 8).Style.Font.SetFontColor(XLColor.FromHtml("#15803D"));
        ws.Cell(currentRow, 8).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

        ws.Cell(currentRow, 10).Value = invSalesVal;
        ws.Cell(currentRow, 10).Style.NumberFormat.SetFormat("$#,##0.00");
        ws.Cell(currentRow, 10).Style.Font.SetBold(true);
        ws.Cell(currentRow, 10).Style.Font.SetFontColor(XLColor.FromHtml("#15803D"));
        ws.Cell(currentRow, 10).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        ws.Range(currentRow, 1, currentRow, 12).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
        currentRow++;

        // Fila NETO TOTAL A PEDIR AL PROVEEDOR
        ws.Range(currentRow, 1, currentRow, 5).Merge();
        ws.Cell(currentRow, 1).Value = "NETO REAL DE PIEZAS A SOLICITAR A PROVEEDOR:";
        ws.Cell(currentRow, 1).Style.Font.SetBold(true);
        ws.Cell(currentRow, 1).Style.Font.SetFontSize(12);
        ws.Cell(currentRow, 1).Style.Font.SetFontColor(XLColor.FromHtml("#B45309"));
        ws.Cell(currentRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        ws.Cell(currentRow, 6).FormulaA1 = dataEndRow >= dataStartRow ? $"SUM(F{dataStartRow}:F{dataEndRow})" : "0";
        ws.Cell(currentRow, 6).Style.NumberFormat.SetFormat("#,##0.00");
        ws.Cell(currentRow, 6).Style.Font.SetBold(true);
        ws.Cell(currentRow, 6).Style.Font.SetFontSize(12);
        ws.Cell(currentRow, 6).Style.Font.SetFontColor(XLColor.FromHtml("#B45309"));
        ws.Cell(currentRow, 6).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        ws.Cell(currentRow, 6).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#FEF3C7"));
        ws.Range(currentRow, 1, currentRow, 12).Style.Border.SetOutsideBorder(XLBorderStyleValues.Medium);
        currentRow++;

        // Fila MONTO TOTAL ESTIMADO DE COMPRA AL PROVEEDOR ($)
        ws.Range(currentRow, 1, currentRow, 7).Merge();
        ws.Cell(currentRow, 1).Value = "MONTO TOTAL ESTIMADO DE COMPRA AL PROVEEDOR ($):";
        ws.Cell(currentRow, 1).Style.Font.SetBold(true);
        ws.Cell(currentRow, 1).Style.Font.SetFontSize(12);
        ws.Cell(currentRow, 1).Style.Font.SetFontColor(XLColor.FromHtml("#B45309"));
        ws.Cell(currentRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        ws.Cell(currentRow, 8).FormulaA1 = dataEndRow >= dataStartRow ? $"SUM(H{dataStartRow}:H{dataEndRow})" : "0";
        ws.Cell(currentRow, 8).Style.NumberFormat.SetFormat("$#,##0.00");
        ws.Cell(currentRow, 8).Style.Font.SetBold(true);
        ws.Cell(currentRow, 8).Style.Font.SetFontSize(12);
        ws.Cell(currentRow, 8).Style.Font.SetFontColor(XLColor.FromHtml("#B45309"));
        ws.Cell(currentRow, 8).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        ws.Cell(currentRow, 8).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#FEF3C7"));
        ws.Range(currentRow, 1, currentRow, 12).Style.Border.SetOutsideBorder(XLBorderStyleValues.Medium);
        currentRow++;

        // Fila MONTO TOTAL ESTIMADO DE VENTA A CLIENTES ($)
        ws.Range(currentRow, 1, currentRow, 9).Merge();
        ws.Cell(currentRow, 1).Value = "MONTO TOTAL ESTIMADO DE VENTA A CLIENTES ($):";
        ws.Cell(currentRow, 1).Style.Font.SetBold(true);
        ws.Cell(currentRow, 1).Style.Font.SetFontSize(12);
        ws.Cell(currentRow, 1).Style.Font.SetFontColor(XLColor.FromHtml("#1E40AF"));
        ws.Cell(currentRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        ws.Cell(currentRow, 10).FormulaA1 = dataEndRow >= dataStartRow ? $"SUM(J{dataStartRow}:J{dataEndRow})" : "0";
        ws.Cell(currentRow, 10).Style.NumberFormat.SetFormat("$#,##0.00");
        ws.Cell(currentRow, 10).Style.Font.SetBold(true);
        ws.Cell(currentRow, 10).Style.Font.SetFontSize(12);
        ws.Cell(currentRow, 10).Style.Font.SetFontColor(XLColor.FromHtml("#1E40AF"));
        ws.Cell(currentRow, 10).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        ws.Cell(currentRow, 10).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#DBEAFE"));
        ws.Range(currentRow, 1, currentRow, 12).Style.Border.SetOutsideBorder(XLBorderStyleValues.Medium);
        currentRow++;

        // Fila DIFERENCIA / MARGEN DE GANANCIA BRUTA ($)
        ws.Range(currentRow, 1, currentRow, 10).Merge();
        ws.Cell(currentRow, 1).Value = "DIFERENCIA / MARGEN DE GANANCIA ESTIMADO (VENTA - COMPRA):";
        ws.Cell(currentRow, 1).Style.Font.SetBold(true);
        ws.Cell(currentRow, 1).Style.Font.SetFontSize(13);
        ws.Cell(currentRow, 1).Style.Font.SetFontColor(XLColor.FromHtml("#166534"));
        ws.Cell(currentRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        ws.Cell(currentRow, 11).FormulaA1 = dataEndRow >= dataStartRow ? $"SUM(K{dataStartRow}:K{dataEndRow})" : "0";
        ws.Cell(currentRow, 11).Style.NumberFormat.SetFormat("$#,##0.00");
        ws.Cell(currentRow, 11).Style.Font.SetBold(true);
        ws.Cell(currentRow, 11).Style.Font.SetFontSize(13);
        ws.Cell(currentRow, 11).Style.Font.SetFontColor(XLColor.FromHtml("#166534"));
        ws.Cell(currentRow, 11).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        ws.Cell(currentRow, 11).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#DCFCE7"));
        ws.Range(currentRow, 1, currentRow, 12).Style.Border.SetOutsideBorder(XLBorderStyleValues.Medium);
        currentRow++;

        // 5. SECCIÓN DE OBSERVACIONES GENERALES DE LA CONSOLIDACIÓN
        currentRow += 2;
        var obsHeaderRange = ws.Range(currentRow, 1, currentRow, 12);
        obsHeaderRange.Merge();
        obsHeaderRange.Value = "OBSERVACIONES Y NOTAS GENERALES DE LA CONSOLIDACIÓN:";
        obsHeaderRange.Style.Font.SetBold(true);
        obsHeaderRange.Style.Font.SetFontSize(11);
        obsHeaderRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#E2E8F0"));
        obsHeaderRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
        currentRow++;

        var obsBoxRange = ws.Range(currentRow, 1, currentRow + 2, 12);
        obsBoxRange.Merge();
        obsBoxRange.Value = string.IsNullOrWhiteSpace(generalObservations)
            ? "Sin observaciones adicionales registradas para este reporte de consolidación."
            : generalObservations;
        obsBoxRange.Style.Font.SetItalic(true);
        obsBoxRange.Style.Font.SetFontSize(10);
        obsBoxRange.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
        obsBoxRange.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Top);
        obsBoxRange.Style.Alignment.SetWrapText(true);
        obsBoxRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#F8FAFC"));
        obsBoxRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

        // Auto-ajustar ancho de columnas
        ws.Columns().AdjustToContents();
        ws.Column(1).Width = Math.Max(ws.Column(1).Width, 14);
        ws.Column(2).Width = Math.Max(ws.Column(2).Width, 35);
        ws.Column(4).Width = Math.Max(ws.Column(4).Width, 18);
        ws.Column(5).Width = Math.Max(ws.Column(5).Width, 18);
        ws.Column(6).Width = Math.Max(ws.Column(6).Width, 18);
        ws.Column(8).Width = Math.Max(ws.Column(8).Width, 20);
        ws.Column(10).Width = Math.Max(ws.Column(10).Width, 20);
        ws.Column(11).Width = Math.Max(ws.Column(11).Width, 22);
        ws.Column(12).Width = Math.Max(ws.Column(12).Width, 30);

        workbook.SaveAs(filePath);
    }
}
