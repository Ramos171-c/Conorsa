using System;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace EnterpriseBillingSystem.Wpf.Views.Dialogs;

public partial class CustomMessageBox : Window
{
    public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

    public CustomMessageBox(string message, string title, MessageBoxButton buttons, MessageBoxImage icon)
    {
        InitializeComponent();
        
        TitleTextBlock.Text = title;
        MessageTextBlock.Text = message;

        ConfigureIcon(icon);
        ConfigureButtons(buttons);
    }

    private void ConfigureIcon(MessageBoxImage icon)
    {
        switch (icon)
        {
            case MessageBoxImage.Information:
                DialogIcon.Kind = PackIconKind.Information;
                DialogIcon.Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Blue
                break;
            case MessageBoxImage.Question:
                DialogIcon.Kind = PackIconKind.HelpCircle;
                DialogIcon.Foreground = (Brush?)Application.Current.TryFindResource("PrimaryHueMidBrush") 
                                         ?? new SolidColorBrush(Color.FromRgb(103, 58, 183)); // Fallback Deep Purple
                break;
            case MessageBoxImage.Warning:
                DialogIcon.Kind = PackIconKind.Alert;
                DialogIcon.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Amber
                break;
            case MessageBoxImage.Error: // Covers Hand and Stop (value 16)
                DialogIcon.Kind = PackIconKind.AlertCircle;
                DialogIcon.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40)); // Red
                break;
            default:
                if (TitleTextBlock.Text.Contains("éxito", StringComparison.OrdinalIgnoreCase) || 
                    TitleTextBlock.Text.Contains("exitos", StringComparison.OrdinalIgnoreCase) ||
                    TitleTextBlock.Text.Contains("completad", StringComparison.OrdinalIgnoreCase) ||
                    TitleTextBlock.Text.Contains("confirmad", StringComparison.OrdinalIgnoreCase))
                {
                    DialogIcon.Kind = PackIconKind.CheckCircle;
                    DialogIcon.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)); // Green
                }
                else
                {
                    DialogIcon.Visibility = Visibility.Collapsed;
                }
                break;
        }
    }

    private void ConfigureButtons(MessageBoxButton buttons)
    {
        BtnYes.Visibility = Visibility.Collapsed;
        BtnNo.Visibility = Visibility.Collapsed;
        BtnOk.Visibility = Visibility.Collapsed;
        BtnCancel.Visibility = Visibility.Collapsed;

        switch (buttons)
        {
            case MessageBoxButton.OK:
                BtnOk.Visibility = Visibility.Visible;
                BtnOk.IsDefault = true;
                break;
            case MessageBoxButton.OKCancel:
                BtnOk.Visibility = Visibility.Visible;
                BtnCancel.Visibility = Visibility.Visible;
                BtnOk.IsDefault = true;
                BtnCancel.IsCancel = true;
                break;
            case MessageBoxButton.YesNo:
                BtnYes.Visibility = Visibility.Visible;
                BtnNo.Visibility = Visibility.Visible;
                BtnYes.IsDefault = true;
                BtnNo.IsCancel = true;
                break;
            case MessageBoxButton.YesNoCancel:
                BtnYes.Visibility = Visibility.Visible;
                BtnNo.Visibility = Visibility.Visible;
                BtnCancel.Visibility = Visibility.Visible;
                BtnYes.IsDefault = true;
                BtnCancel.IsCancel = true;
                break;
        }
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.OK;
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.Cancel;
        DialogResult = false;
        Close();
    }

    private void BtnYes_Click(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.Yes;
        DialogResult = true;
        Close();
    }

    private void BtnNo_Click(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.No;
        DialogResult = false;
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.Cancel;
        Close();
    }

    public static MessageBoxResult Show(string message, string title = "Mensaje", MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None)
    {
        MessageBoxResult result = MessageBoxResult.None;
        
        Window? activeWindow = null;
        foreach (Window window in Application.Current.Windows)
        {
            if (window.IsActive)
            {
                activeWindow = window;
                break;
            }
        }
        
        if (activeWindow == null)
            activeWindow = Application.Current.MainWindow;

        if (activeWindow != null && activeWindow.IsVisible)
        {
            var dialog = new CustomMessageBox(message, title, buttons, icon)
            {
                Owner = activeWindow
            };
            dialog.ShowDialog();
            result = dialog.Result;
        }
        else
        {
            var dialog = new CustomMessageBox(message, title, buttons, icon)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            dialog.ShowDialog();
            result = dialog.Result;
        }

        return result;
    }
}
