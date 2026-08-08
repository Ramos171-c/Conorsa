using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.Sales.Queries;

namespace EnterpriseBillingSystem.Tests;

public class DashboardSummaryTests
{
    private readonly Mock<ISalesOrderRepository> _salesOrderRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IRepository<ApplicationUser>> _userRepoMock = new();

    [Fact]
    public async Task GetDashboardSummary_CalculatesGrossProfitWithConversionFactor_AndFiltersTodayDate()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var boxUomId = Guid.NewGuid();

        var product = new Product
        {
            Id = productId,
            Name = "BOLSON DE PAÑALES CALSON OSITO",
            CurrentCost = 487.50m, // Base unit cost
            Presentations = new List<ProductPresentation>
            {
                new ProductPresentation
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    Name = "Caja",
                    UnitOfMeasureId = boxUomId,
                    ConversionFactor = 4.0m, // 4 base units per box
                    RetailPrice = 2241.38m
                }
            }
        };

        var todayOrder = new SalesOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-TODAY-001",
            OrderDate = DateTime.Today,
            Status = SalesOrderStatus.EnCamino,
            TotalAmount = 2241.38m,
            CreatedBy = "Vendedor1",
            Details = new List<SalesOrderDetail>
            {
                new SalesOrderDetail
                {
                    ProductId = productId,
                    UnitOfMeasureId = boxUomId,
                    Quantity = 1m, // 1 Box
                    UnitPrice = 2241.38m,
                    NetAmount = 2241.38m
                }
            }
        };

        _salesOrderRepoMock.Setup(r => r.GetFilteredWithDetailsAsync(
            It.IsAny<Guid?>(),
            It.IsAny<string?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SalesOrder> { todayOrder });

        _productRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Product> { product });
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ApplicationUser>());

        var handler = new GetDashboardSummaryQueryHandler(
            _salesOrderRepoMock.Object,
            _productRepoMock.Object,
            _userRepoMock.Object);

        // Act
        var result = await handler.Handle(new GetDashboardSummaryQuery(DateTime.Today, DateTime.Today), CancellationToken.None);

        // Assert
        result.SalesToday.Should().Be(2241.38m);
        result.OrdersToday.Should().Be(1);

        // Cost = 1 box * 4 factor * 487.50 = 1950.00
        // Expected Profit = 2241.38 - 1950.00 = 291.38
        result.ProfitToday.Should().Be(291.38m);
        Math.Abs(result.ProfitMarginToday - 13.0).Should().BeLessThan(0.5);
    }
}
