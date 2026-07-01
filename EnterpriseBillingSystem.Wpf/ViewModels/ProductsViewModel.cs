using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnterpriseBillingSystem.Wpf.Models;
using EnterpriseBillingSystem.Wpf.Services.Api;
using EnterpriseBillingSystem.Wpf.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseBillingSystem.Wpf.ViewModels;

public partial class ProductsViewModel : ViewModelBase
{
    private readonly ProductApiClient _productApiClient;
    private readonly INotificationService _notificationService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private string? _searchTerm;

    [ObservableProperty]
    private Guid? _selectedCategoryId;

    [ObservableProperty]
    private int _pageNumber = 1;

    [ObservableProperty]
    private int _pageSize = 10;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<ProductDto> Products { get; } = new();
    public ObservableCollection<CategoryDto> Categories { get; } = new();

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber * PageSize < TotalCount;

    public ProductsViewModel(
        ProductApiClient productApiClient,
        INotificationService notificationService,
        IServiceProvider serviceProvider)
    {
        _productApiClient = productApiClient;
        _notificationService = notificationService;
        _serviceProvider = serviceProvider;
    }

    public async Task InitializeAsync()
    {
        await LoadCategoriesAsync();
        await LoadProductsAsync();
    }

    [RelayCommand]
    private async Task LoadCategoriesAsync()
    {
        try
        {
            var result = await _productApiClient.GetCategoriesAsync(1, 100);
            Categories.Clear();
            Categories.Add(new CategoryDto(Guid.Empty, "-- Todas las Categorías --", null, null, true));
            if (result?.Items != null)
            {
                foreach (var cat in result.Items)
                {
                    Categories.Add(cat);
                }
            }
        }
        catch
        {
            // Fail silently
        }
    }

    [RelayCommand]
    public async Task LoadProductsAsync()
    {
        IsLoading = true;
        try
        {
            var categoryId = SelectedCategoryId == Guid.Empty ? null : SelectedCategoryId;
            var result = await _productApiClient.GetProductsPagedAsync(PageNumber, PageSize, SearchTerm, categoryId);
            
            Products.Clear();
            if (result?.Items != null)
            {
                foreach (var prod in result.Items)
                {
                    Products.Add(prod);
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
            _notificationService.ShowError($"Error al cargar productos: {ex.Message}");
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
            await LoadProductsAsync();
        }
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (HasPreviousPage)
        {
            PageNumber--;
            await LoadProductsAsync();
        }
    }

    [RelayCommand]
    private async Task CreateProductAsync()
    {
        var editorViewModel = new ProductEditorViewModel(_productApiClient, _notificationService);
        await editorViewModel.InitializeAsync();

        // Resolve editor window
        var editorWindow = new Views.Inventory.ProductEditorDialog
        {
            DataContext = editorViewModel,
            Owner = System.Windows.Application.Current.MainWindow
        };

        editorViewModel.RequestClose += () => editorWindow.Close();
        editorWindow.ShowDialog();

        // Reload
        await LoadProductsAsync();
    }

    [RelayCommand]
    private async Task EditProductAsync(ProductDto product)
    {
        if (product == null) return;

        var editorViewModel = new ProductEditorViewModel(_productApiClient, _notificationService, product);
        await editorViewModel.InitializeAsync();

        var editorWindow = new Views.Inventory.ProductEditorDialog
        {
            DataContext = editorViewModel,
            Owner = System.Windows.Application.Current.MainWindow
        };

        editorViewModel.RequestClose += () => editorWindow.Close();
        editorWindow.ShowDialog();

        // Reload
        await LoadProductsAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        PageNumber = 1;
        await LoadProductsAsync();
    }

    partial void OnSearchTermChanged(string? value)
    {
        // Simple instant search or debounce
        _ = LoadProductsAsync();
    }

    partial void OnSelectedCategoryIdChanged(Guid? value)
    {
        _ = LoadProductsAsync();
    }
}
