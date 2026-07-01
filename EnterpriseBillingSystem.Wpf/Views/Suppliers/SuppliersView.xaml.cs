using System.Windows;
using System.Windows.Controls;
using EnterpriseBillingSystem.Wpf.ViewModels;

namespace EnterpriseBillingSystem.Wpf.Views.Suppliers;

public partial class SuppliersView : UserControl
{
    public SuppliersView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SuppliersViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}
