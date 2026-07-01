using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.Currencies;

public record CurrencyDto(
    Guid Id,
    string Code,
    string Name,
    string Symbol,
    decimal ExchangeRate,
    bool IsDefault,
    bool IsActive
);

// ─── QUERIES ──────────────────────────────────────────────────────────────────

public record GetCurrenciesQuery : IRequest<IEnumerable<CurrencyDto>>;

public class GetCurrenciesQueryHandler : IRequestHandler<GetCurrenciesQuery, IEnumerable<CurrencyDto>>
{
    private readonly IRepository<Currency> _currencyRepository;

    public GetCurrenciesQueryHandler(IRepository<Currency> currencyRepository)
    {
        _currencyRepository = currencyRepository;
    }

    public async Task<IEnumerable<CurrencyDto>> Handle(GetCurrenciesQuery request, CancellationToken cancellationToken)
    {
        var currencies = await _currencyRepository.GetAllAsync();
        return currencies.Select(c => new CurrencyDto(
            c.Id,
            c.Code,
            c.Name,
            c.Symbol,
            c.ExchangeRate,
            c.IsDefault,
            c.IsActive
        )).ToList();
    }
}

// ─── COMMANDS ─────────────────────────────────────────────────────────────────

public record CreateCurrencyCommand(
    string Code,
    string Name,
    string Symbol,
    decimal ExchangeRate,
    bool IsDefault,
    bool IsActive
) : IRequest<Guid>;

public class CreateCurrencyCommandValidator : AbstractValidator<CreateCurrencyCommand>
{
    private readonly IRepository<Currency> _currencyRepository;

    public CreateCurrencyCommandValidator(IRepository<Currency> currencyRepository)
    {
        _currencyRepository = currencyRepository;

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("El código de la moneda es requerido.")
            .MaximumLength(10).WithMessage("El código no puede exceder 10 caracteres.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la moneda es requerido.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.");

        RuleFor(x => x.Symbol)
            .NotEmpty().WithMessage("El símbolo de la moneda es requerido.")
            .MaximumLength(10).WithMessage("El símbolo no puede exceder 10 caracteres.");

        RuleFor(x => x.ExchangeRate)
            .GreaterThan(0).WithMessage("La tasa de cambio debe ser mayor a 0.");
    }
}

public class CreateCurrencyCommandHandler : IRequestHandler<CreateCurrencyCommand, Guid>
{
    private readonly IRepository<Currency> _currencyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCurrencyCommandHandler(IRepository<Currency> currencyRepository, IUnitOfWork unitOfWork)
    {
        _currencyRepository = currencyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateCurrencyCommand request, CancellationToken cancellationToken)
    {
        // Si la nueva moneda es default, quitar default a las demás
        if (request.IsDefault)
        {
            var currencies = await _currencyRepository.GetAllAsync();
            foreach (var existing in currencies.Where(c => c.IsDefault))
            {
                existing.IsDefault = false;
                existing.ExchangeRate = 1.0m; // Fallback
                _currencyRepository.Update(existing);
            }
        }

        var currency = new Currency
        {
            Code = request.Code.ToUpper().Trim(),
            Name = request.Name.Trim(),
            Symbol = request.Symbol.Trim(),
            ExchangeRate = request.IsDefault ? 1.0m : request.ExchangeRate,
            IsDefault = request.IsDefault,
            IsActive = request.IsActive
        };

        await _currencyRepository.AddAsync(currency);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return currency.Id;
    }
}

public record UpdateCurrencyCommand(
    Guid Id,
    string Code,
    string Name,
    string Symbol,
    decimal ExchangeRate,
    bool IsDefault,
    bool IsActive
) : IRequest<bool>;

public class UpdateCurrencyCommandValidator : AbstractValidator<UpdateCurrencyCommand>
{
    public UpdateCurrencyCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("El código de la moneda es requerido.")
            .MaximumLength(10).WithMessage("El código no puede exceder 10 caracteres.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la moneda es requerido.")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres.");

        RuleFor(x => x.Symbol)
            .NotEmpty().WithMessage("El símbolo de la moneda es requerido.")
            .MaximumLength(10).WithMessage("El símbolo no puede exceder 10 caracteres.");

        RuleFor(x => x.ExchangeRate)
            .GreaterThan(0).WithMessage("La tasa de cambio debe ser mayor a 0.");
    }
}

public class UpdateCurrencyCommandHandler : IRequestHandler<UpdateCurrencyCommand, bool>
{
    private readonly IRepository<Currency> _currencyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCurrencyCommandHandler(IRepository<Currency> currencyRepository, IUnitOfWork unitOfWork)
    {
        _currencyRepository = currencyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateCurrencyCommand request, CancellationToken cancellationToken)
    {
        var currency = await _currencyRepository.GetByIdAsync(request.Id);
        if (currency == null) return false;

        // Si se establece como default, quitar default a las demás
        if (request.IsDefault && !currency.IsDefault)
        {
            var currencies = await _currencyRepository.GetAllAsync();
            foreach (var existing in currencies.Where(c => c.IsDefault && c.Id != currency.Id))
            {
                existing.IsDefault = false;
                _currencyRepository.Update(existing);
            }
        }

        currency.Code = request.Code.ToUpper().Trim();
        currency.Name = request.Name.Trim();
        currency.Symbol = request.Symbol.Trim();
        currency.ExchangeRate = request.IsDefault ? 1.0m : request.ExchangeRate;
        currency.IsDefault = request.IsDefault;
        currency.IsActive = request.IsActive;

        _currencyRepository.Update(currency);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}

public record DeleteCurrencyCommand(Guid Id) : IRequest<bool>;

public class DeleteCurrencyCommandHandler : IRequestHandler<DeleteCurrencyCommand, bool>
{
    private readonly IRepository<Currency> _currencyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCurrencyCommandHandler(IRepository<Currency> currencyRepository, IUnitOfWork unitOfWork)
    {
        _currencyRepository = currencyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteCurrencyCommand request, CancellationToken cancellationToken)
    {
        var currency = await _currencyRepository.GetByIdAsync(request.Id);
        if (currency == null || currency.IsDefault) return false; // No se puede eliminar la moneda por defecto

        currency.IsDeleted = true;
        _currencyRepository.Update(currency);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
