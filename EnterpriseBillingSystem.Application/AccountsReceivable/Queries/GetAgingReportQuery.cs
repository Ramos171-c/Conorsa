using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;
using EnterpriseBillingSystem.Domain.Enums;

namespace EnterpriseBillingSystem.Application.AccountsReceivable.Queries;

public record AgingCustomerRowDto(
    Guid CustomerId,
    string CustomerName,
    string CustomerCode,
    decimal TotalOutstanding,
    decimal Current,
    decimal Overdue1To30,
    decimal Overdue31To60,
    decimal Overdue61To90,
    decimal OverdueMoreThan90
);

public record GetAgingReportQuery() : IRequest<List<AgingCustomerRowDto>>;

public class GetAgingReportQueryHandler : IRequestHandler<GetAgingReportQuery, List<AgingCustomerRowDto>>
{
    private readonly IAccountsReceivableRepository _arRepository;

    public GetAgingReportQueryHandler(IAccountsReceivableRepository arRepository)
    {
        _arRepository = arRepository;
    }

    public async Task<List<AgingCustomerRowDto>> Handle(GetAgingReportQuery request, CancellationToken cancellationToken)
    {
        // 1. Obtener todas las CxC activas con saldo > 0
        var allArs = await _arRepository.FindAsync(a => 
            a.CurrentBalance > 0 && 
            a.Status != AccountsReceivableStatus.Paid && 
            a.Status != AccountsReceivableStatus.Cancelled);

        var activeArsList = allArs.Where(a => !a.IsDeleted).ToList();

        // 2. Cargar detalles del cliente de forma manual (ya que el repositorio genérico no hace includes)
        // Agrupamos por CustomerId y cargamos detalles
        var groupedByCustomer = activeArsList.GroupBy(a => a.CustomerId);
        var reportRows = new List<AgingCustomerRowDto>();
        var today = DateTime.UtcNow.Date;

        foreach (var group in groupedByCustomer)
        {
            Guid customerId = group.Key;
            decimal totalOutstanding = 0;
            decimal current = 0;
            decimal overdue1To30 = 0;
            decimal overdue31To60 = 0;
            decimal overdue61To90 = 0;
            decimal overdueMoreThan90 = 0;

            string customerName = "N/A";
            string customerCode = "N/A";

            foreach (var arItem in group)
            {
                // Cargar con detalles del cliente si aún no tenemos el nombre/código
                var arDetail = await _arRepository.GetByIdWithDetailsAsync(arItem.Id, cancellationToken);
                if (arDetail != null && arDetail.Customer != null)
                {
                    customerName = arDetail.Customer.Name;
                    customerCode = arDetail.Customer.CustomerCode;
                }

                decimal balance = arItem.CurrentBalance;
                totalOutstanding += balance;

                if (today <= arItem.DueDate.Date)
                {
                    current += balance;
                }
                else
                {
                    int daysOverdue = (today - arItem.DueDate.Date).Days;
                    if (daysOverdue <= 30)
                    {
                        overdue1To30 += balance;
                    }
                    else if (daysOverdue <= 60)
                    {
                        overdue31To60 += balance;
                    }
                    else if (daysOverdue <= 90)
                    {
                        overdue61To90 += balance;
                    }
                    else
                    {
                        overdueMoreThan90 += balance;
                    }
                }
            }

            reportRows.Add(new AgingCustomerRowDto(
                customerId,
                customerName,
                customerCode,
                totalOutstanding,
                current,
                overdue1To30,
                overdue31To60,
                overdue61To90,
                overdueMoreThan90
            ));
        }

        return reportRows.OrderBy(r => r.CustomerName).ToList();
    }
}
