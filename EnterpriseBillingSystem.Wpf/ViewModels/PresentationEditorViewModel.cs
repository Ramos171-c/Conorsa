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
    private decimal _retailMargin;

    [ObservableProperty]
    private decimal _semiWholesalePrice;

    [ObservableProperty]
    private decimal _semiWholesaleMargin;

    [ObservableProperty]
    private decimal _wholesalePrice;

    [ObservableProperty]
    private decimal _wholesaleMargin;

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

    private bool _isUpdating;

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
            
            // Set fields directly to prevent initial change handlers trigger
            _cost = presentation.Cost;
            _retailPrice = presentation.RetailPrice;
            _retailMargin = CalculateMargin(presentation.Cost, presentation.RetailPrice);
            _semiWholesalePrice = presentation.SemiWholesalePrice;
            _semiWholesaleMargin = CalculateMargin(presentation.Cost, presentation.SemiWholesalePrice);
            _wholesalePrice = presentation.WholesalePrice;
            _wholesaleMargin = CalculateMargin(presentation.Cost, presentation.WholesalePrice);

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

    private decimal CalculatePrice(decimal cost, decimal marginPercentage)
    {
        if (marginPercentage >= 100) return 0;
        return Math.Round(cost / (1 - (marginPercentage / 100)), 2);
    }

    private decimal CalculateMargin(decimal cost, decimal price)
    {
        if (price <= 0) return 0;
        return Math.Round(((price - cost) / price) * 100, 2);
    }

    partial void OnCostChanged(decimal value)
    {
        if (_isUpdating) return;
        _isUpdating = true;
        try
        {
            RetailPrice = CalculatePrice(value, RetailMargin);
            SemiWholesalePrice = CalculatePrice(value, SemiWholesaleMargin);
            WholesalePrice = CalculatePrice(value, WholesaleMargin);
        }
        finally
        {
            _isUpdating = false;
        }
    }

    partial void OnRetailPriceChanged(decimal value)
    {
        if (_isUpdating) return;
        _isUpdating = true;
        try
        {
            RetailMargin = CalculateMargin(Cost, value);
        }
        finally
        {
            _isUpdating = false;
        }
    }

    partial void OnRetailMarginChanged(decimal value)
    {
        if (_isUpdating) return;
        _isUpdating = true;
        try
        {
            RetailPrice = CalculatePrice(Cost, value);
        }
        finally
        {
            _isUpdating = false;
        }
    }

    partial void OnSemiWholesalePriceChanged(decimal value)
    {
        if (_isUpdating) return;
        _isUpdating = true;
        try
        {
            SemiWholesaleMargin = CalculateMargin(Cost, value);
        }
        finally
        {
            _isUpdating = false;
        }
    }

    partial void OnSemiWholesaleMarginChanged(decimal value)
    {
        if (_isUpdating) return;
        _isUpdating = true;
        try
        {
            SemiWholesalePrice = CalculatePrice(Cost, value);
        }
        finally
        {
            _isUpdating = false;
        }
    }

    partial void OnWholesalePriceChanged(decimal value)
    {
        if (_isUpdating) return;
        _isUpdating = true;
        try
        {
            WholesaleMargin = CalculateMargin(Cost, value);
        }
        finally
        {
            _isUpdating = false;
        }
    }

    partial void OnWholesaleMarginChanged(decimal value)
    {
        if (_isUpdating) return;
        _isUpdating = true;
        try
        {
            WholesalePrice = CalculatePrice(Cost, value);
        }
        finally
        {
            _isUpdating = false;
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

        if (RetailPrice < Cost || SemiWholesalePrice < Cost || WholesalePrice < Cost)
        {
            ErrorMessage = "Los precios de venta no pueden ser menores que el costo.";
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
