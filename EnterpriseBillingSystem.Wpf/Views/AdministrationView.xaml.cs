using System.Windows;
using System.Windows.Controls;
using EnterpriseBillingSystem.Wpf.ViewModels;

namespace EnterpriseBillingSystem.Wpf.Views;

public partial class AdministrationView : UserControl
{
    public AdministrationView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AdministrationViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}
