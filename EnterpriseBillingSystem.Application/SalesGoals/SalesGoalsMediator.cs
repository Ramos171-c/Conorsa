using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using EnterpriseBillingSystem.Domain.Entities;
using EnterpriseBillingSystem.Domain.Enums;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Application.Common.Interfaces;

namespace EnterpriseBillingSystem.Application.SalesGoals;

public record SalesGoalDto(
    Guid Id,
    Guid UserId,
    string UserName,
    string UserFullName,
    string PeriodName,
    decimal TargetAmount,
    decimal CurrentAmount,
    double ProgressPercentage,
    decimal RemainingAmount,
    DateTime StartDate,
    DateTime EndDate,
    bool IsActive
);

// ─── QUERIES ──────────────────────────────────────────────────────────────────

public record GetSalesGoalsQuery(Guid? UserId = null) : IRequest<IEnumerable<SalesGoalDto>>;

public class GetSalesGoalsQueryHandler : IRequestHandler<GetSalesGoalsQuery, IEnumerable<SalesGoalDto>>
{
    private readonly IRepository<SalesGoal> _goalRepository;
    private readonly IRepository<SalesInvoice> _invoiceRepository;
    private readonly IRepository<SalesOrder> _orderRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public GetSalesGoalsQueryHandler(
        IRepository<SalesGoal> goalRepository,
        IRepository<SalesInvoice> invoiceRepository,
        IRepository<SalesOrder> orderRepository,
        UserManager<ApplicationUser> userManager)
    {
        _goalRepository = goalRepository;
        _invoiceRepository = invoiceRepository;
        _orderRepository = orderRepository;
        _userManager = userManager;
    }

    public async Task<IEnumerable<SalesGoalDto>> Handle(GetSalesGoalsQuery request, CancellationToken cancellationToken)
    {
        var goals = await _goalRepository.GetAllAsync();
        if (request.UserId.HasValue)
        {
            goals = goals.Where(g => g.UserId == request.UserId.Value);
        }

        var resultList = new List<SalesGoalDto>();

        foreach (var goal in goals)
        {
            var user = await _userManager.FindByIdAsync(goal.UserId.ToString());

            // 1. Facturas directas (Posted, creadas por el usuario, sin pedido de origen)
            var invoices = await _invoiceRepository.FindAsync(i =>
                i.Status == SalesInvoiceStatus.Posted &&
                i.CreatedBy == goal.UserId.ToString() &&
                i.SalesOrderId == null &&
                i.InvoiceDate >= goal.StartDate &&
                i.InvoiceDate <= goal.EndDate);

            decimal invoicesSum = invoices.Sum(i => i.TotalAmount);

            // 2. Pedidos capturados (No anulados, creados por el usuario)
            var orders = await _orderRepository.FindAsync(o =>
                o.Status != SalesOrderStatus.Anulado &&
                o.CreatedBy == goal.UserId.ToString() &&
                o.OrderDate >= goal.StartDate &&
                o.OrderDate <= goal.EndDate);

            decimal ordersSum = orders.Sum(o => o.TotalAmount);

            decimal currentSum = invoicesSum + ordersSum;
            double progressPercent = goal.TargetAmount > 0 
                ? (double)(currentSum / goal.TargetAmount) * 100 
                : 0.0;
            decimal remaining = Math.Max(0m, goal.TargetAmount - currentSum);

            resultList.Add(new SalesGoalDto(
                goal.Id,
                goal.UserId,
                user?.UserName ?? "N/A",
                user != null ? $"{user.FirstName} {user.LastName}".Trim() : "N/A",
                goal.PeriodName,
                goal.TargetAmount,
                currentSum,
                progressPercent,
                remaining,
                goal.StartDate,
                goal.EndDate,
                goal.IsActive
            ));
        }

        return resultList;
    }
}

public record GetMySalesGoalsQuery : IRequest<IEnumerable<SalesGoalDto>>;

public class GetMySalesGoalsQueryHandler : IRequestHandler<GetMySalesGoalsQuery, IEnumerable<SalesGoalDto>>
{
    private readonly IRepository<SalesGoal> _goalRepository;
    private readonly IRepository<SalesInvoice> _invoiceRepository;
    private readonly IRepository<SalesOrder> _orderRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;

    public GetMySalesGoalsQueryHandler(
        IRepository<SalesGoal> goalRepository,
        IRepository<SalesInvoice> invoiceRepository,
        IRepository<SalesOrder> orderRepository,
        ICurrentUserService currentUserService,
        UserManager<ApplicationUser> userManager)
    {
        _goalRepository = goalRepository;
        _invoiceRepository = invoiceRepository;
        _orderRepository = orderRepository;
        _currentUserService = currentUserService;
        _userManager = userManager;
    }

    public async Task<IEnumerable<SalesGoalDto>> Handle(GetMySalesGoalsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUserService.UserId) || !Guid.TryParse(_currentUserService.UserId, out Guid currentUserId))
        {
            return Enumerable.Empty<SalesGoalDto>();
        }

        var goals = await _goalRepository.FindAsync(g => g.UserId == currentUserId);
        var resultList = new List<SalesGoalDto>();
        var user = await _userManager.FindByIdAsync(currentUserId.ToString());

        foreach (var goal in goals)
        {
            // 1. Facturas directas (Posted, creadas por el usuario, sin pedido de origen)
            var invoices = await _invoiceRepository.FindAsync(i =>
                i.Status == SalesInvoiceStatus.Posted &&
                i.CreatedBy == currentUserId.ToString() &&
                i.SalesOrderId == null &&
                i.InvoiceDate >= goal.StartDate &&
                i.InvoiceDate <= goal.EndDate);

            decimal invoicesSum = invoices.Sum(i => i.TotalAmount);

            // 2. Pedidos capturados (No anulados, creados por el usuario)
            var orders = await _orderRepository.FindAsync(o =>
                o.Status != SalesOrderStatus.Anulado &&
                o.CreatedBy == currentUserId.ToString() &&
                o.OrderDate >= goal.StartDate &&
                o.OrderDate <= goal.EndDate);

            decimal ordersSum = orders.Sum(o => o.TotalAmount);

            decimal currentSum = invoicesSum + ordersSum;
            double progressPercent = goal.TargetAmount > 0 
                ? (double)(currentSum / goal.TargetAmount) * 100 
                : 0.0;
            decimal remaining = Math.Max(0m, goal.TargetAmount - currentSum);

            resultList.Add(new SalesGoalDto(
                goal.Id,
                goal.UserId,
                user?.UserName ?? _currentUserService.UserId,
                user != null ? $"{user.FirstName} {user.LastName}".Trim() : "Vendedor",
                goal.PeriodName,
                goal.TargetAmount,
                currentSum,
                progressPercent,
                remaining,
                goal.StartDate,
                goal.EndDate,
                goal.IsActive
            ));
        }

        return resultList;
    }
}

// ─── COMMANDS ─────────────────────────────────────────────────────────────────

public record CreateSalesGoalCommand(
    Guid UserId,
    string PeriodName,
    decimal TargetAmount,
    DateTime StartDate,
    DateTime EndDate,
    bool IsActive
) : IRequest<Guid>;

public class CreateSalesGoalCommandValidator : AbstractValidator<CreateSalesGoalCommand>
{
    public CreateSalesGoalCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("El usuario es requerido.");
        RuleFor(x => x.PeriodName).NotEmpty().WithMessage("El periodo de la meta es requerido.");
        RuleFor(x => x.TargetAmount).GreaterThan(0).WithMessage("El monto meta debe ser mayor a 0.");
        RuleFor(x => x.StartDate).LessThan(x => x.EndDate).WithMessage("La fecha de inicio debe ser anterior a la fecha de fin.");
    }
}

public class CreateSalesGoalCommandHandler : IRequestHandler<CreateSalesGoalCommand, Guid>
{
    private readonly IRepository<SalesGoal> _goalRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSalesGoalCommandHandler(IRepository<SalesGoal> goalRepository, IUnitOfWork unitOfWork)
    {
        _goalRepository = goalRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateSalesGoalCommand request, CancellationToken cancellationToken)
    {
        var goal = new SalesGoal
        {
            UserId = request.UserId,
            PeriodName = request.PeriodName.Trim(),
            TargetAmount = request.TargetAmount,
            CurrentAmount = 0m,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = request.IsActive
        };

        await _goalRepository.AddAsync(goal);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return goal.Id;
    }
}

public record UpdateSalesGoalCommand(
    Guid Id,
    Guid UserId,
    string PeriodName,
    decimal TargetAmount,
    DateTime StartDate,
    DateTime EndDate,
    bool IsActive
) : IRequest<bool>;

public class UpdateSalesGoalCommandValidator : AbstractValidator<UpdateSalesGoalCommand>
{
    public UpdateSalesGoalCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("El usuario es requerido.");
        RuleFor(x => x.PeriodName).NotEmpty().WithMessage("El periodo de la meta es requerido.");
        RuleFor(x => x.TargetAmount).GreaterThan(0).WithMessage("El monto meta debe ser mayor a 0.");
        RuleFor(x => x.StartDate).LessThan(x => x.EndDate).WithMessage("La fecha de inicio debe ser anterior a la fecha de fin.");
    }
}

public class UpdateSalesGoalCommandHandler : IRequestHandler<UpdateSalesGoalCommand, bool>
{
    private readonly IRepository<SalesGoal> _goalRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSalesGoalCommandHandler(IRepository<SalesGoal> goalRepository, IUnitOfWork unitOfWork)
    {
        _goalRepository = goalRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateSalesGoalCommand request, CancellationToken cancellationToken)
    {
        var goal = await _goalRepository.GetByIdAsync(request.Id);
        if (goal == null) return false;

        goal.UserId = request.UserId;
        goal.PeriodName = request.PeriodName.Trim();
        goal.TargetAmount = request.TargetAmount;
        goal.StartDate = request.StartDate;
        goal.EndDate = request.EndDate;
        goal.IsActive = request.IsActive;

        _goalRepository.Update(goal);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}

public record DeleteSalesGoalCommand(Guid Id) : IRequest<bool>;

public class DeleteSalesGoalCommandHandler : IRequestHandler<DeleteSalesGoalCommand, bool>
{
    private readonly IRepository<SalesGoal> _goalRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSalesGoalCommandHandler(IRepository<SalesGoal> goalRepository, IUnitOfWork unitOfWork)
    {
        _goalRepository = goalRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteSalesGoalCommand request, CancellationToken cancellationToken)
    {
        var goal = await _goalRepository.GetByIdAsync(request.Id);
        if (goal == null) return false;

        goal.IsDeleted = true;
        _goalRepository.Update(goal);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
