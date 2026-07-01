using System.Windows;
using System.Windows.Controls;
using EnterpriseBillingSystem.Wpf.ViewModels;

namespace EnterpriseBillingSystem.Wpf.Views.Users;

public partial class UsersView : UserControl
{
    public UsersView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is UsersViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}
