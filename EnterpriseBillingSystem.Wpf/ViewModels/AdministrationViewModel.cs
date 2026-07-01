using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnterpriseBillingSystem.Wpf.Models;
using EnterpriseBillingSystem.Wpf.Services.Api;
using EnterpriseBillingSystem.Wpf.Services.Dialogs;

namespace EnterpriseBillingSystem.Wpf.ViewModels;

public partial class AdministrationViewModel : ViewModelBase
{
    private readonly AdministrationApiClient _adminApiClient;
    private readonly UserApiClient _userApiClient;
    private readonly INotificationService _notificationService;

    public string Title => "Configuración y Administración General";

    [ObservableProperty]
    private bool _isLoading;

    // ─── THRESHOLDS (UMBRALES) ────────────────────────────────────────────────
    [ObservableProperty]
    private decimal _semiWholesaleThreshold = 10000;

    [ObservableProperty]
    private decimal _wholesaleThreshold = 30000;

    [ObservableProperty]
    private decimal _minimumInvoiceAmount = 350;

    private List<PricingThresholdDto> _rawThresholds = new();

    // ─── CURRENCIES (DIVISAS) ─────────────────────────────────────────────────
    public ObservableCollection<CurrencyDto> Currencies { get; } = new();

    [ObservableProperty]
    private CurrencyDto? _selectedCurrency;

    [ObservableProperty]
    private string _newCurrencyCode = string.Empty;

    [ObservableProperty]
    private string _newCurrencyName = string.Empty;

    [ObservableProperty]
    private string _newCurrencySymbol = string.Empty;

    [ObservableProperty]
    private decimal _newCurrencyExchangeRate = 1.0m;

    [ObservableProperty]
    private bool _newCurrencyIsDefault;

    [ObservableProperty]
    private bool _newCurrencyIsActive = true;

    [ObservableProperty]
    private bool _isEditingCurrency;

    // ─── SALES GOALS (METAS) ──────────────────────────────────────────────────
    public ObservableCollection<SalesGoalDto> SalesGoals { get; } = new();
    public ObservableCollection<UserDto> Salespeople { get; } = new();

    [ObservableProperty]
    private SalesGoalDto? _selectedGoal;

    [ObservableProperty]
    private UserDto? _selectedSalesperson;

    [ObservableProperty]
    private decimal _newGoalTargetAmount = 10000;

    [ObservableProperty]
    private string _newGoalPeriodName = DateTime.Now.ToString("MMMM yyyy");

    [ObservableProperty]
    private DateTime _newGoalStartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

    [ObservableProperty]
    private DateTime _newGoalEndDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1).AddDays(-1);

    [ObservableProperty]
    private bool _newGoalIsActive = true;

    [ObservableProperty]
    private bool _isEditingGoal;

    // ─── CONSTRUCTOR ──────────────────────────────────────────────────────────
    public AdministrationViewModel(
        AdministrationApiClient adminApiClient,
        UserApiClient userApiClient,
        INotificationService notificationService)
    {
        _adminApiClient = adminApiClient;
        _userApiClient = userApiClient;
        _notificationService = notificationService;
    }

    public async Task InitializeAsync()
    {
        await LoadAllAsync();
    }

    [RelayCommand]
    public async Task LoadAllAsync()
    {
        IsLoading = true;
        try
        {
            await Task.WhenAll(
                LoadThresholdsAsync(),
                LoadCurrenciesAsync(),
                LoadGoalsAsync(),
                LoadSalespeopleAsync()
            );
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al inicializar configuraciones: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ─── THRESHOLDS LOGIC ─────────────────────────────────────────────────────
    private async Task LoadThresholdsAsync()
    {
        try
        {
            _rawThresholds = await _adminApiClient.GetPricingThresholdsAsync();
            var semi = _rawThresholds.FirstOrDefault(t => t.LevelName.Equals("SEMI MAYORISTA", StringComparison.OrdinalIgnoreCase));
            var wholesale = _rawThresholds.FirstOrDefault(t => t.LevelName.Equals("MAYORISTA", StringComparison.OrdinalIgnoreCase));

            if (semi != null) SemiWholesaleThreshold = semi.MinimumSubtotal;
            if (wholesale != null) WholesaleThreshold = wholesale.MinimumSubtotal;

            var minAmtStr = await _adminApiClient.GetSystemParameterAsync("MinimumInvoiceAmount");
            if (!string.IsNullOrWhiteSpace(minAmtStr) && decimal.TryParse(minAmtStr, out var minAmt))
            {
                MinimumInvoiceAmount = minAmt;
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al cargar umbrales y parámetros: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task SaveThresholdsAsync()
    {
        IsLoading = true;
        try
        {
            var semi = _rawThresholds.FirstOrDefault(t => t.LevelName.Equals("SEMI MAYORISTA", StringComparison.OrdinalIgnoreCase));
            var wholesale = _rawThresholds.FirstOrDefault(t => t.LevelName.Equals("MAYORISTA", StringComparison.OrdinalIgnoreCase));

            if (semi == null || wholesale == null)
            {
                _notificationService.ShowError("No se encontraron registros de umbrales para actualizar. Verifique la base de datos.");
                return;
            }

            var command = new UpdatePricingThresholdsCommandDto(new List<ThresholdUpdateInputDto>
            {
                new(semi.Id, SemiWholesaleThreshold, true),
                new(wholesale.Id, WholesaleThreshold, true)
            });

            var success = await _adminApiClient.UpdatePricingThresholdsAsync(command);
            var successParam = await _adminApiClient.UpdateSystemParameterAsync("MinimumInvoiceAmount", MinimumInvoiceAmount.ToString("F2"));
            if (success && successParam)
            {
                _notificationService.ShowSuccess("Configuraciones y umbrales guardados correctamente.");
                await LoadThresholdsAsync();
            }
            else
            {
                _notificationService.ShowError("Error al actualizar la configuración general.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al guardar umbrales: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ─── CURRENCIES LOGIC ─────────────────────────────────────────────────────
    private async Task LoadCurrenciesAsync()
    {
        try
        {
            var list = await _adminApiClient.GetCurrenciesAsync();
            Currencies.Clear();
            foreach (var c in list)
            {
                Currencies.Add(c);
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al cargar divisas: {ex.Message}");
        }
    }

    [RelayCommand]
    private void StartNewCurrency()
    {
        SelectedCurrency = null;
        NewCurrencyCode = string.Empty;
        NewCurrencyName = string.Empty;
        NewCurrencySymbol = string.Empty;
        NewCurrencyExchangeRate = 1.0m;
        NewCurrencyIsDefault = false;
        NewCurrencyIsActive = true;
        IsEditingCurrency = true;
    }

    [RelayCommand]
    private void SelectCurrencyForEdit(CurrencyDto? currency)
    {
        if (currency == null) return;

        SelectedCurrency = currency;
        NewCurrencyCode = currency.Code;
        NewCurrencyName = currency.Name;
        NewCurrencySymbol = currency.Symbol;
        NewCurrencyExchangeRate = currency.ExchangeRate;
        NewCurrencyIsDefault = currency.IsDefault;
        NewCurrencyIsActive = currency.IsActive;
        IsEditingCurrency = true;
    }

    [RelayCommand]
    private void CancelCurrencyEdit()
    {
        IsEditingCurrency = false;
        SelectedCurrency = null;
    }

    [RelayCommand]
    public async Task SaveCurrencyAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCurrencyCode) || string.IsNullOrWhiteSpace(NewCurrencyName) || string.IsNullOrWhiteSpace(NewCurrencySymbol))
        {
            _notificationService.ShowError("Por favor complete todos los campos obligatorios para la divisa.");
            return;
        }

        IsLoading = true;
        try
        {
            if (SelectedCurrency == null)
            {
                // Create
                var cmd = new CreateCurrencyCommandDto(
                    NewCurrencyCode,
                    NewCurrencyName,
                    NewCurrencySymbol,
                    NewCurrencyExchangeRate,
                    NewCurrencyIsDefault,
                    NewCurrencyIsActive
                );
                await _adminApiClient.CreateCurrencyAsync(cmd);
                _notificationService.ShowSuccess("Moneda agregada correctamente.");
            }
            else
            {
                // Update
                var cmd = new UpdateCurrencyCommandDto(
                    SelectedCurrency.Id,
                    NewCurrencyCode,
                    NewCurrencyName,
                    NewCurrencySymbol,
                    NewCurrencyExchangeRate,
                    NewCurrencyIsDefault,
                    NewCurrencyIsActive
                );
                var success = await _adminApiClient.UpdateCurrencyAsync(SelectedCurrency.Id, cmd);
                if (success) _notificationService.ShowSuccess("Moneda actualizada correctamente.");
                else _notificationService.ShowError("Error al actualizar la moneda.");
            }

            IsEditingCurrency = false;
            SelectedCurrency = null;
            await LoadCurrenciesAsync();
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al guardar moneda: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task DeleteCurrencyAsync(CurrencyDto? currency)
    {
        if (currency == null) return;
        if (currency.IsDefault)
        {
            _notificationService.ShowError("No se puede eliminar la moneda predeterminada del sistema.");
            return;
        }

        IsLoading = true;
        try
        {
            var success = await _adminApiClient.DeleteCurrencyAsync(currency.Id);
            if (success)
            {
                _notificationService.ShowSuccess("Moneda eliminada correctamente.");
                await LoadCurrenciesAsync();
            }
            else
            {
                _notificationService.ShowError("Error al eliminar la moneda.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al eliminar moneda: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ─── SALES GOALS LOGIC ────────────────────────────────────────────────────
    private async Task LoadGoalsAsync()
    {
        try
        {
            var list = await _adminApiClient.GetSalesGoalsAsync();
            SalesGoals.Clear();
            foreach (var g in list)
            {
                SalesGoals.Add(g);
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al cargar metas: {ex.Message}");
        }
    }

    private async Task LoadSalespeopleAsync()
    {
        try
        {
            var result = await _userApiClient.GetUsersPagedAsync(1, 100);
            Salespeople.Clear();
            if (result?.Items != null)
            {
                foreach (var u in result.Items)
                {
                    Salespeople.Add(u);
                }
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al cargar vendedores: {ex.Message}");
        }
    }

    [RelayCommand]
    private void StartNewGoal()
    {
        SelectedGoal = null;
        SelectedSalesperson = null;
        NewGoalTargetAmount = 10000;
        NewGoalPeriodName = DateTime.Now.ToString("MMMM yyyy");
        NewGoalStartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        NewGoalEndDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1).AddDays(-1);
        NewGoalIsActive = true;
        IsEditingGoal = true;
    }

    [RelayCommand]
    private void SelectGoalForEdit(SalesGoalDto? goal)
    {
        if (goal == null) return;

        SelectedGoal = goal;
        SelectedSalesperson = Salespeople.FirstOrDefault(s => s.Id == goal.UserId);
        NewGoalTargetAmount = goal.TargetAmount;
        NewGoalPeriodName = goal.PeriodName;
        NewGoalStartDate = goal.StartDate;
        NewGoalEndDate = goal.EndDate;
        NewGoalIsActive = goal.IsActive;
        IsEditingGoal = true;
    }

    [RelayCommand]
    private void CancelGoalEdit()
    {
        IsEditingGoal = false;
        SelectedGoal = null;
    }

    [RelayCommand]
    public async Task SaveGoalAsync()
    {
        if (SelectedSalesperson == null || string.IsNullOrWhiteSpace(NewGoalPeriodName))
        {
            _notificationService.ShowError("Debe seleccionar un trabajador y especificar un periodo.");
            return;
        }

        IsLoading = true;
        try
        {
            if (SelectedGoal == null)
            {
                // Create
                var cmd = new CreateSalesGoalCommandDto(
                    SelectedSalesperson.Id,
                    NewGoalPeriodName,
                    NewGoalTargetAmount,
                    NewGoalStartDate,
                    NewGoalEndDate,
                    NewGoalIsActive
                );
                await _adminApiClient.CreateSalesGoalAsync(cmd);
                _notificationService.ShowSuccess("Meta asignada correctamente.");
            }
            else
            {
                // Update
                var cmd = new UpdateSalesGoalCommandDto(
                    SelectedGoal.Id,
                    SelectedSalesperson.Id,
                    NewGoalPeriodName,
                    NewGoalTargetAmount,
                    NewGoalStartDate,
                    NewGoalEndDate,
                    NewGoalIsActive
                );
                var success = await _adminApiClient.UpdateSalesGoalAsync(SelectedGoal.Id, cmd);
                if (success) _notificationService.ShowSuccess("Meta actualizada correctamente.");
                else _notificationService.ShowError("Error al actualizar la meta.");
            }

            IsEditingGoal = false;
            SelectedGoal = null;
            await LoadGoalsAsync();
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al guardar la meta: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task DeleteGoalAsync(SalesGoalDto? goal)
    {
        if (goal == null) return;

        IsLoading = true;
        try
        {
            var success = await _adminApiClient.DeleteSalesGoalAsync(goal.Id);
            if (success)
            {
                _notificationService.ShowSuccess("Meta eliminada correctamente.");
                await LoadGoalsAsync();
            }
            else
            {
                _notificationService.ShowError("Error al eliminar la meta.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al eliminar la meta: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
