using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.UnitsOfMeasure.DTOs;
using EnterpriseBillingSystem.Application.Common.Models;

namespace EnterpriseBillingSystem.Application.UnitsOfMeasure.Queries;

public record GetUnitsOfMeasurePagedQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null
) : IRequest<PagedResult<UnitOfMeasureDto>>;

public class GetUnitsOfMeasurePagedQueryHandler : IRequestHandler<GetUnitsOfMeasurePagedQuery, PagedResult<UnitOfMeasureDto>>
{
    private readonly IUnitOfMeasureRepository _uomRepository;

    public GetUnitsOfMeasurePagedQueryHandler(IUnitOfMeasureRepository uomRepository)
    {
        _uomRepository = uomRepository;
    }

    public async Task<PagedResult<UnitOfMeasureDto>> Handle(GetUnitsOfMeasurePagedQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _uomRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            cancellationToken);

        var dtos = items.Select(u => new UnitOfMeasureDto(
            u.Id,
            u.Code,
            u.Name,
            u.IsActive
        )).ToList();

        return new PagedResult<UnitOfMeasureDto>(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}
