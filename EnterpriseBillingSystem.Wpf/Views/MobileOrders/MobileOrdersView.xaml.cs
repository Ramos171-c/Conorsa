using System.Windows;
using System.Windows.Controls;
using EnterpriseBillingSystem.Wpf.ViewModels;

namespace EnterpriseBillingSystem.Wpf.Views.MobileOrders;

public partial class MobileOrdersView : UserControl
{
    public MobileOrdersView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MobileOrdersViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}
