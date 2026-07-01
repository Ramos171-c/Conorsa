using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.Common.Interfaces;
using EnterpriseBillingSystem.Application.JournalEntries.Commands;
using EnterpriseBillingSystem.Application.FixedAssets.Commands;

namespace EnterpriseBillingSystem.Application.Purchases.Commands;

/// <summary>
/// Contabiliza una factura de compra.
/// Si CreateAsFixedAsset = true, crea automáticamente un Activo Fijo usando el monto total de la factura.
/// </summary>
public record PostPurchaseInvoiceCommand(
    Guid PurchaseInvoiceId,
    bool CreateAsFixedAsset = false,
    Guid? FixedAssetCategoryId = null
) : IRequest<Unit>;

public class PostPurchaseInvoiceCommandValidator : AbstractValidator<PostPurchaseInvoiceCommand>
{
    public PostPurchaseInvoiceCommandValidator()
    {
        RuleFor(x => x.PurchaseInvoiceId)
            .NotEmpty().WithMessage("El Id de la factura de compra es requerido.");

        When(x => x.CreateAsFixedAsset, () =>
        {
            RuleFor(x => x.FixedAssetCategoryId)
                .NotEmpty().WithMessage("Debe indicar la categoría de activo fijo cuando CreateAsFixedAsset = true.");
        });
    }
}

public class PostPurchaseInvoiceCommandHandler : IRequestHandler<PostPurchaseInvoiceCommand, Unit>
{
    private readonly IPurchaseInvoiceRepository _purchaseInvoiceRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IAccountsPayableRepository _apRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public PostPurchaseInvoiceCommandHandler(
        IPurchaseInvoiceRepository purchaseInvoiceRepository,
        ISupplierRepository supplierRepository,
        IAccountsPayableRepository apRepository,
        ICurrentUserService currentUserService,
        IMediator mediator,
        IUnitOfWork unitOfWork)
    {
        _purchaseInvoiceRepository = purchaseInvoiceRepository;
        _supplierRepository = supplierRepository;
        _apRepository = apRepository;
        _currentUserService = currentUserService;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(PostPurchaseInvoiceCommand request, CancellationToken cancellationToken)
    {
        // 1. Obtener factura con detalles
        var invoice = await _purchaseInvoiceRepository.GetByIdWithDetailsAsync(request.PurchaseInvoiceId, cancellationToken);
        if (invoice == null)
            throw new ArgumentException($"La factura de compra con Id '{request.PurchaseInvoiceId}' no existe.");

        if (invoice.Status != PurchaseInvoiceStatus.Draft)
            throw new InvalidOperationException($"Solo se pueden contabilizar facturas en estado Borrador. Estado actual: {invoice.Status}.");

        // 2. Validar proveedor activo
        var supplier = await _supplierRepository.GetByIdAsync(invoice.SupplierId);
        if (supplier == null)
            throw new ArgumentException("El proveedor asociado a la factura no existe.");
        if (!supplier.IsActive || supplier.Status != SupplierStatus.Active)
            throw new InvalidOperationException($"El proveedor '{supplier.Name}' no está activo.");

        // 3. Validar montos coherentes
        if (invoice.TotalAmount <= 0)
            throw new InvalidOperationException("El monto total de la factura debe ser mayor a 0.");

        // 4. Cambiar estado de la factura a Posted
        invoice.Status = PurchaseInvoiceStatus.Posted;
        invoice.LastModifiedBy = _currentUserService.UserId ?? "System";
        invoice.LastModifiedOnUtc = DateTime.UtcNow;

        _purchaseInvoiceRepository.Update(invoice);

        // 5. Crear la Cuenta por Pagar (AP)
        var existingAp = await _apRepository.GetByInvoiceIdAsync(invoice.Id, cancellationToken);
        if (existingAp != null)
            throw new InvalidOperationException($"Ya existe una cuenta por pagar registrada para la factura de compra '{invoice.InvoiceNumber}'.");

        var ap = new Domain.Entities.AccountsPayable
        {
            Id = Guid.NewGuid(),
            SupplierId = invoice.SupplierId,
            PurchaseInvoiceId = invoice.Id,
            DocumentNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate ?? invoice.InvoiceDate.AddDays(invoice.PaymentTermsDays),
            OriginalAmount = invoice.TotalAmount,
            PaidAmount = 0m,
            CurrentBalance = invoice.TotalAmount,
            Status = AccountsPayableStatus.Pending,
            Notes = invoice.Notes,
            CreatedBy = _currentUserService.UserId ?? "System",
            CreatedOnUtc = DateTime.UtcNow,
            BranchId = invoice.BranchId
        };

        await _apRepository.AddAsync(ap);

        // 6. Si la factura es para capitalizar como Activo Fijo, crear el activo dentro de la misma transacción
        if (request.CreateAsFixedAsset && request.FixedAssetCategoryId.HasValue)
        {
            // Usa CxP como cuenta origen (2100)
            var createAssetCmd = new CreateFixedAssetCommand(
                Name: $"Activo de Factura {invoice.InvoiceNumber}",
                Description: $"Activo creado automáticamente desde Factura de Compra {invoice.InvoiceNumber}",
                FixedAssetCategoryId: request.FixedAssetCategoryId.Value,
                PurchaseInvoiceId: invoice.Id,
                AcquisitionDate: invoice.InvoiceDate,
                AcquisitionCost: invoice.TotalAmount,
                ResidualValue: 0m,
                UsefulLifeMonths: 60, // Valor por defecto — se puede actualizar después
                DepreciationStartDate: invoice.InvoiceDate.AddMonths(1),
                SourceAccountCode: "2100", // Cuentas por Pagar
                Location: null,
                SerialNumber: null,
                Notes: $"Originado desde Factura de Compra {invoice.InvoiceNumber}"
            );

            await _mediator.Send(createAssetCmd, cancellationToken);

            // En este caso el JournalEntry de compra ya lo genera CreateFixedAssetCommand
            // No generamos el JournalEntry de inventario
        }
        else
        {
            // Flujo normal de compra: Generar asiento contable de inventario
            var jeDetails = new List<JournalEntryDetailInput>();
            if (invoice.PaymentTermsDays == 0)
            {
                // Compra Contado: Dr 1300 Inventarios / Cr 1110 Caja General
                jeDetails.Add(new JournalEntryDetailInput("1300", invoice.TotalAmount, 0, $"Compra Contado Factura {invoice.InvoiceNumber}"));
                jeDetails.Add(new JournalEntryDetailInput("1110", 0, invoice.TotalAmount, $"Compra Contado Factura {invoice.InvoiceNumber}"));
            }
            else
            {
                // Compra Crédito: Dr 1300 Inventarios / Cr 2100 Cuentas por Pagar
                jeDetails.Add(new JournalEntryDetailInput("1300", invoice.TotalAmount, 0, $"Compra Crédito Factura {invoice.InvoiceNumber}"));
                jeDetails.Add(new JournalEntryDetailInput("2100", 0, invoice.TotalAmount, $"Compra Crédito Factura {invoice.InvoiceNumber}"));
            }

            var createJeCmd = new CreateJournalEntryCommand(
                EntryDate: invoice.InvoiceDate,
                Description: $"Asiento por Compra Factura {invoice.InvoiceNumber}",
                ReferenceDocument: invoice.InvoiceNumber,
                ReferenceId: invoice.Id,
                SourceModule: "Purchases",
                Details: jeDetails,
                PostImmediately: true
            );

            await _mediator.Send(createJeCmd, cancellationToken);
        }

        // Guardar todo en una transacción única
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
