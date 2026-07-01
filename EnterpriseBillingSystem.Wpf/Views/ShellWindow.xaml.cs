using System;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using EnterpriseBillingSystem.Wpf.Helpers;
using EnterpriseBillingSystem.Wpf.ViewModels;

namespace EnterpriseBillingSystem.Wpf.Views;

public partial class ShellWindow : Window
{
    public ShellWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Register for snackbar notifications
        WeakReferenceMessenger.Default.Register<NotificationMessage>(this, (r, m) =>
        {
            Dispatcher.Invoke(() =>
            {
                MainSnackbar.MessageQueue?.Enqueue(m.Message);
            });
        });

        // Register for logout messages
        WeakReferenceMessenger.Default.Register<LogoutMessage>(this, (r, m) =>
        {
            Dispatcher.Invoke(() =>
            {
                var loginWindow = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Login.LoginWindow>(App.AppHost!.Services);
                loginWindow.Show();
                this.Close();
                Application.Current.MainWindow = loginWindow;
            });
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        WeakReferenceMessenger.Default.Unregister<NotificationMessage>(this);
        WeakReferenceMessenger.Default.Unregister<LogoutMessage>(this);
    }
}
