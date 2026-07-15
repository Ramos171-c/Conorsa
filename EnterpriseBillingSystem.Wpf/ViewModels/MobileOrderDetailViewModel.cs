using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnterpriseBillingSystem.Wpf.Models;
using EnterpriseBillingSystem.Wpf.Services.Api;
using EnterpriseBillingSystem.Wpf.Services.Dialogs;

namespace EnterpriseBillingSystem.Wpf.ViewModels;

public partial class MobileOrderDetailViewModel : ViewModelBase
{
    private readonly SalesApiClient _salesApiClient;
    private readonly CustomerApiClient _customerApiClient;
    private readonly INotificationService _notificationService;
    private readonly SalesOrderDetailDto _order;

    public event Action? RequestClose;
    public event Action? OrderActionTaken;

    [ObservableProperty]
    private string? _dispatcherNotes;

    [ObservableProperty]
    private string _orderNumber = string.Empty;

    [ObservableProperty]
    private string _customerName = string.Empty;

    [ObservableProperty]
    private string _customerCode = string.Empty;

    [ObservableProperty]
    private DateTime _orderDate;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private decimal _subTotal;

    [ObservableProperty]
    private decimal _discountAmount;

    [ObservableProperty]
    private decimal _taxAmount;

    [ObservableProperty]
    private decimal _totalAmount;

    [ObservableProperty]
    private bool _isActionEnabled;

    [ObservableProperty]
    private bool _isProcessing;

    public ObservableCollection<SalesOrderDetailItemDto> Details { get; } = new();

    [ObservableProperty]
    private string _selectedNewStatus = string.Empty;

    public ObservableCollection<string> AvailableStatuses { get; } = new()
    {
        "Recibido",
        "EnProceso",
        "EnCamino",
        "Completado",
        "Anulado"
    };

    public bool IsCancellationRequested => Status.Equals("SolicitudAnulacion", StringComparison.OrdinalIgnoreCase) || Status.Equals("7", StringComparison.OrdinalIgnoreCase);

    public string CancellationReason
    {
        get
        {
            if (string.IsNullOrEmpty(Notes)) return "No especificado";
            int index = Notes.IndexOf("[SOLICITUD ANULACIÓN]:", StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                return Notes.Substring(index + "[SOLICITUD ANULACIÓN]:".Length).Trim();
            }
            return Notes;
        }
    }

    public bool IsStatusChangeVisible => !Status.Equals("Anulado", StringComparison.OrdinalIgnoreCase) && 
                                         !Status.Equals("Completado", StringComparison.OrdinalIgnoreCase) &&
                                         !Status.Equals("SolicitudAnulacion", StringComparison.OrdinalIgnoreCase);


    public MobileOrderDetailViewModel(SalesApiClient salesApiClient, CustomerApiClient customerApiClient, INotificationService notificationService, SalesOrderDetailDto order)
    {
        _salesApiClient = salesApiClient;
        _customerApiClient = customerApiClient;
        _notificationService = notificationService;
        _order = order;

        OrderNumber = order.OrderNumber;
        CustomerName = order.CustomerName;
        CustomerCode = order.CustomerCode;
        OrderDate = order.OrderDate;
        Notes = order.Notes;
        Status = order.Status;
        SubTotal = order.SubTotal;
        DiscountAmount = order.DiscountAmount;
        TaxAmount = order.TaxAmount;
        TotalAmount = order.TotalAmount;

        IsActionEnabled = order.Status.Equals("Recibido", StringComparison.OrdinalIgnoreCase);
        SelectedNewStatus = AvailableStatuses.Contains(order.Status) ? order.Status : AvailableStatuses[0];

        foreach (var item in order.Details)
        {
            Details.Add(item);
        }
    }

    [RelayCommand]
    private async Task ConfirmOrderAsync()
    {
        if (!IsActionEnabled) return;

        var confirm = Views.Dialogs.CustomMessageBox.Show(
            $"¿Está seguro de que desea confirmar el pedido {OrderNumber}?",
            "Confirmar Pedido",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        IsProcessing = true;
        try
        {
            var success = await _salesApiClient.ConfirmSalesOrderAsync(_order.Id);
            if (success)
            {
                _notificationService.ShowSuccess($"Pedido {OrderNumber} confirmado exitosamente.");
                OrderActionTaken?.Invoke();
                RequestClose?.Invoke();
            }
            else
            {
                _notificationService.ShowError("Error al confirmar el pedido.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al confirmar pedido: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task CancelOrderAsync()
    {
        if (!IsActionEnabled) return;

        var input = Views.Dialogs.CustomInputDialog.Show(
            "Escriba el motivo de la anulación del pedido:",
            "Motivo de Anulación",
            "Anulado por el Administrador");

        if (!input.IsConfirmed) return;

        string reason = input.Text;
        if (string.IsNullOrWhiteSpace(reason))
        {
            Views.Dialogs.CustomMessageBox.Show("El motivo es requerido.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsProcessing = true;
        try
        {
            var success = await _salesApiClient.CancelSalesOrderAsync(_order.Id, reason);
            if (success)
            {
                _notificationService.ShowSuccess($"Pedido {OrderNumber} anulado exitosamente.");
                OrderActionTaken?.Invoke();
                RequestClose?.Invoke();
            }
            else
            {
                _notificationService.ShowError("Error al anular el pedido.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al anular pedido: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task UpdateStatusAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedNewStatus)) return;

        if (SelectedNewStatus.Equals(Status, StringComparison.OrdinalIgnoreCase))
        {
            Views.Dialogs.CustomMessageBox.Show(
                "El pedido ya se encuentra en este estado.", 
                "Información", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
            return;
        }

        var confirm = Views.Dialogs.CustomMessageBox.Show(
            $"¿Está seguro de que desea cambiar el estado del pedido a '{SelectedNewStatus}'?",
            "Actualizar Estado",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        IsProcessing = true;
        try
        {
            int statusValue = SelectedNewStatus switch
            {
                "Solicitud" => 1,
                "Recibido" => 2,
                "Anulado" => 3,
                "EnProceso" => 4,
                "EnCamino" => 5,
                "Completado" => 6,
                _ => 1
            };

            var success = await _salesApiClient.UpdateSalesOrderStatusAsync(_order.Id, statusValue);
            if (success)
            {
                Status = SelectedNewStatus;
                IsActionEnabled = Status.Equals("Recibido", StringComparison.OrdinalIgnoreCase);
                OnPropertyChanged(nameof(IsStatusChangeVisible));
                
                _notificationService.ShowSuccess($"Estado del pedido {OrderNumber} actualizado a '{SelectedNewStatus}' exitosamente.");
                OrderActionTaken?.Invoke();
            }
            else
            {
                _notificationService.ShowError("Error al actualizar el estado del pedido.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al actualizar estado del pedido: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    public bool IsReturnEnabled => !Status.Equals("Anulado", StringComparison.OrdinalIgnoreCase);

    [RelayCommand]
    private async Task ReturnTotalAsync()
    {
        var confirm = Views.Dialogs.CustomMessageBox.Show(
            $"¿Está seguro de que desea procesar la DEVOLUCIÓN TOTAL para el pedido {OrderNumber}?",
            "Devolución Total",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        IsProcessing = true;
        try
        {
            var cmd = new ReturnSalesOrderCommandDto(_order.Id, null);
            var success = await _salesApiClient.ReturnSalesOrderAsync(_order.Id, cmd);
            if (success)
            {
                _notificationService.ShowSuccess($"Devolución total del pedido {OrderNumber} procesada exitosamente.");
                OrderActionTaken?.Invoke();
                RequestClose?.Invoke();
            }
            else
            {
                _notificationService.ShowError("Error al procesar la devolución total.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al procesar devolución: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task ReturnPartialAsync()
    {
        var itemsToReturn = Details.Where(d => d.ReturnedQuantity > 0).ToList();
        if (!itemsToReturn.Any())
        {
            Views.Dialogs.CustomMessageBox.Show(
                "Debe ingresar una cantidad a devolver mayor a 0 en al menos un producto.",
                "Advertencia",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        foreach (var item in itemsToReturn)
        {
            if (item.ReturnedQuantity < 0)
            {
                Views.Dialogs.CustomMessageBox.Show(
                    $"La cantidad a devolver para el producto '{item.ProductName}' no puede ser negativa.",
                    "Error de Validación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            if (item.ReturnedQuantity > item.Quantity)
            {
                Views.Dialogs.CustomMessageBox.Show(
                    $"La cantidad a devolver ({item.ReturnedQuantity}) no puede superar la cantidad del pedido ({item.Quantity}) para '{item.ProductName}'.",
                    "Error de Validación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
        }

        var confirm = Views.Dialogs.CustomMessageBox.Show(
            $"¿Está seguro de que desea procesar la devolución parcial de {itemsToReturn.Count} productos?",
            "Devolución Parcial",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        IsProcessing = true;
        try
        {
            var returnItems = itemsToReturn.Select(item => new ReturnSalesOrderDetailItemDto(item.Id, item.ReturnedQuantity)).ToList();
            var cmd = new ReturnSalesOrderCommandDto(_order.Id, returnItems);
            var success = await _salesApiClient.ReturnSalesOrderAsync(_order.Id, cmd);
            if (success)
            {
                _notificationService.ShowSuccess("Devolución parcial procesada exitosamente.");
                OrderActionTaken?.Invoke();
                RequestClose?.Invoke();
            }
            else
            {
                _notificationService.ShowError("Error al procesar la devolución parcial.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al procesar devolución parcial: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task ApproveCancellationAsync()
    {
        var confirm = Views.Dialogs.CustomMessageBox.Show(
            $"¿Está seguro de que desea APROBAR la solicitud de anulación del pedido {OrderNumber}? Esto anulará el pedido permanentemente.",
            "Aprobar Anulación",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        IsProcessing = true;
        try
        {
            var success = await _salesApiClient.CancelSalesOrderAsync(_order.Id, "Anulación aprobada por el administrador.");
            if (success)
            {
                _notificationService.ShowSuccess($"Solicitud de anulación aprobada. El pedido {OrderNumber} ha sido anulado.");
                OrderActionTaken?.Invoke();
                RequestClose?.Invoke();
            }
            else
            {
                _notificationService.ShowError("Error al aprobar la anulación.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al aprobar anulación: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task RejectCancellationAsync()
    {
        var confirm = Views.Dialogs.CustomMessageBox.Show(
            $"¿Está seguro de que desea RECHAZAR la solicitud de anulación del pedido {OrderNumber}? El pedido regresará a estado Recibido.",
            "Rechazar Anulación",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        IsProcessing = true;
        try
        {
            var success = await _salesApiClient.UpdateSalesOrderStatusAsync(_order.Id, 2); // 2 is Recibido
            if (success)
            {
                _notificationService.ShowSuccess($"Solicitud de anulación rechazada. El pedido {OrderNumber} ha regresado a estado Recibido.");
                OrderActionTaken?.Invoke();
                RequestClose?.Invoke();
            }
            else
            {
                _notificationService.ShowError("Error al rechazar la solicitud de anulación.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al rechazar solicitud: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task PrintDeliveryTicketAsync()
    {
        IsProcessing = true;
        try
        {
            // 1. Fetch full customer details to get Address and Route
            CustomerDto? customer = null;
            try
            {
                customer = await _customerApiClient.GetCustomerByIdAsync(_order.CustomerId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching customer details: {ex.Message}");
            }

            // 2. Build the FlowDocument dynamically
            var doc = new System.Windows.Documents.FlowDocument
            {
                PagePadding = new Thickness(30),
                ColumnWidth = double.PositiveInfinity,
                FontFamily = new System.Windows.Media.FontFamily("Courier New"),
                FontSize = 12,
                TextAlignment = TextAlignment.Left
            };

            var sec = new System.Windows.Documents.Section();

            // Header - Dulce y caramelos
            var headerPara = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Dulce y caramelos\n"))
            {
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center
            };
            headerPara.Inlines.Add(new System.Windows.Documents.Run("TICKET DE ENTREGA\n"));
            headerPara.Inlines.Add(new System.Windows.Documents.Run("==================================\n"));
            sec.Blocks.Add(headerPara);

            // Customer Details
            var custPara = new System.Windows.Documents.Paragraph();
            custPara.Inlines.Add(new System.Windows.Documents.Run($"Pedido No:   {OrderNumber}\n"));
            custPara.Inlines.Add(new System.Windows.Documents.Run($"Fecha:       {OrderDate:dd/MM/yyyy HH:mm}\n"));
            custPara.Inlines.Add(new System.Windows.Documents.Run($"Cliente:     {CustomerName} ({CustomerCode})\n"));
            
            if (customer != null)
            {
                custPara.Inlines.Add(new System.Windows.Documents.Run($"Ruta:        {customer.RouteName ?? "No asignada"}\n"));
                
                // Get default address
                var address = customer.Addresses?.FirstOrDefault(a => a.IsDefault) ?? customer.Addresses?.FirstOrDefault();
                if (address != null)
                {
                    custPara.Inlines.Add(new System.Windows.Documents.Run($"Dirección:   {address.AddressLine1}, {address.City}\n"));
                }
                
                // Get default phone
                var phone = customer.Phones?.FirstOrDefault()?.PhoneNumber;
                if (!string.IsNullOrEmpty(phone))
                {
                    custPara.Inlines.Add(new System.Windows.Documents.Run($"Teléfono:    {phone}\n"));
                }
            }
            custPara.Inlines.Add(new System.Windows.Documents.Run("==================================\n"));
            sec.Blocks.Add(custPara);

            // Order Lines
            var itemsPara = new System.Windows.Documents.Paragraph();
            itemsPara.Inlines.Add(new System.Windows.Documents.Run("DETALLE DEL PEDIDO\n"));
            itemsPara.Inlines.Add(new System.Windows.Documents.Run("------------------------------------\n"));
            
            foreach (var item in Details)
            {
                // Full product name (untruncated)
                itemsPara.Inlines.Add(new System.Windows.Documents.Run($"{item.ProductName}\n"));
                
                // Indented quantity with UOM aligned next to net price
                string qtyUom = $"{item.Quantity:N2} {item.UnitOfMeasure}";
                string net = $"C${item.NetAmount:N2}";
                itemsPara.Inlines.Add(new System.Windows.Documents.Run($"   {qtyUom.PadRight(18)} {net.PadLeft(11)}\n"));
            }
            itemsPara.Inlines.Add(new System.Windows.Documents.Run("------------------------------------\n"));
            sec.Blocks.Add(itemsPara);

            // Totals
            var totalsPara = new System.Windows.Documents.Paragraph
            {
                TextAlignment = TextAlignment.Right
            };
            totalsPara.Inlines.Add(new System.Windows.Documents.Run($"Subtotal:     C${SubTotal:N2}\n"));
            if (DiscountAmount > 0)
            {
                totalsPara.Inlines.Add(new System.Windows.Documents.Run($"Descuento:   -C${DiscountAmount:N2}\n"));
            }
            totalsPara.Inlines.Add(new System.Windows.Documents.Run($"TOTAL:        C${TotalAmount:N2}\n"));
            
            decimal totalUsd = TotalAmount / 36.5m;
            totalsPara.Inlines.Add(new System.Windows.Documents.Run($"TOTAL USD:     ${totalUsd:N2}\n"));
            totalsPara.Inlines.Add(new System.Windows.Documents.Run("==================================\n"));
            sec.Blocks.Add(totalsPara);

            // Observations
            bool showNotes = false;
            string notesText = "";
            if (!string.IsNullOrWhiteSpace(Notes))
            {
                var cleanNotes = Notes.Trim();
                bool isDefaultNote = string.Equals(cleanNotes, "Pedido desde POS movil (Vendedor)", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(cleanNotes, "Pedido desde POS Móvil (Vendedor)", StringComparison.OrdinalIgnoreCase);
                                     
                if (!isDefaultNote)
                {
                    showNotes = true;
                    notesText = Notes;
                }
            }

            if (showNotes)
            {
                var obsPara = new System.Windows.Documents.Paragraph();
                obsPara.Inlines.Add(new System.Windows.Documents.Run("OBSERVACIONES:\n"));
                obsPara.Inlines.Add(new System.Windows.Documents.Run($"- Vendedor:  {notesText}\n"));
                obsPara.Inlines.Add(new System.Windows.Documents.Run("==================================\n"));
                sec.Blocks.Add(obsPara);
            }

            doc.Blocks.Add(sec);

            // 3. Print
            var printDialog = new System.Windows.Controls.PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                doc.PageWidth = printDialog.PrintableAreaWidth;
                doc.PageHeight = printDialog.PrintableAreaHeight;
                
                var documentPaginator = ((System.Windows.Documents.IDocumentPaginatorSource)doc).DocumentPaginator;
                printDialog.PrintDocument(documentPaginator, $"Ticket_Entrega_{OrderNumber}");
                
                _notificationService.ShowSuccess("Ticket de entrega enviado a la impresora.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al imprimir el ticket de entrega: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
        }
    }
}

