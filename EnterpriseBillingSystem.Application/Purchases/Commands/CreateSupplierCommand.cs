using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Purchases.Commands;

// ─── DTOs ────────────────────────────────────────────────────────────────────

public record CreateSupplierCommand(
    string IdentificationNumber,
    IdentificationType IdentificationType,
    string Name,
    string? LegalName,
    Guid SupplierCategoryId,
    string? Phone,
    string? Email,
    string? Address,
    string? ContactName
) : IRequest<Guid>;

// ─── Validator ────────────────────────────────────────────────────────────────

public class CreateSupplierCommandValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierCommandValidator()
    {
        RuleFor(x => x.IdentificationNumber)
            .NotEmpty().WithMessage("El número de identificación es requerido.")
            .MaximumLength(30).WithMessage("El número de identificación no puede superar 30 caracteres.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del proveedor es requerido.")
            .MaximumLength(200).WithMessage("El nombre no puede superar 200 caracteres.");

        RuleFor(x => x.LegalName)
            .MaximumLength(200).WithMessage("La razón social no puede superar 200 caracteres.")
            .When(x => x.LegalName != null);

        RuleFor(x => x.SupplierCategoryId)
            .NotEmpty().WithMessage("La categoría del proveedor es requerida.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("El correo electrónico no tiene un formato válido.")
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}

// ─── Handler ─────────────────────────────────────────────────────────────────

public class CreateSupplierCommandHandler : IRequestHandler<CreateSupplierCommand, Guid>
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly IRepository<SupplierCategory> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSupplierCommandHandler(
        ISupplierRepository supplierRepository,
        IRepository<SupplierCategory> categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _supplierRepository = supplierRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        // 1. Validar categoría
        var category = await _categoryRepository.GetByIdAsync(request.SupplierCategoryId);
        if (category == null)
            throw new ArgumentException("La categoría de proveedor especificada no existe.");

        // 2. Verificar identificación única
        var existingById = await _supplierRepository.GetByIdentificationAsync(request.IdentificationNumber, cancellationToken);
        if (existingById != null)
            throw new InvalidOperationException($"Ya existe un proveedor con el número de identificación '{request.IdentificationNumber}'.");

        // 3. Generar código interno
        var supplierCode = await _supplierRepository.GenerateSupplierCodeAsync(cancellationToken);

        // 4. Crear proveedor
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            SupplierCode = supplierCode,
            IdentificationNumber = request.IdentificationNumber,
            IdentificationType = request.IdentificationType,
            Name = request.Name,
            LegalName = request.LegalName,
            SupplierCategoryId = request.SupplierCategoryId,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            ContactName = request.ContactName,
            Status = SupplierStatus.Active,
            IsActive = true,
            CreatedBy = "System",
            CreatedOnUtc = DateTime.UtcNow
        };

        await _supplierRepository.AddAsync(supplier);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return supplier.Id;
    }
}
