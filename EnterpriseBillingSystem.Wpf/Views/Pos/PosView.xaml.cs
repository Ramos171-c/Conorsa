using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using EnterpriseBillingSystem.Wpf.Models;
using EnterpriseBillingSystem.Wpf.ViewModels;

namespace EnterpriseBillingSystem.Wpf.Views.Pos;

public partial class PosView : UserControl
{
    private DispatcherTimer? _productDebounceTimer;
    private DispatcherTimer? _customerDebounceTimer;

    public PosView()
    {
        InitializeComponent();
        SetupDebounceTimers();
    }

    private void SetupDebounceTimers()
    {
        _productDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _productDebounceTimer.Tick += async (s, e) =>
        {
            _productDebounceTimer.Stop();
            if (DataContext is PosViewModel viewModel)
            {
                await viewModel.SearchProductsAsync(ProductSearchInput.Text);
            }
        };

        _customerDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _customerDebounceTimer.Tick += async (s, e) =>
        {
            _customerDebounceTimer.Stop();
            if (DataContext is PosViewModel viewModel)
            {
                await viewModel.SearchCustomersAsync(CustomerSearchInput.Text);
            }
        };
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is PosViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
        BarcodeScannerInput.Focus();
    }

    private void ProductSearchInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        _productDebounceTimer?.Stop();
        _productDebounceTimer?.Start();
    }

    private void CustomerSearchInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        _customerDebounceTimer?.Stop();
        _customerDebounceTimer?.Start();
    }

    private void BarcodeScannerInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is PosViewModel viewModel)
            {
                var code = BarcodeScannerInput.Text;
                if (!string.IsNullOrWhiteSpace(code))
                {
                    viewModel.AddProductByBarcodeAsync(code).ContinueWith(t =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (t.Result)
                            {
                                BarcodeScannerInput.Text = string.Empty;
                            }
                            else
                            {
                                viewModel.SearchProductsAsync(code).ContinueWith(t2 =>
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        var match = viewModel.ProductSearchResults.FirstOrDefault(p => 
                                            (p.Barcode != null && p.Barcode.Equals(code, StringComparison.OrdinalIgnoreCase)) || 
                                            p.InternalCode.Equals(code, StringComparison.OrdinalIgnoreCase));
                                        
                                        if (match != null)
                                        {
                                            viewModel.AddProductToCart(match);
                                            BarcodeScannerInput.Text = string.Empty;
                                        }
                                        else
                                        {
                                            viewModel.ProductSearchResults.Clear();
                                        }
                                    });
                                });
                            }
                        });
                    });
                }
            }
        }
    }

    private void ProductResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProductsResultList.SelectedItem is ProductSearchResultDto selectedProduct)
        {
            if (DataContext is PosViewModel viewModel)
            {
                viewModel.AddProductToCart(selectedProduct);
                ProductSearchInput.Text = string.Empty;
                viewModel.ProductSearchResults.Clear();
                BarcodeScannerInput.Focus();
            }
        }
    }

    private void CustomerResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var listBox = (ListBox)sender;
        if (listBox.SelectedItem is CustomerSearchResultDto selectedCustomer)
        {
            if (DataContext is PosViewModel viewModel)
            {
                viewModel.SelectedCustomer = selectedCustomer;
                CustomerSearchInput.Text = string.Empty;
                viewModel.CustomerSearchResults.Clear();
                BarcodeScannerInput.Focus();
            }
        }
    }

    private void DeleteCartItem_Click(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        if (button.DataContext is CartItemViewModel item)
        {
            if (DataContext is PosViewModel viewModel)
            {
                viewModel.RemoveProductFromCart(item);
            }
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (DataContext is PosViewModel viewModel)
        {
            switch (e.Key)
            {
                case Key.F2:
                    ProductSearchInput.Focus();
                    ProductSearchInput.SelectAll();
                    e.Handled = true;
                    break;
                case Key.F4:
                    CustomerSearchInput.Focus();
                    CustomerSearchInput.SelectAll();
                    e.Handled = true;
                    break;
                case Key.F8:
                    if (viewModel.OpenPaymentCommand.CanExecute(null))
                    {
                        viewModel.OpenPaymentCommand.Execute(null);
                    }
                    e.Handled = true;
                    break;
                case Key.Escape:
                    if (viewModel.IsPaymentPanelOpen)
                    {
                        viewModel.CancelPaymentCommand.Execute(null);
                    }
                    else
                    {
                        viewModel.ClearSaleCommand.Execute(null);
                    }
                    BarcodeScannerInput.Focus();
                    e.Handled = true;
                    break;
            }
        }
    }

    private void Contado_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is PosViewModel viewModel)
        {
            viewModel.IsCreditSale = false;
        }
    }

    private void Credito_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is PosViewModel viewModel)
        {
            viewModel.IsCreditSale = true;
        }
    }
}
