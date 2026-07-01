using System.Windows.Controls;
using EnterpriseBillingSystem.Wpf.ViewModels;

namespace EnterpriseBillingSystem.Wpf.Views.Inventory;

public partial class InventoryDashboardView : UserControl
{
    public InventoryDashboardView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is InventoryDashboardViewModel vm)
        {
            await vm.LoadDashboardAsync();
        }
    }
}
