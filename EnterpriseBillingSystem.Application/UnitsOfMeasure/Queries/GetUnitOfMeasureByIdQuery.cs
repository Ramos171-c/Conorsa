using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.UnitsOfMeasure.DTOs;

namespace EnterpriseBillingSystem.Application.UnitsOfMeasure.Queries;

public record GetUnitOfMeasureByIdQuery(Guid Id) : IRequest<UnitOfMeasureDto?>;

public class GetUnitOfMeasureByIdQueryHandler : IRequestHandler<GetUnitOfMeasureByIdQuery, UnitOfMeasureDto?>
{
    private readonly IRepository<UnitOfMeasure> _uomRepository;

    public GetUnitOfMeasureByIdQueryHandler(IRepository<UnitOfMeasure> uomRepository)
    {
        _uomRepository = uomRepository;
    }

    public async Task<UnitOfMeasureDto?> Handle(GetUnitOfMeasureByIdQuery request, CancellationToken cancellationToken)
    {
        var uom = await _uomRepository.GetByIdAsync(request.Id);
        if (uom == null) return null;

        return new UnitOfMeasureDto(
            Id: uom.Id,
            Code: uom.Code,
            Name: uom.Name,
            IsActive: uom.IsActive
        );
    }
}
