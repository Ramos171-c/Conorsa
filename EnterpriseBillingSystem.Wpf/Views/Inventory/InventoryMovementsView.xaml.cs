using System.Windows;
using System.Windows.Controls;
using EnterpriseBillingSystem.Wpf.ViewModels;

namespace EnterpriseBillingSystem.Wpf.Views.Inventory;

public partial class InventoryMovementsView : UserControl
{
    public InventoryMovementsView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is InventoryMovementsViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}
