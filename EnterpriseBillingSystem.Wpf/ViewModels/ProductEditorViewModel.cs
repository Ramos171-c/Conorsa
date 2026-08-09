using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnterpriseBillingSystem.Wpf.Models;
using EnterpriseBillingSystem.Wpf.Services.Api;
using EnterpriseBillingSystem.Wpf.Services.Dialogs;

namespace EnterpriseBillingSystem.Wpf.ViewModels;

public class ProductPresentationViewModel : ObservableObject
{
    public Guid? Id { get; set; }
    public decimal OriginalRetailPrice { get; set; }
    public decimal OriginalSemiWholesalePrice { get; set; }
    public decimal OriginalWholesalePrice { get; set; }
    public decimal OriginalCost { get; set; }

    private Guid _unitOfMeasureId;
    public Guid UnitOfMeasureId
    {
        get => _unitOfMeasureId;
        set => SetProperty(ref _unitOfMeasureId, value);
    }

    private string _unitOfMeasureCode = string.Empty;
    public string UnitOfMeasureCode
    {
        get => _unitOfMeasureCode;
        set => SetProperty(ref _unitOfMeasureCode, value);
    }

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private decimal _conversionFactor = 1;
    public decimal ConversionFactor
    {
        get => _conversionFactor;
        set => SetProperty(ref _conversionFactor, value);
    }

    private string? _barcode;
    public string? Barcode
    {
        get => _barcode;
        set => SetProperty(ref _barcode, value);
    }

    private decimal _cost;
    public decimal Cost
    {
        get => _cost;
        set => SetProperty(ref _cost, value);
    }

    private decimal _retailPrice;
    public decimal RetailPrice
    {
        get => _retailPrice;
        set => SetProperty(ref _retailPrice, value);
    }

    private decimal _semiWholesalePrice;
    public decimal SemiWholesalePrice
    {
        get => _semiWholesalePrice;
        set => SetProperty(ref _semiWholesalePrice, value);
    }

    private decimal _wholesalePrice;
    public decimal WholesalePrice
    {
        get => _wholesalePrice;
        set => SetProperty(ref _wholesalePrice, value);
    }

    private bool _isBaseUnit;
    public bool IsBaseUnit
    {
        get => _isBaseUnit;
        set => SetProperty(ref _isBaseUnit, value);
    }

    private bool _isDefaultSalePresentation;
    public bool IsDefaultSalePresentation
    {
        get => _isDefaultSalePresentation;
        set => SetProperty(ref _isDefaultSalePresentation, value);
    }

    private bool _allowPurchase = true;
    public bool AllowPurchase
    {
        get => _allowPurchase;
        set => SetProperty(ref _allowPurchase, value);
    }

    private bool _allowSale = true;
    public bool AllowSale
    {
        get => _allowSale;
        set => SetProperty(ref _allowSale, value);
    }

    private bool _isActive = true;
    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }
}

public partial class ProductEditorViewModel : ViewModelBase
{
    private readonly ProductApiClient _productApiClient;
    private readonly INotificationService _notificationService;
    private readonly ProductDto? _originalProduct;

    [ObservableProperty]
    private string _title = "Nuevo Producto";

    [ObservableProperty]
    private string _internalCode = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private int _productType = 1; // 1 = Physical, 2 = Service, 3 = Digital

    [ObservableProperty]
    private int _productStatus = 2; // 1 = Draft, 2 = Active, 3 = Discontinued

    [ObservableProperty]
    private bool _trackInventory = true;

    [ObservableProperty]
    private bool _requiresSerialNumber;

    [ObservableProperty]
    private bool _requiresBatchControl;

    [ObservableProperty]
    private Guid _categoryId;

    [ObservableProperty]
    private Guid _defaultUnitOfMeasureId;

    [ObservableProperty]
    private decimal _currentCost;

    [ObservableProperty]
    private string? _imagePath;

    [ObservableProperty]
    private bool _isCatalogVisible = true;

    [ObservableProperty]
    private bool _isSoldOut;

    [ObservableProperty]
    private decimal _minimumStock;

    [ObservableProperty]
    private bool _isFavorite;

    [ObservableProperty]
    private int _favoriteOrder;

    [ObservableProperty]
    private bool _highlightInCatalog;

    [ObservableProperty]
    private string? _shortDescription;

    [ObservableProperty]
    private string? _catalogBadge;

    [ObservableProperty]
    private int _displayOrder;

    [ObservableProperty]
    private bool _autoMarkSoldOut = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHidden))]
    private bool _isActive = true;

    public bool IsHidden
    {
        get => !IsActive;
        set => IsActive = !value;
    }

    [ObservableProperty]
    private Guid _taxId;

    [ObservableProperty]
    private string? _priceChangeReason;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private byte[]? _selectedImageBytes;

    [ObservableProperty]
    private string? _selectedImageFileName;

    public ObservableCollection<CategoryDto> Categories { get; } = new();
    public ObservableCollection<UnitOfMeasureDto> UnitsOfMeasure { get; } = new();
    public ObservableCollection<TaxDto> AvailableTaxes { get; } = new();
    public ObservableCollection<ProductPresentationViewModel> Presentations { get; } = new();

    public event Action? RequestClose;

    public bool IsPriceChanged => Presentations.Any(p => p.Id != null && p.Id != Guid.Empty && 
        (p.RetailPrice != p.OriginalRetailPrice || 
         p.SemiWholesalePrice != p.OriginalSemiWholesalePrice || 
         p.WholesalePrice != p.OriginalWholesalePrice || 
         p.Cost != p.OriginalCost));

    public ProductEditorViewModel(
        ProductApiClient productApiClient,
        INotificationService notificationService,
        ProductDto? product = null)
    {
        _productApiClient = productApiClient;
        _notificationService = notificationService;
        _originalProduct = product;

        Presentations.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (ProductPresentationViewModel item in e.NewItems)
                {
                    item.PropertyChanged += Presentation_PropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (ProductPresentationViewModel item in e.OldItems)
                {
                    item.PropertyChanged -= Presentation_PropertyChanged;
                }
            }
            OnPropertyChanged(nameof(IsPriceChanged));
        };

        if (product != null)
        {
            IsEditMode = true;
            Title = "Editar Producto";

            InternalCode = product.InternalCode;
            Name = product.Name;
            Description = product.Description;
            ProductType = (int)product.ProductType;
            ProductStatus = (int)product.ProductStatus;
            TrackInventory = product.TrackInventory;
            RequiresSerialNumber = product.RequiresSerialNumber;
            RequiresBatchControl = product.RequiresBatchControl;
            CategoryId = product.CategoryId;
            DefaultUnitOfMeasureId = product.DefaultUnitOfMeasureId;
            CurrentCost = product.CurrentCost;
            ImagePath = product.ImagePath;
            IsCatalogVisible = product.IsCatalogVisible;
            IsSoldOut = product.IsSoldOut;
            MinimumStock = product.MinimumStock;
            IsFavorite = product.IsFavorite;
            FavoriteOrder = product.FavoriteOrder;
            HighlightInCatalog = product.HighlightInCatalog;
            ShortDescription = product.ShortDescription;
            CatalogBadge = product.CatalogBadge;
            DisplayOrder = product.DisplayOrder;
            AutoMarkSoldOut = product.AutoMarkSoldOut;
            IsActive = product.IsActive;
        }
    }

    private void Presentation_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProductPresentationViewModel.RetailPrice) || 
            e.PropertyName == nameof(ProductPresentationViewModel.SemiWholesalePrice) || 
            e.PropertyName == nameof(ProductPresentationViewModel.WholesalePrice) || 
            e.PropertyName == nameof(ProductPresentationViewModel.Cost))
        {
            OnPropertyChanged(nameof(IsPriceChanged));
        }
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            var categoriesResult = await _productApiClient.GetCategoriesAsync();
            Categories.Clear();
            if (categoriesResult?.Items != null)
            {
                foreach (var cat in categoriesResult.Items)
                {
                    Categories.Add(cat);
                }
            }

            var uomsResult = await _productApiClient.GetUnitsOfMeasureAsync();
            UnitsOfMeasure.Clear();
            if (uomsResult?.Items != null)
            {
                foreach (var uom in uomsResult.Items)
                {
                    UnitsOfMeasure.Add(uom);
                }
            }

            var taxesResult = await _productApiClient.GetTaxesAsync();
            AvailableTaxes.Clear();
            if (taxesResult?.Items != null)
            {
                foreach (var tax in taxesResult.Items)
                {
                    AvailableTaxes.Add(tax);
                }
            }

            if (_originalProduct != null)
            {
                var firstTax = _originalProduct.Taxes.FirstOrDefault();
                if (firstTax != null)
                {
                    TaxId = firstTax.Id;
                }

                var presentations = await _productApiClient.GetPresentationsAsync(_originalProduct.Id);
                Presentations.Clear();
                foreach (var pres in presentations)
                {
                    Presentations.Add(new ProductPresentationViewModel
                    {
                        Id = pres.Id,
                        OriginalRetailPrice = pres.RetailPrice,
                        OriginalSemiWholesalePrice = pres.SemiWholesalePrice,
                        OriginalWholesalePrice = pres.WholesalePrice,
                        OriginalCost = pres.Cost,
                        UnitOfMeasureId = pres.UnitOfMeasureId,
                        UnitOfMeasureCode = pres.UnitOfMeasureCode,
                        Name = pres.Name,
                        ConversionFactor = pres.ConversionFactor,
                        Barcode = pres.Barcode,
                        Cost = pres.Cost,
                        RetailPrice = pres.RetailPrice,
                        SemiWholesalePrice = pres.SemiWholesalePrice,
                        WholesalePrice = pres.WholesalePrice,
                        IsBaseUnit = pres.IsBaseUnit,
                        IsDefaultSalePresentation = pres.IsDefaultSalePresentation,
                        AllowPurchase = pres.AllowPurchase,
                        AllowSale = pres.AllowSale,
                        IsActive = pres.IsActive
                    });
                }
            }
            else
            {
                if (AvailableTaxes.Any())
                {
                    TaxId = AvailableTaxes.First().Id;
                }
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al inicializar datos: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SelectImage()
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Imágenes (*.jpg;*.jpeg;*.png;*.gif;*.webp)|*.jpg;*.jpeg;*.png;*.gif;*.webp",
            Title = "Seleccionar Imagen del Producto"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                var rawBytes = File.ReadAllBytes(openFileDialog.FileName);
                var (optimizedBytes, optimizedFileName) = OptimizeImage(rawBytes, openFileDialog.SafeFileName);

                SelectedImageBytes = optimizedBytes;
                SelectedImageFileName = optimizedFileName;

                var tempFile = Path.Combine(Path.GetTempPath(), $"preview_{Guid.NewGuid()}_{optimizedFileName}");
                File.WriteAllBytes(tempFile, optimizedBytes);
                ImagePath = tempFile;
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error al leer el archivo: {ex.Message}");
            }
        }
    }

    private (byte[] Bytes, string FileName) OptimizeImage(byte[] rawBytes, string originalFileName, int maxDimension = 1200)
    {
        try
        {
            using var ms = new MemoryStream(rawBytes);
            var decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(
                ms,
                System.Windows.Media.Imaging.BitmapCreateOptions.None,
                System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);

            if (decoder.Frames.Count == 0) return (rawBytes, originalFileName);

            var frame = decoder.Frames[0];
            double width = frame.PixelWidth;
            double height = frame.PixelHeight;

            if (width <= maxDimension && height <= maxDimension && rawBytes.Length <= 500 * 1024)
            {
                return (rawBytes, originalFileName);
            }

            double scale = 1.0;
            if (width > maxDimension || height > maxDimension)
            {
                scale = Math.Min((double)maxDimension / width, (double)maxDimension / height);
            }

            int targetWidth = Math.Max(1, (int)(width * scale));
            int targetHeight = Math.Max(1, (int)(height * scale));

            var resized = new System.Windows.Media.Imaging.TransformedBitmap(
                frame,
                new System.Windows.Media.ScaleTransform(
                    (double)targetWidth / width,
                    (double)targetHeight / height));

            var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
            using var outMs = new MemoryStream();

            if (ext == ".png")
            {
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(resized));
                encoder.Save(outMs);
                return (outMs.ToArray(), originalFileName);
            }
            else
            {
                var encoder = new System.Windows.Media.Imaging.JpegBitmapEncoder { QualityLevel = 85 };
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(resized));
                encoder.Save(outMs);
                var newFileName = Path.ChangeExtension(originalFileName, ".jpg");
                return (outMs.ToArray(), newFileName);
            }
        }
        catch
        {
            return (rawBytes, originalFileName);
        }
    }

    [RelayCommand]
    private void RemoveImage()
    {
        SelectedImageBytes = null;
        SelectedImageFileName = null;
        ImagePath = null;
    }

    [RelayCommand]
    private void RotateImage()
    {
        try
        {
            byte[]? currentBytes = SelectedImageBytes;
            if (currentBytes == null && !string.IsNullOrWhiteSpace(ImagePath))
            {
                if (File.Exists(ImagePath))
                {
                    currentBytes = File.ReadAllBytes(ImagePath);
                }
                else if (ImagePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    using var httpClient = new System.Net.Http.HttpClient();
                    currentBytes = httpClient.GetByteArrayAsync(ImagePath).GetAwaiter().GetResult();
                }
            }

            if (currentBytes == null || currentBytes.Length == 0) return;

            using var ms = new MemoryStream(currentBytes);
            var decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(
                ms,
                System.Windows.Media.Imaging.BitmapCreateOptions.None,
                System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);

            var frame = decoder.Frames[0];

            // Rotate 90 degrees Clockwise using native WPF TransformedBitmap
            var rotatedBitmap = new System.Windows.Media.Imaging.TransformedBitmap(
                frame,
                new System.Windows.Media.RotateTransform(90));

            var encoder = new System.Windows.Media.Imaging.JpegBitmapEncoder { QualityLevel = 85 };
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rotatedBitmap));

            using var outMs = new MemoryStream();
            encoder.Save(outMs);

            SelectedImageBytes = outMs.ToArray();
            SelectedImageFileName = $"rotated_{DateTime.UtcNow.Ticks}.jpg";

            // Save temp file for WPF preview
            var tempFile = Path.Combine(Path.GetTempPath(), $"preview_{Guid.NewGuid()}.jpg");
            File.WriteAllBytes(tempFile, SelectedImageBytes);
            ImagePath = tempFile;
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Error al rotar la imagen: {ex.Message}");
        }
    }

    [RelayCommand]
    private void AddPresentation()
    {
        var vm = new PresentationEditorViewModel(UnitsOfMeasure.ToList());
        var dialog = new Views.Inventory.PresentationEditorDialog { DataContext = vm };
        dialog.Owner = App.Current.MainWindow;
        vm.RequestClose += () =>
        {
            if (vm.DialogResult == true)
                dialog.DialogResult = true;
            dialog.Close();
        };
        if (dialog.ShowDialog() == true)
        {
            var model = new ProductPresentationViewModel
            {
                Name = vm.Name,
                UnitOfMeasureId = vm.UnitOfMeasureId,
                UnitOfMeasureCode = UnitsOfMeasure.First(u => u.Id == vm.UnitOfMeasureId).Code,
                ConversionFactor = vm.ConversionFactor,
                Barcode = vm.Barcode,
                Cost = vm.Cost,
                RetailPrice = vm.RetailPrice,
                SemiWholesalePrice = vm.SemiWholesalePrice,
                WholesalePrice = vm.WholesalePrice,
                IsBaseUnit = vm.IsBaseUnit,
                IsDefaultSalePresentation = vm.IsDefaultSalePresentation,
                AllowPurchase = vm.AllowPurchase,
                AllowSale = vm.AllowSale,
                IsActive = vm.IsActive
            };

            if (model.IsBaseUnit)
            {
                foreach (var p in Presentations) p.IsBaseUnit = false;
            }
            if (model.IsDefaultSalePresentation)
            {
                foreach (var p in Presentations) p.IsDefaultSalePresentation = false;
            }

            Presentations.Add(model);
        }
    }

    [RelayCommand]
    private void EditPresentation(ProductPresentationViewModel presentation)
    {
        if (presentation == null) return;
        var vm = new PresentationEditorViewModel(UnitsOfMeasure.ToList(), presentation);
        var dialog = new Views.Inventory.PresentationEditorDialog { DataContext = vm };
        dialog.Owner = App.Current.MainWindow;
        vm.RequestClose += () =>
        {
            if (vm.DialogResult == true)
                dialog.DialogResult = true;
            dialog.Close();
        };
        if (dialog.ShowDialog() == true)
        {
            presentation.Name = vm.Name;
            presentation.UnitOfMeasureId = vm.UnitOfMeasureId;
            presentation.UnitOfMeasureCode = UnitsOfMeasure.First(u => u.Id == vm.UnitOfMeasureId).Code;
            presentation.ConversionFactor = vm.ConversionFactor;
            presentation.Barcode = vm.Barcode;
            presentation.Cost = vm.Cost;
            presentation.RetailPrice = vm.RetailPrice;
            presentation.SemiWholesalePrice = vm.SemiWholesalePrice;
            presentation.WholesalePrice = vm.WholesalePrice;
            presentation.IsBaseUnit = vm.IsBaseUnit;
            presentation.IsDefaultSalePresentation = vm.IsDefaultSalePresentation;
            presentation.AllowPurchase = vm.AllowPurchase;
            presentation.AllowSale = vm.AllowSale;
            presentation.IsActive = vm.IsActive;

            if (presentation.IsBaseUnit)
            {
                foreach (var p in Presentations.Where(x => x != presentation)) p.IsBaseUnit = false;
            }
            if (presentation.IsDefaultSalePresentation)
            {
                foreach (var p in Presentations.Where(x => x != presentation)) p.IsDefaultSalePresentation = false;
            }

            OnPropertyChanged(nameof(IsPriceChanged));
        }
    }

    [RelayCommand]
    private void DeletePresentation(ProductPresentationViewModel presentation)
    {
        if (presentation == null) return;
        Presentations.Remove(presentation);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(InternalCode))
        {
            _notificationService.ShowWarning("El código es requerido.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            _notificationService.ShowWarning("El nombre del producto es requerido.");
            return;
        }

        if (CategoryId == Guid.Empty)
        {
            _notificationService.ShowWarning("Debe seleccionar una categoría.");
            return;
        }

        if (DefaultUnitOfMeasureId == Guid.Empty)
        {
            _notificationService.ShowWarning("Debe seleccionar una unidad de medida.");
            return;
        }

        if (TaxId == Guid.Empty)
        {
            _notificationService.ShowWarning("Debe seleccionar un impuesto.");
            return;
        }

        if (!Presentations.Any())
        {
            _notificationService.ShowWarning("Debe agregar al menos una presentación.");
            return;
        }

        if (Presentations.Count(p => p.IsBaseUnit) != 1)
        {
            _notificationService.ShowWarning("Debe marcar exactamente una presentación como unidad base.");
            return;
        }

        if (Presentations.Count(p => p.IsDefaultSalePresentation) != 1)
        {
            _notificationService.ShowWarning("Debe marcar exactamente una presentación como predeterminada de venta.");
            return;
        }

        var barcodes = Presentations.Where(p => !string.IsNullOrWhiteSpace(p.Barcode)).Select(p => p.Barcode).ToList();
        if (barcodes.Count != barcodes.Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            _notificationService.ShowWarning("No se permiten códigos de barra duplicados en las presentaciones.");
            return;
        }

        if (IsPriceChanged && string.IsNullOrWhiteSpace(PriceChangeReason))
        {
            _notificationService.ShowWarning("Debe ingresar el motivo del cambio de precio/costo.");
            return;
        }

        IsLoading = true;
        try
        {
            // Sort presentations: base unit MUST be first (backend validator requirement)
            var presentationsPayload = Presentations
                .OrderByDescending(p => p.IsBaseUnit)
                .ThenBy(p => p.ConversionFactor)
                .Select(p => new ProductPresentationInputDto(
                    p.Id,
                    p.UnitOfMeasureId,
                    p.Name,
                    p.ConversionFactor,
                    p.Barcode,
                    p.Cost,
                    p.RetailPrice,
                    p.SemiWholesalePrice,
                    p.WholesalePrice,
                    p.IsBaseUnit,
                    p.IsDefaultSalePresentation,
                    p.AllowPurchase,
                    p.AllowSale,
                    p.IsActive
                )).ToList();

            if (IsEditMode && _originalProduct != null)
            {
                var command = new
                {
                    Id = _originalProduct.Id,
                    InternalCode,
                    Name,
                    Description,
                    ProductType,
                    ProductStatus,
                    TrackInventory,
                    RequiresSerialNumber,
                    RequiresBatchControl,
                    CategoryId,
                    BrandId = _originalProduct.BrandId,
                    DefaultUnitOfMeasureId,
                    CurrentCost,
                    ImagePath = _originalProduct.ImagePath,
                    IsCatalogVisible,
                    IsSoldOut,
                    MinimumStock,
                    IsFavorite,
                    FavoriteOrder,
                    AllowPromotions = true,
                    HighlightInCatalog,
                    ShortDescription,
                    CatalogBadge,
                    DisplayOrder,
                    AutoMarkSoldOut,
                    TaxId,
                    PriceChangeReason,
                    IsActive = IsActive,
                    Presentations = presentationsPayload,
                    BranchOverrides = new List<object>()
                };

                var success = await _productApiClient.UpdateProductAsync(_originalProduct.Id, command);
                if (success)
                {
                    if (SelectedImageBytes != null && SelectedImageFileName != null)
                    {
                        var newImageUrl = await _productApiClient.UploadImageAsync(_originalProduct.Id, SelectedImageBytes, SelectedImageFileName);
                        if (!string.IsNullOrWhiteSpace(newImageUrl))
                        {
                            ImagePath = newImageUrl;
                        }
                    }
                    else if (ImagePath == null && _originalProduct.ImagePath != null)
                    {
                        await _productApiClient.DeleteImageAsync(_originalProduct.Id);
                    }

                    _notificationService.ShowSuccess("Producto actualizado exitosamente.");
                    RequestClose?.Invoke();
                }
                else
                {
                    _notificationService.ShowError("Error al actualizar el producto.");
                }
            }
            else
            {
                var command = new
                {
                    InternalCode,
                    Name,
                    Description,
                    ProductType,
                    ProductStatus,
                    TrackInventory,
                    RequiresSerialNumber,
                    RequiresBatchControl,
                    CategoryId,
                    BrandId = (Guid?)null,
                    DefaultUnitOfMeasureId,
                    CurrentCost,
                    ImagePath = (string?)null,
                    IsCatalogVisible,
                    IsSoldOut,
                    MinimumStock,
                    IsFavorite,
                    FavoriteOrder,
                    AllowPromotions = true,
                    HighlightInCatalog,
                    ShortDescription,
                    CatalogBadge,
                    DisplayOrder,
                    AutoMarkSoldOut,
                    TaxId,
                    IsActive = IsActive,
                    Presentations = presentationsPayload,
                    BranchOverrides = new List<object>()
                };

                var productId = await _productApiClient.CreateProductAsync(command);
                if (productId != Guid.Empty)
                {
                    if (SelectedImageBytes != null && SelectedImageFileName != null)
                    {
                        await _productApiClient.UploadImageAsync(productId, SelectedImageBytes, SelectedImageFileName);
                    }

                    _notificationService.ShowSuccess("Producto creado exitosamente.");
                    RequestClose?.Invoke();
                }
                else
                {
                    _notificationService.ShowError("Error al crear el producto.");
                }
            }
        }
        catch (Exception ex)
        {
            // Try to extract readable validation messages from the raw JSON error
            var msg = ex.Message;
            try
            {
                if (msg.Contains("\"errors\"") || msg.Contains("\"detail\""))
                {
                    var json = System.Text.Json.JsonDocument.Parse(msg.Substring(msg.IndexOf('{')));
                    if (json.RootElement.TryGetProperty("detail", out var detail))
                    {
                        msg = detail.GetString() ?? msg;
                    }
                    if (json.RootElement.TryGetProperty("errors", out var errors))
                    {
                        var errorMessages = new System.Collections.Generic.List<string>();
                        foreach (var prop in errors.EnumerateObject())
                        {
                            foreach (var err in prop.Value.EnumerateArray())
                            {
                                errorMessages.Add(err.GetString() ?? "");
                            }
                        }
                        if (errorMessages.Any())
                        {
                            msg = string.Join("\n", errorMessages);
                        }
                    }
                }
            }
            catch { /* Use original message if parsing fails */ }
            _notificationService.ShowError($"Error al guardar: {msg}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke();
    }
}
