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
    private readonly IUnitOfWork _unitOfWork;

    public RoutesController(IRepository<RouteEntity> routeRepository, IUnitOfWork unitOfWork)
    {
        _routeRepository = routeRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<List<RouteDto>>> GetAll([FromQuery] bool includeInactive = false)
    {
        var allRoutes = await _routeRepository.GetAllAsync();
        var query = allRoutes.AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(r => r.IsActive);
        }
        var routes = query
            .OrderBy(r => r.Name)
            .Select(r => new RouteDto(r.Id, r.Code, r.Name, r.IsActive))
            .ToList();

        return Ok(routes);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateRouteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { Message = "El código y nombre de la ruta son requeridos." });
        }

        var existing = await _routeRepository.FindAsync(r => r.Code == dto.Code);
        if (existing.Any())
        {
            return BadRequest(new { Message = "El código de la ruta ya está registrado." });
        }

        var route = new RouteEntity
        {
            Code = dto.Code.Trim(),
            Name = dto.Name.Trim(),
            IsActive = true
        };

        await _routeRepository.AddAsync(route);
        await _unitOfWork.SaveChangesAsync();

        return Ok(route.Id);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateRouteDto dto)
    {
        var route = await _routeRepository.GetByIdAsync(id);
        if (route == null) return NotFound();

        if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { Message = "El código y nombre de la ruta son requeridos." });
        }

        var existing = await _routeRepository.FindAsync(r => r.Code == dto.Code && r.Id != id);
        if (existing.Any())
        {
            return BadRequest(new { Message = "El código de la ruta ya está registrado en otra ruta." });
        }

        route.Code = dto.Code.Trim();
        route.Name = dto.Name.Trim();
        route.IsActive = dto.IsActive;

        _routeRepository.Update(route);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var route = await _routeRepository.GetByIdAsync(id);
        if (route == null) return NotFound();

        route.IsActive = false;
        _routeRepository.Update(route);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }
}

public record RouteDto(Guid Id, string Code, string Name, bool IsActive);
public record CreateRouteDto(string Code, string Name);
public record UpdateRouteDto(string Code, string Name, bool IsActive);
