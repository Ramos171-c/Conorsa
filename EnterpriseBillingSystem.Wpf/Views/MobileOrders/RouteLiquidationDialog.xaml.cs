using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using EnterpriseBillingSystem.Wpf.Models;
using EnterpriseBillingSystem.Wpf.Services.Api;

namespace EnterpriseBillingSystem.Wpf.Views.MobileOrders;

public class PresentationOption
{
    public Guid? Id { get; set; }
    public Guid UnitOfMeasureId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal ConversionFactor { get; set; } = 1.0m;
}

public partial class RouteLiquidationDialogItem : ObservableObject
{
    public Guid ProductId { get; }
    public string ProductCode { get; }
    public string ProductName { get; }
    public decimal BaseQuantitySent { get; }
    public decimal SalePrice { get; }
    public decimal Cost { get; }

    public List<PresentationOption> Presentations { get; }

    [ObservableProperty]
    private PresentationOption _selectedPresentation = null!;

    [ObservableProperty]
    private decimal _quantitySent;

    [ObservableProperty]
    private decimal _quantityReturned;

    [ObservableProperty]
    private decimal _quantitySold;

    [ObservableProperty]
    private decimal _subtotalSold;

    [ObservableProperty]
    private decimal _subtotalReturned;

    [ObservableProperty]
    private string _notes = string.Empty;

    public RouteLiquidationDialogItem(
        Guid productId,
        string productCode,
        string productName,
        decimal baseQuantitySent,
        decimal salePrice,
        decimal cost,
        List<PresentationOption> presentations)
    {
        ProductId = productId;
        ProductCode = productCode;
        ProductName = productName;
        BaseQuantitySent = baseQuantitySent;
        SalePrice = salePrice;
        Cost = cost;
        Presentations = presentations;

        _selectedPresentation = presentations.FirstOrDefault() ?? new PresentationOption { Name = "UND", ConversionFactor = 1.0m };
        _quantitySent = BaseQuantitySent / _selectedPresentation.ConversionFactor;
        _quantityReturned = 0;
        _quantitySold = _quantitySent;
        _subtotalSold = _quantitySold * SalePrice;
        _subtotalReturned = 0;
    }

    partial void OnSelectedPresentationChanged(PresentationOption value)
    {
        if (value != null && value.ConversionFactor > 0)
        {
            QuantitySent = BaseQuantitySent / value.ConversionFactor;
            if (QuantityReturned > QuantitySent)
            {
                QuantityReturned = QuantitySent;
            }
            Recalculate();
        }
    }

    partial void OnQuantityReturnedChanged(decimal value)
    {
        if (value > QuantitySent)
        {
            QuantityReturned = QuantitySent;
        }
        else if (value < 0)
        {
            QuantityReturned = 0;
        }
        Recalculate();
    }

    public void Recalculate()
    {
        QuantitySold = Math.Max(0, QuantitySent - QuantityReturned);
        SubtotalSold = QuantitySold * SalePrice;
        SubtotalReturned = QuantityReturned * SalePrice;
        OnPropertyChanged(nameof(SubtotalSold));
        OnPropertyChanged(nameof(SubtotalReturned));
    }
}

public partial class RouteLiquidationDialogViewModel : ObservableObject
{
    private readonly SalesApiClient _salesApiClient;
    private readonly Action<bool> _closeAction;

    [ObservableProperty]
    private string _headerTitle = string.Empty;

    [ObservableProperty]
    private string _observations = string.Empty;

    public Guid RouteId { get; }
    public string RouteName { get; }

    public ObservableCollection<RouteLiquidationDialogItem> Items { get; } = new();

    public decimal TotalSent => Items.Sum(i => i.QuantitySent);
    public decimal TotalReturned => Items.Sum(i => i.QuantityReturned);
    public decimal TotalSold => Items.Sum(i => i.QuantitySold);
    public decimal TotalAmountSold => Items.Sum(i => i.SubtotalSold);

    public RouteLiquidationDialogViewModel(
        Guid routeId,
        string routeName,
        List<ConsolidatedProductDto> consolidatedProducts,
        SalesApiClient salesApiClient,
        Action<bool> closeAction)
    {
        RouteId = routeId;
        RouteName = routeName;
        _salesApiClient = salesApiClient;
        _closeAction = closeAction;

        HeaderTitle = $"Liquidación / Devolución Masiva - Ruta: {RouteName}";

        foreach (var prod in consolidatedProducts)
        {
            var options = new List<PresentationOption>
            {
                new PresentationOption
                {
                    Id = null,
                    UnitOfMeasureId = Guid.Empty,
                    Name = string.IsNullOrWhiteSpace(prod.UnitOfMeasure) ? "UND" : prod.UnitOfMeasure,
                    ConversionFactor = 1.0m
                }
            };

            var item = new RouteLiquidationDialogItem(
                prod.ProductId,
                prod.ProductCode,
                prod.ProductName,
                prod.TotalQuantity,
                prod.UnitPrice,
                prod.UnitCost,
                options
            );

            item.PropertyChanged += Item_PropertyChanged;
            Items.Add(item);
        }
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(TotalSent));
        OnPropertyChanged(nameof(TotalReturned));
        OnPropertyChanged(nameof(TotalSold));
        OnPropertyChanged(nameof(TotalAmountSold));
    }

    [RelayCommand]
    private void ExportExcel()
    {
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Archivos de Excel (*.xlsx)|*.xlsx",
                FileName = $"Borrador_Liquidacion_{RouteName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
                Title = "Guardar Borrador de Liquidación en Excel"
            };

            if (dialog.ShowDialog() == true)
            {
                Services.Export.ExcelExportService.ExportRouteLiquidationToExcel(RouteName, Items, dialog.FileName, Observations);
                System.Windows.MessageBox.Show("Borrador de liquidación exportado exitosamente a Excel.", "Exportación Exitosa", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error al exportar borrador a Excel: {ex.Message}", "Error de Exportación", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task ConfirmLiquidationAsync()
    {
        try
        {
            var details = Items.Select(i => new
            {
                productId = i.ProductId,
                unitOfMeasureId = i.SelectedPresentation.UnitOfMeasureId,
                productPresentationId = i.SelectedPresentation.Id,
                quantitySent = i.QuantitySent,
                quantityReturned = i.QuantityReturned,
                salePrice = i.SalePrice,
                cost = i.Cost,
                notes = i.Notes
            }).ToList();

            var command = new
            {
                routeId = RouteId,
                observations = Observations,
                details = details
            };

            await _salesApiClient.CreateRouteLiquidationAsync(command);
            DialogHost.Close(null, true);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error al procesar la liquidación: {ex.Message}", "Error de Liquidación", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}

public partial class RouteLiquidationDialog : UserControl
{
    public RouteLiquidationDialog()
    {
        InitializeComponent();
    }
}
