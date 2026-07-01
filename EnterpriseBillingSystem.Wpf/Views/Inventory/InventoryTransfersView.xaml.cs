using System.Windows.Controls;
using EnterpriseBillingSystem.Wpf.ViewModels;

namespace EnterpriseBillingSystem.Wpf.Views.Inventory;

public partial class InventoryTransfersView : UserControl
{
    public InventoryTransfersView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is InventoryTransfersViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}
