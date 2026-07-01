using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EnterpriseBillingSystem.Domain.Repositories;
using RouteEntity = EnterpriseBillingSystem.Domain.Entities.Route;

namespace EnterpriseBillingSystem.WebApi.Controllers;

[Route("api/v1/routes")]
public class RoutesController : ApiControllerBase
{
    private readonly IRepository<RouteEntity> _routeRepository;

    public RoutesController(IRepository<RouteEntity> routeRepository)
    {
        _routeRepository = routeRepository;
    }

    [HttpGet]
    public async Task<ActionResult<List<RouteDto>>> GetAll()
    {
        var allRoutes = await _routeRepository.GetAllAsync();
        var routes = allRoutes
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .Select(r => new RouteDto(r.Id, r.Code, r.Name))
            .ToList();

        return Ok(routes);
    }
}

public record RouteDto(Guid Id, string Code, string Name);
