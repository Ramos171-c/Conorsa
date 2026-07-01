using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnterpriseBillingSystem.Wpf.Models;
using EnterpriseBillingSystem.Wpf.Services.Api;
using EnterpriseBillingSystem.Wpf.Services.Dialogs;

namespace EnterpriseBillingSystem.Wpf.ViewModels;

public partial class UsersViewModel : ViewModelBase
{
    private readonly UserApiClient _userApiClient;
    private readonly INotificationService _notificationService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private string? _searchTerm;

    [ObservableProperty]
    private int _pageNumber = 1;

    [ObservableProperty]
    private int _pageSize = 10;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<UserDto> Users { get; } = new();

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber * PageSize < TotalCount;

    public UsersViewModel(
        UserApiClient userApiClient,
        INotificationService notificationService,
        IServiceProvider serviceProvider)
    {
        _userApiClient = userApiClient;
        _notificationService = notificationService;
        _serviceProvider = serviceProvider;
    }

    public async Task InitializeAsync()
    {
        await LoadUsersAsync();
    }

    [RelayCommand]
    public async Task LoadUsersAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _userApiClient.GetUsersPagedAsync(PageNumber, PageSize, SearchTerm);
            Users.Clear();
            if (result?.Items != null)
            {
                foreach (var user in result.Items)
                {
                    Users.Add(user);
                }
                TotalCount = result.TotalCount;
            }
            else
            {
                TotalCount = 0;
            }

            OnPropertyChanged(nameof(HasPreviousPage));
            OnPropertyChanged(nameof(HasNextPage));
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al cargar trabajadores: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (HasNextPage)
        {
            PageNumber++;
            await LoadUsersAsync();
        }
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (HasPreviousPage)
        {
            PageNumber--;
            await LoadUsersAsync();
        }
    }

    [RelayCommand]
    private async Task CreateUserAsync()
    {
        var editorViewModel = new UserEditorViewModel(_userApiClient, _notificationService);
        await editorViewModel.InitializeAsync();

        var editorWindow = new Views.Users.UserEditorDialog
        {
            DataContext = editorViewModel,
            Owner = System.Windows.Application.Current.MainWindow
        };

        editorViewModel.RequestClose += () => editorWindow.Close();
        editorWindow.ShowDialog();

        await LoadUsersAsync();
    }

    [RelayCommand]
    private async Task EditUserAsync(UserDto user)
    {
        if (user == null) return;

        var editorViewModel = new UserEditorViewModel(_userApiClient, _notificationService, user);
        await editorViewModel.InitializeAsync();

        var editorWindow = new Views.Users.UserEditorDialog
        {
            DataContext = editorViewModel,
            Owner = System.Windows.Application.Current.MainWindow
        };

        editorViewModel.RequestClose += () => editorWindow.Close();
        editorWindow.ShowDialog();

        await LoadUsersAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        PageNumber = 1;
        await LoadUsersAsync();
    }

    partial void OnSearchTermChanged(string? value)
    {
        _ = LoadUsersAsync();
    }
}
