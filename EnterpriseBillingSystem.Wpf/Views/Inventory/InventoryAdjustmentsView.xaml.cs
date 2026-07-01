using System.Windows.Controls;
using EnterpriseBillingSystem.Wpf.ViewModels;

namespace EnterpriseBillingSystem.Wpf.Views.Inventory;

public partial class InventoryAdjustmentsView : UserControl
{
    public InventoryAdjustmentsView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is InventoryAdjustmentsViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}
