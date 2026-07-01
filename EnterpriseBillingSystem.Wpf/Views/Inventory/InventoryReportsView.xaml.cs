using System.Windows.Controls;
using EnterpriseBillingSystem.Wpf.ViewModels;

namespace EnterpriseBillingSystem.Wpf.Views.Inventory;

public partial class InventoryReportsView : UserControl
{
    public InventoryReportsView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is InventoryReportsViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}
