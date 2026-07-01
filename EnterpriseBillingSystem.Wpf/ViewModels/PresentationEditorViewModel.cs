using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnterpriseBillingSystem.Wpf.Models;

namespace EnterpriseBillingSystem.Wpf.ViewModels;

public partial class PresentationEditorViewModel : ViewModelBase
{
    public ObservableCollection<UnitOfMeasureDto> UnitsOfMeasure { get; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private Guid _unitOfMeasureId;

    [ObservableProperty]
    private decimal _conversionFactor = 1;

    [ObservableProperty]
    private string? _barcode;

    [ObservableProperty]
    private decimal _cost;

    [ObservableProperty]
    private decimal _retailPrice;

    [ObservableProperty]
    private decimal _semiWholesalePrice;

    [ObservableProperty]
    private decimal _wholesalePrice;

    [ObservableProperty]
    private bool _isBaseUnit;

    [ObservableProperty]
    private bool _isDefaultSalePresentation;

    [ObservableProperty]
    private bool _allowPurchase = true;

    [ObservableProperty]
    private bool _allowSale = true;

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private string _dialogTitle = "Nueva Presentación";

    [ObservableProperty]
    private string? _errorMessage;

    public bool? DialogResult { get; set; }
    public event Action? RequestClose;

    public PresentationEditorViewModel(List<UnitOfMeasureDto> unitsOfMeasure, ProductPresentationViewModel? presentation = null)
    {
        UnitsOfMeasure = new ObservableCollection<UnitOfMeasureDto>(unitsOfMeasure);

        if (presentation != null)
        {
            DialogTitle = "Editar Presentación";
            Name = presentation.Name;
            UnitOfMeasureId = presentation.UnitOfMeasureId;
            ConversionFactor = presentation.ConversionFactor;
            Barcode = presentation.Barcode;
            Cost = presentation.Cost;
            RetailPrice = presentation.RetailPrice;
            SemiWholesalePrice = presentation.SemiWholesalePrice;
            WholesalePrice = presentation.WholesalePrice;
            IsBaseUnit = presentation.IsBaseUnit;
            IsDefaultSalePresentation = presentation.IsDefaultSalePresentation;
            AllowPurchase = presentation.AllowPurchase;
            AllowSale = presentation.AllowSale;
            IsActive = presentation.IsActive;
        }
        else if (unitsOfMeasure.Count > 0)
        {
            UnitOfMeasureId = unitsOfMeasure[0].Id;
        }
    }

    [RelayCommand]
    private void Save()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "El nombre es requerido.";
            return;
        }

        if (ConversionFactor <= 0)
        {
            ErrorMessage = "El factor de conversión debe ser mayor a cero.";
            return;
        }

        if (RetailPrice < 0 || SemiWholesalePrice < 0 || WholesalePrice < 0)
        {
            ErrorMessage = "Los precios de venta no pueden ser negativos.";
            return;
        }

        if (RetailPrice < Cost)
        {
            ErrorMessage = "El precio minorista no puede ser menor que el costo.";
            return;
        }

        if (SemiWholesalePrice < Cost)
        {
            ErrorMessage = "El precio semi-mayorista no puede ser menor que el costo.";
            return;
        }

        if (WholesalePrice < Cost)
        {
            ErrorMessage = "El precio mayorista no puede ser menor que el costo.";
            return;
        }

        DialogResult = true;
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
        RequestClose?.Invoke();
    }
}
