using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.Sales.Commands;
using EnterpriseBillingSystem.Application.Inventory.Commands;
using EnterpriseBillingSystem.Application.Inventory.Queries;
using EnterpriseBillingSystem.Application.Common.Interfaces;

namespace EnterpriseBillingSystem.Tests;

public class InventoryAuditTests
{
    private readonly Mock<ISalesOrderRepository> _salesOrderRepoMock = new();
    private readonly Mock<ISalesInvoiceRepository> _salesInvoiceRepoMock = new();
    private readonly Mock<IInventoryRepository> _inventoryRepoMock = new();
    private readonly Mock<IInventoryMovementRepository> _movementRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IRepository<BranchWarehouse>> _branchWarehouseRepoMock = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepoMock = new();
    private readonly Mock<IRepository<SalesOrderDetail>> _salesOrderDetailRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    public InventoryAuditTests()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns("TestUser");
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    [Fact]
    public async Task UpdateSalesOrderStatus_EnProceso_DoesNotDeductInventory()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new SalesOrder
        {
            Id = orderId,
            OrderNumber = "ORD-001",
            Status = SalesOrderStatus.Recibido,
            Details = new List<SalesOrderDetail>
            {
                new SalesOrderDetail { ProductId = Guid.NewGuid(), Quantity = 1 }
            }
        };

        _salesOrderRepoMock.Setup(r => r.GetByIdWithDetailsAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var handler = new ConfirmSalesOrderCommandHandler(
            _salesOrderRepoMock.Object,
            _inventoryRepoMock.Object,
            _productRepoMock.Object,
            _branchWarehouseRepoMock.Object,
            _unitOfWorkMock.Object);

        // Act
        await handler.Handle(new ConfirmSalesOrderCommand(orderId), CancellationToken.None);

        // Assert
        order.Status.Should().Be(SalesOrderStatus.EnProceso);
        _inventoryRepoMock.Verify(r => r.Update(It.IsAny<Inventory>()), Times.Never);
        _movementRepoMock.Verify(r => r.AddAsync(It.IsAny<InventoryMovement>()), Times.Never);
    }

    [Fact]
    public async Task UpdateSalesOrderStatus_TransitionToEnCamino_DeductsInventoryOnce()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var uomId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        var product = new Product
        {
            Id = productId,
            Name = "Galletas Trio",
            TrackInventory = true,
            ProductType = ProductType.Physical,
            Presentations = new List<ProductPresentation>
            {
                new ProductPresentation { Id = Guid.NewGuid(), ProductId = productId, UnitOfMeasureId = uomId, ConversionFactor = 24.0000m, IsActive = true }
            }
        };

        var order = new SalesOrder
        {
            Id = orderId,
            OrderNumber = "ORD-002",
            Status = SalesOrderStatus.EnProceso,
            Details = new List<SalesOrderDetail>
            {
                new SalesOrderDetail { ProductId = productId, UnitOfMeasureId = uomId, Quantity = 2 }
            }
        };

        var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = "Bodega Exhibición" };
        var branchWarehouse = new BranchWarehouse { Id = warehouseId, WarehouseId = warehouse.Id, IsActive = true };
        var inventory = new Inventory { Id = Guid.NewGuid(), BranchWarehouseId = warehouseId, ProductId = productId, PhysicalStock = 100m };

        _salesOrderRepoMock.Setup(r => r.GetByIdWithDetailsAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _warehouseRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Warehouse, bool>>>()))
            .ReturnsAsync(new List<Warehouse> { warehouse });
        _branchWarehouseRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BranchWarehouse, bool>>>()))
            .ReturnsAsync(new List<BranchWarehouse> { branchWarehouse });
        _productRepoMock.Setup(r => r.GetByIdWithDetailsAsync(productId, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _inventoryRepoMock.Setup(r => r.GetByWarehouseAndProductAsync(warehouseId, productId, It.IsAny<CancellationToken>())).ReturnsAsync(inventory);
        _movementRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<InventoryMovement, bool>>>()))
            .ReturnsAsync(new List<InventoryMovement>());
        _movementRepoMock.Setup(r => r.GenerateMovementNumberAsync(It.IsAny<CancellationToken>())).ReturnsAsync("MOV-001");

        var handler = new UpdateSalesOrderStatusCommandHandler(
            _salesOrderRepoMock.Object,
            _inventoryRepoMock.Object,
            _movementRepoMock.Object,
            _productRepoMock.Object,
            _branchWarehouseRepoMock.Object,
            _warehouseRepoMock.Object,
            _salesOrderDetailRepoMock.Object,
            _currentUserServiceMock.Object,
            _unitOfWorkMock.Object);

        // Act
        await handler.Handle(new UpdateSalesOrderStatusCommand(orderId, SalesOrderStatus.EnCamino), CancellationToken.None);

        // Assert
        order.Status.Should().Be(SalesOrderStatus.EnCamino);
        // 2 boxes * 24 factor = 48 base units deducted
        inventory.PhysicalStock.Should().Be(52m); // 100 - 48
        _movementRepoMock.Verify(r => r.AddAsync(It.Is<InventoryMovement>(m => m.MovementType == MovementType.Exit)), Times.Once);
    }

    [Fact]
    public async Task UpdateSalesOrderStatus_TransitionToEnCamino_WhenAlreadyMovementExists_IsIdempotent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new SalesOrder
        {
            Id = orderId,
            OrderNumber = "ORD-003",
            Status = SalesOrderStatus.EnProceso,
            Details = new List<SalesOrderDetail>()
        };

        var existingMovement = new InventoryMovement
        {
            ReferenceDocument = "ORD-003",
            MovementType = MovementType.Exit
        };

        _salesOrderRepoMock.Setup(r => r.GetByIdWithDetailsAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _movementRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<InventoryMovement, bool>>>()))
            .ReturnsAsync(new List<InventoryMovement> { existingMovement });

        var handler = new UpdateSalesOrderStatusCommandHandler(
            _salesOrderRepoMock.Object,
            _inventoryRepoMock.Object,
            _movementRepoMock.Object,
            _productRepoMock.Object,
            _branchWarehouseRepoMock.Object,
            _warehouseRepoMock.Object,
            _salesOrderDetailRepoMock.Object,
            _currentUserServiceMock.Object,
            _unitOfWorkMock.Object);

        // Act
        await handler.Handle(new UpdateSalesOrderStatusCommand(orderId, SalesOrderStatus.EnCamino), CancellationToken.None);

        // Assert
        order.Status.Should().Be(SalesOrderStatus.EnCamino);
        _inventoryRepoMock.Verify(r => r.Update(It.IsAny<Inventory>()), Times.Never);
    }

    [Fact]
    public async Task CancelSalesOrder_WhenEnCamino_GeneratesReversalMovementAndRestoresStock()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var uomId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        var order = new SalesOrder
        {
            Id = orderId,
            OrderNumber = "ORD-004",
            Status = SalesOrderStatus.EnCamino,
            Details = new List<SalesOrderDetail>
            {
                new SalesOrderDetail { ProductId = productId, UnitOfMeasureId = uomId, Quantity = 1 }
            }
        };

        var product = new Product
        {
            Id = productId,
            Name = "Galleta Jumbo",
            TrackInventory = true,
            Presentations = new List<ProductPresentation>
            {
                new ProductPresentation { UnitOfMeasureId = uomId, ConversionFactor = 10.0000m }
            }
        };

        var exitMovement = new InventoryMovement
        {
            ReferenceDocument = "ORD-004",
            MovementType = MovementType.Exit,
            FromBranchWarehouseId = warehouseId
        };

        var allMovements = new List<InventoryMovement> { exitMovement };

        var inventory = new Inventory { BranchWarehouseId = warehouseId, ProductId = productId, PhysicalStock = 10m };

        _salesOrderRepoMock.Setup(r => r.GetByIdWithDetailsAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _movementRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<InventoryMovement, bool>>>()))
            .ReturnsAsync((Expression<Func<InventoryMovement, bool>> pred) => allMovements.Where(pred.Compile()).ToList());

        _productRepoMock.Setup(r => r.GetByIdWithDetailsAsync(productId, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _inventoryRepoMock.Setup(r => r.GetByWarehouseAndProductAsync(warehouseId, productId, It.IsAny<CancellationToken>())).ReturnsAsync(inventory);
        _movementRepoMock.Setup(r => r.GenerateMovementNumberAsync(It.IsAny<CancellationToken>())).ReturnsAsync("MOV-002");

        var handler = new CancelSalesOrderCommandHandler(
            _salesOrderRepoMock.Object,
            _inventoryRepoMock.Object,
            _movementRepoMock.Object,
            _productRepoMock.Object,
            _branchWarehouseRepoMock.Object,
            _warehouseRepoMock.Object,
            _currentUserServiceMock.Object,
            _unitOfWorkMock.Object);

        // Act
        await handler.Handle(new CancelSalesOrderCommand(orderId, "Cliente canceló"), CancellationToken.None);

        // Assert
        order.Status.Should().Be(SalesOrderStatus.Anulado);
        inventory.PhysicalStock.Should().Be(20m); // 10 + (1 * 10)
        _movementRepoMock.Verify(r => r.AddAsync(It.Is<InventoryMovement>(m => m.MovementType == MovementType.SaleReversal)), Times.Once);
    }

    [Fact]
    public async Task UpdateSalesOrderStatus_TransitionToEnCamino_WithInsufficientStock_AdjustsAndRemovesDetails()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var p1Id = Guid.NewGuid();
        var p2Id = Guid.NewGuid();
        var uomId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        var p1 = new Product
        {
            Id = p1Id,
            Name = "Product 1",
            TrackInventory = true,
            ProductType = ProductType.Physical,
            Presentations = new List<ProductPresentation>
            {
                new ProductPresentation { Id = Guid.NewGuid(), ProductId = p1Id, UnitOfMeasureId = uomId, ConversionFactor = 1.0m, IsActive = true }
            }
        };

        var p2 = new Product
        {
            Id = p2Id,
            Name = "Product 2",
            TrackInventory = true,
            ProductType = ProductType.Physical,
            Presentations = new List<ProductPresentation>
            {
                new ProductPresentation { Id = Guid.NewGuid(), ProductId = p2Id, UnitOfMeasureId = uomId, ConversionFactor = 1.0m, IsActive = true }
            }
        };

        var detail1 = new SalesOrderDetail { ProductId = p1Id, UnitOfMeasureId = uomId, Quantity = 10m, UnitPrice = 5m };
        var detail2 = new SalesOrderDetail { ProductId = p2Id, UnitOfMeasureId = uomId, Quantity = 5m, UnitPrice = 10m };

        var order = new SalesOrder
        {
            Id = orderId,
            OrderNumber = "ORD-StockAdjust",
            Status = SalesOrderStatus.EnProceso,
            Details = new List<SalesOrderDetail> { detail1, detail2 }
        };

        var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = "Bodega Exhibición" };
        var branchWarehouse = new BranchWarehouse { Id = warehouseId, WarehouseId = warehouse.Id, IsActive = true };
        var inventory1 = new Inventory { Id = Guid.NewGuid(), BranchWarehouseId = warehouseId, ProductId = p1Id, PhysicalStock = 5m };
        var inventory2 = new Inventory { Id = Guid.NewGuid(), BranchWarehouseId = warehouseId, ProductId = p2Id, PhysicalStock = 0m };

        _salesOrderRepoMock.Setup(r => r.GetByIdWithDetailsAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _warehouseRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Warehouse, bool>>>()))
            .ReturnsAsync(new List<Warehouse> { warehouse });
        _branchWarehouseRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<BranchWarehouse, bool>>>()))
            .ReturnsAsync(new List<BranchWarehouse> { branchWarehouse });
        _productRepoMock.Setup(r => r.GetByIdWithDetailsAsync(p1Id, It.IsAny<CancellationToken>())).ReturnsAsync(p1);
        _productRepoMock.Setup(r => r.GetByIdWithDetailsAsync(p2Id, It.IsAny<CancellationToken>())).ReturnsAsync(p2);
        _inventoryRepoMock.Setup(r => r.GetByWarehouseAndProductAsync(warehouseId, p1Id, It.IsAny<CancellationToken>())).ReturnsAsync(inventory1);
        _inventoryRepoMock.Setup(r => r.GetByWarehouseAndProductAsync(warehouseId, p2Id, It.IsAny<CancellationToken>())).ReturnsAsync(inventory2);
        _movementRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<InventoryMovement, bool>>>()))
            .ReturnsAsync(new List<InventoryMovement>());
        _movementRepoMock.Setup(r => r.GenerateMovementNumberAsync(It.IsAny<CancellationToken>())).ReturnsAsync("MOV-StockAdjust");

        var handler = new UpdateSalesOrderStatusCommandHandler(
            _salesOrderRepoMock.Object,
            _inventoryRepoMock.Object,
            _movementRepoMock.Object,
            _productRepoMock.Object,
            _branchWarehouseRepoMock.Object,
            _warehouseRepoMock.Object,
            _salesOrderDetailRepoMock.Object,
            _currentUserServiceMock.Object,
            _unitOfWorkMock.Object);

        // Act
        await handler.Handle(new UpdateSalesOrderStatusCommand(orderId, SalesOrderStatus.EnCamino), CancellationToken.None);

        // Assert
        order.Status.Should().Be(SalesOrderStatus.EnCamino);
        
        // Detail 1 should be adjusted to 5
        detail1.Quantity.Should().Be(5m);
        detail1.OriginalPresaleQuantity.Should().Be(10m);
        inventory1.PhysicalStock.Should().Be(0m);

        // Detail 2 should be removed
        order.Details.Should().NotContain(detail2);
        _salesOrderDetailRepoMock.Verify(r => r.Remove(detail2), Times.Once);
    }
}
