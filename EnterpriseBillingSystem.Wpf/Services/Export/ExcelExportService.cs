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

            ws.Cell(currentRow, 3).Value = item.FullUnitOfMeasure;
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
            ws.Cell(currentRow, 10).Value = item.DisplayTotalSales;
            ws.Cell(currentRow, 10).Style.NumberFormat.SetFormat("$#,##0.00");
            ws.Cell(currentRow, 10).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            ws.Cell(currentRow, 10).Style.Font.SetBold(true);
            ws.Cell(currentRow, 10).Style.Font.SetFontColor(XLColor.FromHtml("#1E40AF"));

            // DIFERENCIA / GANANCIA ($)
            ws.Cell(currentRow, 11).Value = item.DisplayProfit;
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
        currentRow += 3;

        // =========================================================================
        // NUEVA SECCIÓN INMEDIATAMENTE DEBAJO: PEDIDO DE COMPRA SUGERIDO AL PROVEEDOR
        // =========================================================================
        var poTitleSectionRange = ws.Range(currentRow, 1, currentRow + 1, 12);
        poTitleSectionRange.Merge();
        poTitleSectionRange.Value = "NUEVA SECCIÓN: PEDIDO DE COMPRA SUGERIDO AL PROVEEDOR (EMPAQUES Y CAJAS COMPLETAS)";
        poTitleSectionRange.Style.Font.SetBold(true);
        poTitleSectionRange.Style.Font.SetFontSize(14);
        poTitleSectionRange.Style.Font.SetFontColor(XLColor.White);
        poTitleSectionRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#92400E")); // Amber Dark
        poTitleSectionRange.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        poTitleSectionRange.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        currentRow += 2;

        var poSubTitleSectionRange = ws.Range(currentRow, 1, currentRow, 12);
        poSubTitleSectionRange.Merge();
        poSubTitleSectionRange.Value = "Cálculo Automático por Empaques de Proveedor | Redondeo Superior CEILING(Unidades Requeridas / Contenido por Caja)";
        poSubTitleSectionRange.Style.Font.SetFontSize(10);
        poSubTitleSectionRange.Style.Font.SetFontColor(XLColor.FromHtml("#FEF3C7"));
        poSubTitleSectionRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#78350F"));
        poSubTitleSectionRange.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        poSubTitleSectionRange.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        currentRow += 2;

        int poHeaderRowSheet1 = currentRow;
        string[] poHeadersSheet1 = new[]
        {
            "Proveedor",
            "Código",
            "Producto",
            "U. Compra",
            "PEDIR (CAJAS)",
            "Requeridas (Unids)",
            "Total Unidades",
            "Observaciones del Vendedor",
            "", "", "", ""
        };

        ws.Range(poHeaderRowSheet1, 8, poHeaderRowSheet1, 12).Merge();

        for (int i = 0; i < 8; i++)
        {
            int colIdx = i + 1;
            var cell = ws.Cell(poHeaderRowSheet1, colIdx);
            if (i == 7)
            {
                cell = ws.Cell(poHeaderRowSheet1, 8);
            }
            cell.Value = poHeadersSheet1[i];
            cell.Style.Font.SetBold(true);
            cell.Style.Font.SetFontSize(10);
            cell.Style.Font.SetFontColor(XLColor.White);
            cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#78350F"));
            cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            cell.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            cell.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
        }
        ws.Row(poHeaderRowSheet1).Height = 26;
        currentRow++;

        int poDataStartSheet1 = currentRow;
        var purchaseProducts = productList.Where(p => p.SuggestedBoxesToOrder > 0 || p.NetQuantityToOrder > 0).ToList();
        var groupedBySupplierSheet1 = purchaseProducts.GroupBy(p => p.SupplierName).OrderBy(g => g.Key);

        foreach (var supplierGroup in groupedBySupplierSheet1)
        {
            var supHeader = ws.Range(currentRow, 1, currentRow, 12);
            supHeader.Merge();
            supHeader.Value = $"PROVEEDOR: {supplierGroup.Key.ToUpper()}";
            supHeader.Style.Font.SetBold(true);
            supHeader.Style.Font.SetFontSize(11);
            supHeader.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#FEF3C7"));
            supHeader.Style.Font.SetFontColor(XLColor.FromHtml("#92400E"));
            supHeader.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            currentRow++;

            foreach (var item in supplierGroup)
            {
                ws.Cell(currentRow, 1).Value = item.SupplierName;

                ws.Cell(currentRow, 2).Value = item.ProductCode;
                ws.Cell(currentRow, 2).Style.Font.SetBold(true);
                ws.Cell(currentRow, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws.Cell(currentRow, 3).Value = item.ProductName;

                ws.Cell(currentRow, 4).Value = item.PurchaseUnitName;
                ws.Cell(currentRow, 4).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                // 1. PEDIR (CAJAS) - Destacado en verde (en lugar de Contenido)
                ws.Cell(currentRow, 5).Value = item.SuggestedBoxesToOrder;
                ws.Cell(currentRow, 5).Style.NumberFormat.SetFormat("#,##0");
                ws.Cell(currentRow, 5).Style.Font.SetBold(true);
                ws.Cell(currentRow, 5).Style.Font.SetFontColor(XLColor.FromHtml("#15803D"));
                ws.Cell(currentRow, 5).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#DCFCE7"));
                ws.Cell(currentRow, 5).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

                // 2. Requeridas (Unidades físicas/piezas)
                ws.Cell(currentRow, 6).Value = item.NetQuantityToOrder;
                ws.Cell(currentRow, 6).Style.NumberFormat.SetFormat("#,##0.00");
                ws.Cell(currentRow, 6).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

                // 3. Total Unidades a recibir por cajas completas
                ws.Cell(currentRow, 7).Value = item.SuggestedTotalUnitsToOrder;
                ws.Cell(currentRow, 7).Style.NumberFormat.SetFormat("#,##0.00");
                ws.Cell(currentRow, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                ws.Cell(currentRow, 7).Style.Font.SetBold(true);

                var obsRangeItem = ws.Range(currentRow, 8, currentRow, 12);
                obsRangeItem.Merge();
                obsRangeItem.Value = item.SellerObservations;
                obsRangeItem.Style.Font.SetItalic(true);
                obsRangeItem.Style.Font.SetFontColor(XLColor.FromHtml("#B45309"));

                var rowRange = ws.Range(currentRow, 1, currentRow, 12);
                foreach (var cell in rowRange.Cells())
                {
                    cell.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                    cell.Style.Border.SetOutsideBorderColor(XLColor.FromHtml("#E2E8F0"));
                }
                currentRow++;
            }
        }

        int poDataEndSheet1 = currentRow - 1;

        // Fila Totales de Cajas Sugeridas en Hoja 1
        ws.Range(currentRow, 1, currentRow, 4).Merge();
        ws.Cell(currentRow, 1).Value = "TOTAL DE CAJAS COMPLETAS A SOLICITAR AL PROVEEDOR:";
        ws.Cell(currentRow, 1).Style.Font.SetBold(true);
        ws.Cell(currentRow, 1).Style.Font.SetFontSize(12);
        ws.Cell(currentRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

        ws.Cell(currentRow, 5).FormulaA1 = poDataEndSheet1 >= poDataStartSheet1 ? $"SUM(E{poDataStartSheet1}:E{poDataEndSheet1})" : "0";
        ws.Cell(currentRow, 5).Style.NumberFormat.SetFormat("#,##0");
        ws.Cell(currentRow, 5).Style.Font.SetBold(true);
        ws.Cell(currentRow, 5).Style.Font.SetFontSize(13);
        ws.Cell(currentRow, 5).Style.Font.SetFontColor(XLColor.FromHtml("#15803D"));
        ws.Cell(currentRow, 5).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#DCFCE7"));
        ws.Cell(currentRow, 5).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

        ws.Cell(currentRow, 6).FormulaA1 = poDataEndSheet1 >= poDataStartSheet1 ? $"SUM(F{poDataStartSheet1}:F{poDataEndSheet1})" : "0";
        ws.Cell(currentRow, 6).Style.NumberFormat.SetFormat("#,##0.00");
        ws.Cell(currentRow, 6).Style.Font.SetBold(true);
        ws.Cell(currentRow, 6).Style.Font.SetFontSize(11);
        ws.Cell(currentRow, 6).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

        ws.Cell(currentRow, 7).FormulaA1 = poDataEndSheet1 >= poDataStartSheet1 ? $"SUM(G{poDataStartSheet1}:G{poDataEndSheet1})" : "0";
        ws.Cell(currentRow, 7).Style.NumberFormat.SetFormat("#,##0.00");
        ws.Cell(currentRow, 7).Style.Font.SetBold(true);
        ws.Cell(currentRow, 7).Style.Font.SetFontSize(12);
        ws.Cell(currentRow, 7).Style.Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
        ws.Cell(currentRow, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

        ws.Range(currentRow, 8, currentRow, 12).Merge();
        ws.Range(currentRow, 1, currentRow, 12).Style.Border.SetOutsideBorder(XLBorderStyleValues.Medium);

        // Auto-ajustar ancho de columnas Hoja 1
        ws.Columns().AdjustToContents();
        ws.Column(1).Width = Math.Max(ws.Column(1).Width, 18);
        ws.Column(2).Width = Math.Max(ws.Column(2).Width, 14);
        ws.Column(3).Width = Math.Max(ws.Column(3).Width, 35);
        ws.Column(4).Width = Math.Max(ws.Column(4).Width, 14);
        ws.Column(5).Width = Math.Max(ws.Column(5).Width, 18);
        ws.Column(6).Width = Math.Max(ws.Column(6).Width, 18);
        ws.Column(7).Width = Math.Max(ws.Column(7).Width, 18);
        ws.Column(8).Width = Math.Max(ws.Column(8).Width, 20);
        ws.Column(9).Width = Math.Max(ws.Column(9).Width, 30);

        // =========================================================================
        // HOJA 2: PEDIDO DE COMPRA SUGERIDO AL PROVEEDOR (CAJAS COMPLETAS)
        // =========================================================================
        var ws2 = workbook.Worksheets.Add("Pedido de Compra (Cajas)");
        ws2.ShowGridLines = true;

        var poTitleRange = ws2.Range("A1:I2");
        poTitleRange.Merge();
        poTitleRange.Value = "EMPRESA BILLING SYSTEM - PEDIDO DE COMPRA SUGERIDO AL PROVEEDOR";
        poTitleRange.Style.Font.SetBold(true);
        poTitleRange.Style.Font.SetFontSize(16);
        poTitleRange.Style.Font.SetFontColor(XLColor.White);
        poTitleRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#92400E")); // Amber / Brown
        poTitleRange.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        poTitleRange.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);

        var poSubTitleRange = ws2.Range("A3:I3");
        poSubTitleRange.Merge();
        poSubTitleRange.Value = "Cálculo Automático por Empaques de Proveedor | Redondeo Superior CEILING(Unidades / Caja)";
        poSubTitleRange.Style.Font.SetFontSize(10);
        poSubTitleRange.Style.Font.SetFontColor(XLColor.FromHtml("#FEF3C7"));
        poSubTitleRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#78350F"));
        poSubTitleRange.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        poSubTitleRange.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);

        int poHeaderRow = 5;
        string[] poHeaders = new[]
        {
            "Proveedor",
            "Código",
            "Producto",
            "U. Compra",
            "PEDIR (CAJAS)",
            "Requeridas (Unids)",
            "Total Unidades",
            "Observaciones del Vendedor"
        };

        for (int i = 0; i < poHeaders.Length; i++)
        {
            var cell = ws2.Cell(poHeaderRow, i + 1);
            cell.Value = poHeaders[i];
            cell.Style.Font.SetBold(true);
            cell.Style.Font.SetFontSize(10);
            cell.Style.Font.SetFontColor(XLColor.White);
            cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#78350F"));
            cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            cell.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            cell.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
        }
        ws2.Row(poHeaderRow).Height = 26;

        int poCurrentRow = 6;
        int poDataStartRow = poCurrentRow;

        var groupedBySupplier = purchaseProducts.GroupBy(p => p.SupplierName).OrderBy(g => g.Key);

        foreach (var supplierGroup in groupedBySupplier)
        {
            // Subencabezado de Proveedor
            var supHeader = ws2.Range(poCurrentRow, 1, poCurrentRow, 8);
            supHeader.Merge();
            supHeader.Value = $"PROVEEDOR: {supplierGroup.Key.ToUpper()}";
            supHeader.Style.Font.SetBold(true);
            supHeader.Style.Font.SetFontSize(11);
            supHeader.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#FEF3C7"));
            supHeader.Style.Font.SetFontColor(XLColor.FromHtml("#92400E"));
            supHeader.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            poCurrentRow++;

            foreach (var item in supplierGroup)
            {
                ws2.Cell(poCurrentRow, 1).Value = item.SupplierName;
                ws2.Cell(poCurrentRow, 2).Value = item.ProductCode;
                ws2.Cell(poCurrentRow, 2).Style.Font.SetBold(true);
                ws2.Cell(poCurrentRow, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                ws2.Cell(poCurrentRow, 3).Value = item.ProductName;

                ws2.Cell(poCurrentRow, 4).Value = item.PurchaseUnitName;
                ws2.Cell(poCurrentRow, 4).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                // 1. PEDIR (CAJAS) - Destacado en verde
                ws2.Cell(poCurrentRow, 5).Value = item.SuggestedBoxesToOrder;
                ws2.Cell(poCurrentRow, 5).Style.NumberFormat.SetFormat("#,##0");
                ws2.Cell(poCurrentRow, 5).Style.Font.SetBold(true);
                ws2.Cell(poCurrentRow, 5).Style.Font.SetFontColor(XLColor.FromHtml("#15803D"));
                ws2.Cell(poCurrentRow, 5).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#DCFCE7"));
                ws2.Cell(poCurrentRow, 5).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

                // 2. Requeridas (Unidades físicas/piezas)
                ws2.Cell(poCurrentRow, 6).Value = item.NetQuantityToOrder;
                ws2.Cell(poCurrentRow, 6).Style.NumberFormat.SetFormat("#,##0.00");
                ws2.Cell(poCurrentRow, 6).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

                // 3. Total Unidades a recibir por cajas completas
                ws2.Cell(poCurrentRow, 7).Value = item.SuggestedTotalUnitsToOrder;
                ws2.Cell(poCurrentRow, 7).Style.NumberFormat.SetFormat("#,##0.00");
                ws2.Cell(poCurrentRow, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                ws2.Cell(poCurrentRow, 7).Style.Font.SetBold(true);

                ws2.Cell(poCurrentRow, 8).Value = item.SellerObservations;
                ws2.Cell(poCurrentRow, 8).Style.Font.SetItalic(true);
                ws2.Cell(poCurrentRow, 8).Style.Font.SetFontColor(XLColor.FromHtml("#B45309"));

                var rowRange = ws2.Range(poCurrentRow, 1, poCurrentRow, 9);
                foreach (var cell in rowRange.Cells())
                {
                    cell.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                    cell.Style.Border.SetOutsideBorderColor(XLColor.FromHtml("#E2E8F0"));
                }
                poCurrentRow++;
            }
        }

        int poDataEndRow = poCurrentRow - 1;

        // Fila Total de Cajas Sugeridas
        poCurrentRow++;
        ws2.Range(poCurrentRow, 1, poCurrentRow, 6).Merge();
        ws2.Cell(poCurrentRow, 1).Value = "TOTAL DE CAJAS COMPLETAS A SOLICITAR AL PROVEEDOR:";
        ws2.Cell(poCurrentRow, 1).Style.Font.SetBold(true);
        ws2.Cell(poCurrentRow, 1).Style.Font.SetFontSize(12);
        ws2.Cell(poCurrentRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        ws2.Cell(poCurrentRow, 7).FormulaA1 = poDataEndRow >= poDataStartRow ? $"SUM(G{poDataStartRow}:G{poDataEndRow})" : "0";
        ws2.Cell(poCurrentRow, 7).Style.NumberFormat.SetFormat("#,##0");
        ws2.Cell(poCurrentRow, 7).Style.Font.SetBold(true);
        ws2.Cell(poCurrentRow, 7).Style.Font.SetFontSize(13);
        ws2.Cell(poCurrentRow, 7).Style.Font.SetFontColor(XLColor.FromHtml("#15803D"));
        ws2.Cell(poCurrentRow, 7).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#DCFCE7"));
        ws2.Cell(poCurrentRow, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

        ws2.Cell(poCurrentRow, 8).FormulaA1 = poDataEndRow >= poDataStartRow ? $"SUM(H{poDataStartRow}:H{poDataEndRow})" : "0";
        ws2.Cell(poCurrentRow, 8).Style.NumberFormat.SetFormat("#,##0.00");
        ws2.Cell(poCurrentRow, 8).Style.Font.SetBold(true);
        ws2.Cell(poCurrentRow, 8).Style.Font.SetFontSize(13);
        ws2.Cell(poCurrentRow, 8).Style.Font.SetFontColor(XLColor.FromHtml("#1E3A8A"));
        ws2.Cell(poCurrentRow, 8).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

        ws2.Range(poCurrentRow, 1, poCurrentRow, 9).Style.Border.SetOutsideBorder(XLBorderStyleValues.Medium);

        ws2.Columns().AdjustToContents();
        ws2.Column(1).Width = Math.Max(ws2.Column(1).Width, 20);
        ws2.Column(2).Width = Math.Max(ws2.Column(2).Width, 14);
        ws2.Column(3).Width = Math.Max(ws2.Column(3).Width, 35);
        ws2.Column(5).Width = Math.Max(ws2.Column(5).Width, 18);
        ws2.Column(6).Width = Math.Max(ws2.Column(6).Width, 18);
        ws2.Column(7).Width = Math.Max(ws2.Column(7).Width, 18);
        ws2.Column(8).Width = Math.Max(ws2.Column(8).Width, 18);
        ws2.Column(9).Width = Math.Max(ws2.Column(9).Width, 35);

        workbook.SaveAs(filePath);
    }

    public static void ExportRouteLiquidationToExcel(string routeName, IEnumerable<object> items, string filePath, string observations = "")
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Liquidacion de Ruta");
        ws.ShowGridLines = true;

        var titleRange = ws.Range("A1:H2");
        titleRange.Merge();
        titleRange.Value = $"CONSORSA - LIQUIDACIÓN Y DEVOLUCIÓN DE RUTA: {routeName.ToUpper()}";
        titleRange.Style.Font.SetBold(true);
        titleRange.Style.Font.SetFontSize(15);
        titleRange.Style.Font.SetFontColor(XLColor.White);
        titleRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#6A1B9A"));
        titleRange.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        titleRange.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);

        var subtitleRange = ws.Range("A3:H3");
        subtitleRange.Merge();
        subtitleRange.Value = $"Fecha de Emisión: {DateTime.Now:dd/MM/yyyy HH:mm} | Estado: Borrador de Liquidación";
        subtitleRange.Style.Font.SetFontSize(10);
        subtitleRange.Style.Font.SetFontColor(XLColor.FromHtml("#E1BEE7"));
        subtitleRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#4A148C"));
        subtitleRange.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        int headerRow = 5;
        string[] headers = new[]
        {
            "Código",
            "Producto",
            "Presentación / U.M.",
            "Cant. Enviada",
            "Cant. Retornada",
            "Cant. Vendida",
            "Precio Venta (C$)",
            "Total Venta (C$)"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.SetBold(true);
            cell.Style.Font.SetFontColor(XLColor.White);
            cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#4A148C"));
            cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }

        int currentRow = 6;
        var itemList = items.ToList();
        foreach (dynamic item in itemList)
        {
            ws.Cell(currentRow, 1).Value = item.ProductCode;
            ws.Cell(currentRow, 2).Value = item.ProductName;
            ws.Cell(currentRow, 3).Value = item.SelectedPresentation?.Name ?? "UND";
            ws.Cell(currentRow, 4).Value = item.QuantitySent;
            ws.Cell(currentRow, 5).Value = item.QuantityReturned;
            ws.Cell(currentRow, 6).Value = item.QuantitySold;
            ws.Cell(currentRow, 7).Value = item.SalePrice;
            ws.Cell(currentRow, 8).Value = item.SubtotalSold;

            ws.Cell(currentRow, 4).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(currentRow, 5).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(currentRow, 7).Style.NumberFormat.Format = "C$ #,##0.00";
            ws.Cell(currentRow, 8).Style.NumberFormat.Format = "C$ #,##0.00";

            currentRow++;
        }

        ws.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }
}
