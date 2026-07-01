using System.Windows;
using System.Windows.Controls;
using EnterpriseBillingSystem.Wpf.ViewModels;

namespace EnterpriseBillingSystem.Wpf.Views.Login;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.LoginSuccess += () =>
        {
            Dispatcher.Invoke(() =>
            {
                var shellWindow = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<ShellWindow>(App.AppHost!.Services);
                shellWindow.Show();
                this.Close();
                Application.Current.MainWindow = shellWindow;
            });
        };
    }

    private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel)
        {
            viewModel.Password = ((PasswordBox)sender).Password;
        }
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
