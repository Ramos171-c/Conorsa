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
            item.PropertyChanged += (s, e) => RecalculateTotalsAndNotes();
            Details.Add(item);
        }
        RecalculateTotalsAndNotes();
    }

    [ObservableProperty]
    private bool _isOrderEdited;

    public bool CanEditOrder => !Status.Equals("Anulado", StringComparison.OrdinalIgnoreCase) && 
                                !Status.Equals("Completado", StringComparison.OrdinalIgnoreCase);

    public decimal EffectiveTotalAmount => Details.Sum(d => d.EffectiveNetAmount);
    public decimal TotalMissingQuantity => Details.Sum(d => d.MissingQuantity);
    public string TotalMissingQuantityDisplay => $"{TotalMissingQuantity:N2} pzas";

    public void RecalculateTotalsAndNotes()
    {
        SubTotal = Details.Sum(d => d.Quantity * d.UnitPrice);
        DiscountAmount = Details.Sum(d => d.DiscountAmount);
        TaxAmount = Details.Sum(d => d.TaxAmount);
        TotalAmount = SubTotal - DiscountAmount + TaxAmount;

        OnPropertyChanged(nameof(EffectiveTotalAmount));
        OnPropertyChanged(nameof(TotalMissingQuantity));
        OnPropertyChanged(nameof(TotalMissingQuantityDisplay));

        var missingItems = Details.Where(d => d.MissingQuantity > 0).ToList();
        if (missingItems.Any())
        {
            var summary = string.Join("; ", missingItems.Select(m => $"{m.ProductName}: faltan {m.MissingQuantity:N2} pzas{(string.IsNullOrWhiteSpace(m.MissingReason) ? "" : $" [{m.MissingReason}]")}"));
            DispatcherNotes = $"[PRODUCTOS FALTANTES REGISTRADOS]: {summary}";
        }
    }

    [RelayCommand]
    private void RemoveDetailItem(object? parameter)
    {
        if (parameter is not SalesOrderDetailItemDto item) return;

        if (Details.Count <= 1)
        {
            Views.Dialogs.CustomMessageBox.Show(
                "No se puede eliminar el único producto del pedido. Si desea cancelar todo el pedido, utilice la opción 'Anular Pedido'.",
                "Operación no permitida",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var confirm = Views.Dialogs.CustomMessageBox.Show(
            $"¿Está seguro de que desea eliminar el producto '{item.ProductName}' del pedido?",
            "Eliminar Producto del Pedido",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        Details.Remove(item);
        IsOrderEdited = true;
        RecalculateTotalsAndNotes();

        _notificationService.ShowWarning($"Producto '{item.ProductName}' retirado del pedido. Presione 'Guardar Cambios' para aplicar en el servidor.");
    }

    [RelayCommand]
    private async Task SaveOrderChangesAsync()
    {
        var billableDetails = Details.Where(d => d.DeliveredQuantity > 0).ToList();

        if (!billableDetails.Any())
        {
            _notificationService.ShowError("El pedido debe contener al menos un producto a entregar.");
            return;
        }

        var missingItems = Details.Where(d => d.MissingQuantity > 0 || d.DeliveredQuantity < d.Quantity).ToList();

        string confirmMsg = $"¿Desea guardar las modificaciones en el pedido {OrderNumber}?";
        if (missingItems.Any())
        {
            confirmMsg = $"Se han registrado faltantes ({missingItems.Count} producto(s)). El pedido se ajustará a las cantidades reales entregadas y se facturarán ÚNICAMENTE las unidades entregadas. ¿Desea continuar?";
        }

        var confirm = Views.Dialogs.CustomMessageBox.Show(
            confirmMsg,
            "Guardar Cambios y Ajustar Facturación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        IsProcessing = true;
        try
        {
            string updatedNotes = Notes ?? string.Empty;
            if (missingItems.Any())
            {
                var missingSummary = string.Join("; ", missingItems.Select(m => 
                    $"{m.ProductName}: Faltaron {m.MissingQuantity:N2} {m.UnitOfMeasure} [Motivo: {(string.IsNullOrWhiteSpace(m.MissingReason) ? "No vino en pedido general" : m.MissingReason)}]"));
                
                if (!updatedNotes.Contains("[PRODUCTOS NO ENTREGADOS / NO VINIERON]:"))
                {
                    updatedNotes = (string.IsNullOrWhiteSpace(updatedNotes) ? "" : updatedNotes + "\n") + 
                                   $"[PRODUCTOS NO ENTREGADOS / NO VINIERON]: {missingSummary}";
                }
            }

            var reqDetails = billableDetails.Select(d => new SalesOrderDetailRequestDto(
                ProductId: d.ProductId,
                UnitOfMeasureId: d.UnitOfMeasureId,
                Quantity: d.DeliveredQuantity, // Facturar SOLO la cantidad entregada real
                UnitPrice: d.UnitPrice,
                DiscountPercentage: d.DiscountPercentage,
                TaxPercentage: d.TaxPercentage
            )).ToList();

            var command = new UpdateSalesOrderCommandDto(
                Id: _order.Id,
                CustomerId: _order.CustomerId,
                OrderDate: OrderDate,
                Notes: updatedNotes,
                Details: reqDetails
            );

            var success = await _salesApiClient.UpdateSalesOrderAsync(_order.Id, command);
            if (success)
            {
                IsOrderEdited = false;
                Notes = updatedNotes;
                _notificationService.ShowSuccess($"Pedido {OrderNumber} ajustado exitosamente. Se facturará únicamente lo entregado.");
                OrderActionTaken?.Invoke();
            }
            else
            {
                _notificationService.ShowError("Error al guardar los cambios del pedido.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al actualizar pedido: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmOrderAsync()
    {
        if (!IsActionEnabled) return;

        var missingItems = Details.Where(d => d.MissingQuantity > 0 || d.DeliveredQuantity < d.Quantity).ToList();

        if (IsOrderEdited || missingItems.Any())
        {
            await SaveOrderChangesAsync();
            if (IsOrderEdited) return;
        }

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

    public bool CanCancelOrder => !Status.Equals("Anulado", StringComparison.OrdinalIgnoreCase) && 
                                  !Status.Equals("Completado", StringComparison.OrdinalIgnoreCase);

    [RelayCommand]
    private async Task CancelOrderAsync()
    {
        if (!CanCancelOrder) return;

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

        var missingItems = Details.Where(d => d.MissingQuantity > 0 || d.DeliveredQuantity < d.Quantity).ToList();
        bool statusChanged = !SelectedNewStatus.Equals(Status, StringComparison.OrdinalIgnoreCase);

        // Si el estado es el mismo Y no hay modificaciones en productos/faltantes, avisar al usuario
        if (!statusChanged && !IsOrderEdited && !missingItems.Any())
        {
            Views.Dialogs.CustomMessageBox.Show(
                "El pedido ya se encuentra en este estado y no tiene modificaciones pendientes.", 
                "Información", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
            return;
        }

        // Si se han modificado cantidades o hay faltantes, guardar primero las modificaciones para ajustar la factura
        if (IsOrderEdited || missingItems.Any())
        {
            await SaveOrderChangesAsync();
            if (IsOrderEdited) return; // Si fue cancelado o falló, no continuar
        }

        // Si además el estado cambió, actualizarlo en el servidor
        if (statusChanged)
        {
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
                PagePadding = new Thickness(15, 10, 15, 10),
                ColumnWidth = double.PositiveInfinity,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI, Arial"),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.Black,
                TextAlignment = TextAlignment.Left
            };

            var sec = new System.Windows.Documents.Section();

            // Logo
            try
            {
                System.Windows.Media.Imaging.BitmapImage? logoBitmap = null;
                try
                {
                    logoBitmap = new System.Windows.Media.Imaging.BitmapImage();
                    logoBitmap.BeginInit();
                    logoBitmap.UriSource = new Uri("pack://application:,,,/EnterpriseBillingSystem.Wpf;component/Assets/logo.png", UriKind.RelativeOrAbsolute);
                    logoBitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    logoBitmap.EndInit();
                }
                catch
                {
                    var logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png");
                    if (System.IO.File.Exists(logoPath))
                    {
                        logoBitmap = new System.Windows.Media.Imaging.BitmapImage();
                        logoBitmap.BeginInit();
                        logoBitmap.UriSource = new Uri(logoPath, UriKind.Absolute);
                        logoBitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        logoBitmap.EndInit();
                    }
                }

                if (logoBitmap != null)
                {
                    var img = new System.Windows.Controls.Image
                    {
                        Source = logoBitmap,
                        Width = 70,
                        Height = 70,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    var imgContainer = new System.Windows.Documents.BlockUIContainer(img)
                    {
                        Margin = new Thickness(0, 0, 0, 4),
                        TextAlignment = TextAlignment.Center
                    };
                    sec.Blocks.Add(imgContainer);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error rendering ticket logo: {ex.Message}");
            }

            // Header - Dulce y caramelos + Dirección + Teléfono
            var headerPara = new System.Windows.Documents.Paragraph
            {
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.Black,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            };
            headerPara.Inlines.Add(new System.Windows.Documents.Run("Dulce y caramelos\n") { FontSize = 18, FontWeight = FontWeights.Bold });
            headerPara.Inlines.Add(new System.Windows.Documents.Run("Dirección: Matagalpa\n") { FontSize = 11, FontWeight = FontWeights.Bold });
            headerPara.Inlines.Add(new System.Windows.Documents.Run("Teléfono:  86953060\n") { FontSize = 11, FontWeight = FontWeights.Bold });
            headerPara.Inlines.Add(new System.Windows.Documents.Run("TICKET DE ENTREGA\n") { FontSize = 13, FontWeight = FontWeights.Bold });
            headerPara.Inlines.Add(new System.Windows.Documents.Run("==================================\n") { FontWeight = FontWeights.Bold });
            sec.Blocks.Add(headerPara);

            // Customer Details
            var custPara = new System.Windows.Documents.Paragraph
            {
                Foreground = System.Windows.Media.Brushes.Black,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            custPara.Inlines.Add(new System.Windows.Documents.Run($"Pedido No:   {OrderNumber}\n"));
            custPara.Inlines.Add(new System.Windows.Documents.Run($"Fecha:       {OrderDate:dd/MM/yyyy HH:mm}\n"));
            custPara.Inlines.Add(new System.Windows.Documents.Run($"Cliente:     {CustomerName} ({CustomerCode})\n"));
            
            if (customer != null)
            {
                custPara.Inlines.Add(new System.Windows.Documents.Run($"Ruta:        {customer.RouteName ?? "No asignada"}\n"));
                
                var address = customer.Addresses?.FirstOrDefault(a => a.IsDefault) ?? customer.Addresses?.FirstOrDefault();
                if (address != null)
                {
                    custPara.Inlines.Add(new System.Windows.Documents.Run($"Dirección:   {address.AddressLine1}, {address.City}\n"));
                }
                
                var phone = customer.Phones?.FirstOrDefault()?.PhoneNumber;
                if (!string.IsNullOrEmpty(phone))
                {
                    custPara.Inlines.Add(new System.Windows.Documents.Run($"Teléfono:    {phone}\n"));
                }
            }
            custPara.Inlines.Add(new System.Windows.Documents.Run("==================================\n") { FontWeight = FontWeights.Bold });
            sec.Blocks.Add(custPara);

            // Order Lines
            var itemsPara = new System.Windows.Documents.Paragraph
            {
                Foreground = System.Windows.Media.Brushes.Black,
                Margin = new Thickness(0, 0, 0, 4)
            };
            itemsPara.Inlines.Add(new System.Windows.Documents.Bold(new System.Windows.Documents.Run("DETALLE DEL PEDIDO\n")));
            itemsPara.Inlines.Add(new System.Windows.Documents.Run("----------------------------------\n") { FontWeight = FontWeights.Bold });
            
            decimal delSubtotal = 0;
            decimal delDiscount = 0;
            decimal delTax = 0;

            var billableDetails = Details.Where(d => d.DeliveredQuantity > 0).ToList();
            int itemIndex = 0;

            foreach (var item in billableDetails)
            {
                decimal baseAmount = item.DeliveredQuantity * item.UnitPrice;
                decimal disc = baseAmount * (item.DiscountPercentage / 100m);
                decimal tax = (baseAmount - disc) * (item.TaxPercentage / 100m);

                delSubtotal += baseAmount;
                delDiscount += disc;
                delTax += tax;

                string codePrefix = !string.IsNullOrWhiteSpace(item.ProductCode) ? $"[{item.ProductCode}] " : "";
                string displayName = !string.IsNullOrWhiteSpace(item.ProductDescription) ? item.ProductDescription : item.ProductName;

                // Line 1: Product Code + Description (with U/E)
                itemsPara.Inlines.Add(new System.Windows.Documents.Bold(new System.Windows.Documents.Run($"{codePrefix}{displayName}\n")));
                
                // Line 2: Cantidad x Precio Unitario = Total
                if (item.MissingQuantity > 0)
                {
                    itemsPara.Inlines.Add(new System.Windows.Documents.Run($"   Pedido:    {item.Quantity:N2} {item.UnitOfMeasure}\n"));
                    itemsPara.Inlines.Add(new System.Windows.Documents.Bold(new System.Windows.Documents.Run($"   Faltante:  {item.MissingQuantity:N2} {item.UnitOfMeasure} [{(string.IsNullOrWhiteSpace(item.MissingReason) ? "No vino" : item.MissingReason)}]\n")));
                    itemsPara.Inlines.Add(new System.Windows.Documents.Bold(new System.Windows.Documents.Run($"   Entregado: {item.DeliveredQuantity:N2} {item.UnitOfMeasure} x C${item.UnitPrice:N2} = C${item.EffectiveNetAmount:N2}\n")));
                }
                else
                {
                    itemsPara.Inlines.Add(new System.Windows.Documents.Bold(new System.Windows.Documents.Run($"   {item.DeliveredQuantity:N2} {item.UnitOfMeasure} x C${item.UnitPrice:N2} = C${item.EffectiveNetAmount:N2}\n")));
                }

                // Divider line between items
                itemIndex++;
                if (itemIndex < billableDetails.Count)
                {
                    itemsPara.Inlines.Add(new System.Windows.Documents.Run("----------------------------------\n"));
                }
            }
            itemsPara.Inlines.Add(new System.Windows.Documents.Run("==================================\n") { FontWeight = FontWeights.Bold });
            sec.Blocks.Add(itemsPara);

            // Totals
            var totalsPara = new System.Windows.Documents.Paragraph
            {
                Foreground = System.Windows.Media.Brushes.Black,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 0, 0, 4)
            };

            decimal delTotal = delSubtotal - delDiscount + delTax;
            var missingItems = Details.Where(d => d.MissingQuantity > 0).ToList();

            totalsPara.Inlines.Add(new System.Windows.Documents.Run($"Subtotal:     C${delSubtotal:N2}\n"));
            if (delDiscount > 0)
            {
                totalsPara.Inlines.Add(new System.Windows.Documents.Run($"Descuento:   -C${delDiscount:N2}\n"));
            }
            if (delTax > 0)
            {
                totalsPara.Inlines.Add(new System.Windows.Documents.Run($"IVA:          C${delTax:N2}\n"));
            }
            if (missingItems.Any())
            {
                totalsPara.Inlines.Add(new System.Windows.Documents.Bold(new System.Windows.Documents.Run($"Faltantes:    {missingItems.Sum(m => m.MissingQuantity):N2} pzas\n")));
            }
            totalsPara.Inlines.Add(new System.Windows.Documents.Bold(new System.Windows.Documents.Run($"TOTAL NETO:   C${delTotal:N2}\n")));
            
            decimal totalUsd = delTotal / 36.5m;
            totalsPara.Inlines.Add(new System.Windows.Documents.Bold(new System.Windows.Documents.Run($"TOTAL USD:     ${totalUsd:N2}\n")));
            totalsPara.Inlines.Add(new System.Windows.Documents.Run("==================================\n") { FontWeight = FontWeights.Bold });
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
                    // Strip the [Faltantes] block from printed ticket observations
                    int faltantesIndex = cleanNotes.IndexOf("[Faltantes]:", StringComparison.OrdinalIgnoreCase);
                    if (faltantesIndex >= 0)
                    {
                        notesText = cleanNotes.Substring(0, faltantesIndex).Trim();
                    }
                    else
                    {
                        notesText = cleanNotes;
                    }
                    
                    showNotes = !string.IsNullOrWhiteSpace(notesText);
                }
            }

            if (showNotes)
            {
                var obsPara = new System.Windows.Documents.Paragraph
                {
                    Foreground = System.Windows.Media.Brushes.Black,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 4)
                };
                obsPara.Inlines.Add(new System.Windows.Documents.Bold(new System.Windows.Documents.Run("OBSERVACIONES:\n")));
                obsPara.Inlines.Add(new System.Windows.Documents.Run($"- Vendedor:  {notesText}\n"));
                obsPara.Inlines.Add(new System.Windows.Documents.Run("==================================\n") { FontWeight = FontWeights.Bold });
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

