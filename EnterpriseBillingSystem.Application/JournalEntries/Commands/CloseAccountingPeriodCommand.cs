using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.Common.Interfaces;

namespace EnterpriseBillingSystem.Application.JournalEntries.Commands;

public record CloseAccountingPeriodCommand(
    int Year,
    int Month
) : IRequest<Unit>;

public class CloseAccountingPeriodCommandValidator : AbstractValidator<CloseAccountingPeriodCommand>
{
    public CloseAccountingPeriodCommandValidator()
    {
        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100).WithMessage("El año debe estar entre 2000 y 2100.");

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12).WithMessage("El mes debe estar entre 1 y 12.");
    }
}

public class CloseAccountingPeriodCommandHandler : IRequestHandler<CloseAccountingPeriodCommand, Unit>
{
    private readonly IRepository<AccountingPeriod> _periodRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CloseAccountingPeriodCommandHandler(
        IRepository<AccountingPeriod> periodRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _periodRepository = periodRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(CloseAccountingPeriodCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId ?? "System";

        var periods = await _periodRepository.FindAsync(p => p.Year == request.Year && p.Month == request.Month);
        var period = periods.FirstOrDefault();

        if (period == null)
        {
            period = new AccountingPeriod
            {
                Id = Guid.NewGuid(),
                Year = request.Year,
                Month = request.Month,
                IsClosed = true,
                ClosedBy = currentUserId,
                ClosedAt = DateTime.UtcNow,
                CreatedBy = currentUserId,
                CreatedOnUtc = DateTime.UtcNow
            };
            await _periodRepository.AddAsync(period);
        }
        else
        {
            if (period.IsClosed)
            {
                throw new InvalidOperationException($"El período contable {request.Year}/{request.Month} ya se encuentra cerrado.");
            }

            period.IsClosed = true;
            period.ClosedBy = currentUserId;
            period.ClosedAt = DateTime.UtcNow;
            period.LastModifiedBy = currentUserId;
            period.LastModifiedOnUtc = DateTime.UtcNow;
            _periodRepository.Update(period);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
