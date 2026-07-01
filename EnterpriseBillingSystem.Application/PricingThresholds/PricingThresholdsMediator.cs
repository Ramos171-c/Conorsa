using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.PricingThresholds;

public record PricingThresholdDto(
    Guid Id,
    string LevelName,
    decimal MinimumSubtotal,
    bool IsActive
);

public record ThresholdUpdateInput(
    Guid Id,
    decimal MinimumSubtotal,
    bool IsActive
);

// ─── QUERIES ──────────────────────────────────────────────────────────────────

public record GetPricingThresholdsQuery : IRequest<IEnumerable<PricingThresholdDto>>;

public class GetPricingThresholdsQueryHandler : IRequestHandler<GetPricingThresholdsQuery, IEnumerable<PricingThresholdDto>>
{
    private readonly IRepository<PricingThreshold> _thresholdRepository;

    public GetPricingThresholdsQueryHandler(IRepository<PricingThreshold> thresholdRepository)
    {
        _thresholdRepository = thresholdRepository;
    }

    public async Task<IEnumerable<PricingThresholdDto>> Handle(GetPricingThresholdsQuery request, CancellationToken cancellationToken)
    {
        var thresholds = await _thresholdRepository.GetAllAsync();
        return thresholds.Select(t => new PricingThresholdDto(
            t.Id,
            t.LevelName,
            t.MinimumSubtotal,
            t.IsActive
        )).ToList();
    }
}

// ─── COMMANDS ─────────────────────────────────────────────────────────────────

public record UpdatePricingThresholdsCommand(
    List<ThresholdUpdateInput> Thresholds
) : IRequest<bool>;

public class UpdatePricingThresholdsCommandValidator : AbstractValidator<UpdatePricingThresholdsCommand>
{
    public UpdatePricingThresholdsCommandValidator()
    {
        RuleFor(x => x.Thresholds)
            .NotEmpty().WithMessage("La lista de umbrales no puede estar vacía.");
    }
}

public class UpdatePricingThresholdsCommandHandler : IRequestHandler<UpdatePricingThresholdsCommand, bool>
{
    private readonly IRepository<PricingThreshold> _thresholdRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePricingThresholdsCommandHandler(IRepository<PricingThreshold> thresholdRepository, IUnitOfWork unitOfWork)
    {
        _thresholdRepository = thresholdRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdatePricingThresholdsCommand request, CancellationToken cancellationToken)
    {
        foreach (var input in request.Thresholds)
        {
            var threshold = await _thresholdRepository.GetByIdAsync(input.Id);
            if (threshold != null)
            {
                threshold.MinimumSubtotal = input.MinimumSubtotal;
                threshold.IsActive = input.IsActive;
                _thresholdRepository.Update(threshold);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
