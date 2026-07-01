using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using EnterpriseBillingSystem.Domain.Repositories;

namespace EnterpriseBillingSystem.Application.AccountsPayable.Queries;

public record AgingSupplierRowDto(
    Guid SupplierId,
    string SupplierName,
    string SupplierCode,
    decimal TotalOutstanding,
    decimal Current,
    decimal Overdue1To30,
    decimal Overdue31To60,
    decimal Overdue61To90,
    decimal OverdueMoreThan90
);

public record GetAccountsPayableAgingReportQuery() : IRequest<List<AgingSupplierRowDto>>;

public class GetAccountsPayableAgingReportQueryHandler : IRequestHandler<GetAccountsPayableAgingReportQuery, List<AgingSupplierRowDto>>
{
    private readonly IAccountsPayableRepository _apRepository;

    public GetAccountsPayableAgingReportQueryHandler(IAccountsPayableRepository apRepository)
    {
        _apRepository = apRepository;
    }

    public async Task<List<AgingSupplierRowDto>> Handle(GetAccountsPayableAgingReportQuery request, CancellationToken cancellationToken)
    {
        // 1. Obtener todas las CxP activas con proveedores cargados (Cero consultas N+1)
        var activeAps = await _apRepository.GetActiveWithSuppliersAsync(cancellationToken);
        var activeApsList = activeAps.ToList();

        // 2. Agrupar por Proveedor
        var groupedBySupplier = activeApsList.GroupBy(a => a.SupplierId);
        var reportRows = new List<AgingSupplierRowDto>();
        var today = DateTime.UtcNow.Date;

        foreach (var group in groupedBySupplier)
        {
            Guid supplierId = group.Key;
            decimal totalOutstanding = 0;
            decimal current = 0;
            decimal overdue1To30 = 0;
            decimal overdue31To60 = 0;
            decimal overdue61To90 = 0;
            decimal overdueMoreThan90 = 0;

            string supplierName = "N/A";
            string supplierCode = "N/A";

            // El proveedor está precargado por el Include del repositorio
            var firstItem = group.FirstOrDefault();
            if (firstItem != null && firstItem.Supplier != null)
            {
                supplierName = firstItem.Supplier.Name;
                supplierCode = firstItem.Supplier.SupplierCode;
            }

            foreach (var apItem in group)
            {
                decimal balance = apItem.CurrentBalance;
                totalOutstanding += balance;

                if (today <= apItem.DueDate.Date)
                {
                    current += balance;
                }
                else
                {
                    int daysOverdue = (today - apItem.DueDate.Date).Days;
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

            reportRows.Add(new AgingSupplierRowDto(
                supplierId,
                supplierName,
                supplierCode,
                totalOutstanding,
                current,
                overdue1To30,
                overdue31To60,
                overdue61To90,
                overdueMoreThan90
            ));
        }

        return reportRows.OrderBy(r => r.SupplierName).ToList();
    }
}
