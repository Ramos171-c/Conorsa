using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EnterpriseBillingSystem.Wpf.Models;

namespace EnterpriseBillingSystem.Wpf.ViewModels;

public partial class CartItemViewModel : ObservableObject
{
    [ObservableProperty]
    private Guid _productId;

    [ObservableProperty]
    private Guid _unitOfMeasureId;

    [ObservableProperty]
    private string _productCode = string.Empty;

    [ObservableProperty]
    private string _productName = string.Empty;

    [ObservableProperty]
    private decimal _quantity = 1;

    [ObservableProperty]
    private decimal _unitPrice;

    [ObservableProperty]
    private decimal _discountPercentage;

    [ObservableProperty]
    private decimal _taxPercentage = 16.00m;

    [ObservableProperty]
    private decimal _discountAmount;

    [ObservableProperty]
    private decimal _taxAmount;

    [ObservableProperty]
    private decimal _subtotal;

    [ObservableProperty]
    private decimal _total;

    [ObservableProperty]
    private Guid _productPresentationId;

    [ObservableProperty]
    private string _presentationName = string.Empty;

    [ObservableProperty]
    private decimal _conversionFactor = 1;

    [ObservableProperty]
    private decimal _baseAvailableStock;

    [ObservableProperty]
    private string _priceLevel = "Retail";

    [ObservableProperty]
    private CustomerPricingType _activePricingType = CustomerPricingType.Retail;

    public ObservableCollection<ProductPresentationDto> AvailablePresentations { get; } = new();

    private ProductPresentationDto? _selectedPresentation;
    public ProductPresentationDto? SelectedPresentation
    {
        get => _selectedPresentation;
        set
        {
            if (SetProperty(ref _selectedPresentation, value))
            {
                if (value != null)
                {
                    ProductPresentationId = value.Id;
                    PresentationName = value.Name;
                    ConversionFactor = value.ConversionFactor;
                    TaxPercentage = value.TaxPercentage;
                    UnitOfMeasureId = value.UnitOfMeasureId;
                    UpdatePriceLevelAndUnitPrice();
                    Recalculate();
                }
            }
        }
    }

    public decimal EquivalentAvailableStock => ConversionFactor > 0 ? BaseAvailableStock / ConversionFactor : BaseAvailableStock;

    public Action? OnItemChanged { get; set; }

    public CartItemViewModel()
    {
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ActivePricingType))
            {
                UpdatePriceLevelAndUnitPrice();
            }
            else if (e.PropertyName == nameof(UnitPrice) || 
                      e.PropertyName == nameof(DiscountPercentage) ||
                      e.PropertyName == nameof(TaxPercentage) ||
                      e.PropertyName == nameof(Quantity))
            {
                Recalculate();
            }
            
            if (e.PropertyName == nameof(ConversionFactor) ||
                e.PropertyName == nameof(BaseAvailableStock))
            {
                OnPropertyChanged(nameof(EquivalentAvailableStock));
            }
        };
    }

    public void UpdatePriceLevelAndUnitPrice()
    {
        if (SelectedPresentation == null) return;

        switch (ActivePricingType)
        {
            case CustomerPricingType.Wholesale:
                UnitPrice = SelectedPresentation.WholesalePrice;
                PriceLevel = "Mayorista";
                break;
            case CustomerPricingType.SemiWholesale:
                UnitPrice = SelectedPresentation.SemiWholesalePrice;
                PriceLevel = "Semi Mayorista";
                break;
            case CustomerPricingType.Retail:
            default:
                UnitPrice = SelectedPresentation.RetailPrice;
                PriceLevel = "Retail";
                break;
        }
    }

    public void Recalculate()
    {
        var rawSubtotal = Quantity * UnitPrice;
        DiscountAmount = Math.Round(rawSubtotal * (DiscountPercentage / 100m), 4);
        Subtotal = Math.Round(rawSubtotal - DiscountAmount, 4);
        TaxAmount = Math.Round(Subtotal * (TaxPercentage / 100m), 4);
        Total = Math.Round(Subtotal + TaxAmount, 4);

        OnItemChanged?.Invoke();
    }
}
