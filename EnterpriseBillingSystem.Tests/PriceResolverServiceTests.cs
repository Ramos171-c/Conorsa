using System;
using Xunit;
using EnterpriseBillingSystem.Application.Common.Services;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Tests;

public class PriceResolverServiceTests
{
    private readonly PriceResolverService _service;

    public PriceResolverServiceTests()
    {
        _service = new PriceResolverService();
    }

    [Fact]
    public void ResolveUnitPrice_VendedorDetalle_ClienteRetail_RetornaRetailPrice()
    {
        var presentation = new ProductPresentation
        {
            Cost = 500m,
            RetailPrice = 650m,
            SemiWholesalePrice = 600m,
            WholesalePrice = 550m
        };

        var unitPrice = _service.ResolveUnitPrice(presentation, SellerCategory.Detail, CustomerPricingType.Retail);

        Assert.Equal(650m, unitPrice);
    }

    [Fact]
    public void ResolveUnitPrice_VendedorDetalle_ClienteSemiWholesale_RetornaSemiWholesalePrice()
    {
        var presentation = new ProductPresentation
        {
            Cost = 500m,
            RetailPrice = 650m,
            SemiWholesalePrice = 600m,
            WholesalePrice = 550m
        };

        var unitPrice = _service.ResolveUnitPrice(presentation, SellerCategory.Detail, CustomerPricingType.SemiWholesale);

        Assert.Equal(600m, unitPrice);
    }

    [Fact]
    public void ResolveUnitPrice_VendedorDetalle_ClienteWholesale_RetornaWholesalePrice()
    {
        var presentation = new ProductPresentation
        {
            Cost = 500m,
            RetailPrice = 650m,
            SemiWholesalePrice = 600m,
            WholesalePrice = 550m
        };

        var unitPrice = _service.ResolveUnitPrice(presentation, SellerCategory.Detail, CustomerPricingType.Wholesale);

        Assert.Equal(550m, unitPrice);
    }

    [Fact]
    public void ResolveUnitPrice_VendedorCosto_CualquierCliente_RetornaCostExclusivamente()
    {
        var presentation = new ProductPresentation
        {
            Cost = 500m,
            RetailPrice = 650m,
            SemiWholesalePrice = 600m,
            WholesalePrice = 550m
        };

        var unitPriceRetail = _service.ResolveUnitPrice(presentation, SellerCategory.Cost, CustomerPricingType.Retail);
        var unitPriceSemi = _service.ResolveUnitPrice(presentation, SellerCategory.Cost, CustomerPricingType.SemiWholesale);
        var unitPriceWholesale = _service.ResolveUnitPrice(presentation, SellerCategory.Cost, CustomerPricingType.Wholesale);

        Assert.Equal(500m, unitPriceRetail);
        Assert.Equal(500m, unitPriceSemi);
        Assert.Equal(500m, unitPriceWholesale);
    }

    [Fact]
    public void IsPresentationAvailableForSeller_ExclusivoCosto_VendedorDetalleNoPermitido()
    {
        var presentation = new ProductPresentation
        {
            IsActive = true,
            AllowSale = true,
            AllowDetailChannel = false,
            AllowCostChannel = true
        };

        var availableForDetail = _service.IsPresentationAvailableForSeller(presentation, SellerCategory.Detail, isAdmin: false);
        var availableForCost = _service.IsPresentationAvailableForSeller(presentation, SellerCategory.Cost, isAdmin: false);
        var availableForAdmin = _service.IsPresentationAvailableForSeller(presentation, SellerCategory.Detail, isAdmin: true);

        Assert.False(availableForDetail);
        Assert.True(availableForCost);
        Assert.True(availableForAdmin);
    }

    [Fact]
    public void IsPresentationAvailableForSeller_ExclusivoDetalle_VendedorCostoNoPermitido()
    {
        var presentation = new ProductPresentation
        {
            IsActive = true,
            AllowSale = true,
            AllowDetailChannel = true,
            AllowCostChannel = false
        };

        var availableForDetail = _service.IsPresentationAvailableForSeller(presentation, SellerCategory.Detail, isAdmin: false);
        var availableForCost = _service.IsPresentationAvailableForSeller(presentation, SellerCategory.Cost, isAdmin: false);
        var availableForAdmin = _service.IsPresentationAvailableForSeller(presentation, SellerCategory.Cost, isAdmin: true);

        Assert.True(availableForDetail);
        Assert.False(availableForCost);
        Assert.True(availableForAdmin);
    }
}
