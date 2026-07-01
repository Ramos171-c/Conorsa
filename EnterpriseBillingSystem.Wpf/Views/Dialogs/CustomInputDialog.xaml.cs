using System.Windows;
using System.Windows.Input;

namespace EnterpriseBillingSystem.Wpf.Views.Dialogs;

public partial class CustomInputDialog : Window
{
    public bool IsConfirmed { get; private set; }
    public string InputText => InputTextBox.Text;

    public CustomInputDialog(string prompt, string title, string defaultValue = "")
    {
        InitializeComponent();
        
        TitleTextBlock.Text = title;
        PromptTextBlock.Text = prompt;
        InputTextBox.Text = defaultValue;
        
        InputTextBox.Focus();
        if (!string.IsNullOrEmpty(defaultValue))
        {
            InputTextBox.SelectAll();
        }
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        ConfirmAndClose();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        CancelAndClose();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CancelAndClose();
    }

    private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ConfirmAndClose();
        }
        else if (e.Key == Key.Escape)
        {
            CancelAndClose();
        }
    }

    private void ConfirmAndClose()
    {
        IsConfirmed = true;
        DialogResult = true;
        Close();
    }

    private void CancelAndClose()
    {
        IsConfirmed = false;
        DialogResult = false;
        Close();
    }

    public static (bool IsConfirmed, string Text) Show(string prompt, string title = "Entrada de Datos", string defaultValue = "")
    {
        bool isConfirmed = false;
        string text = string.Empty;

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
            var dialog = new CustomInputDialog(prompt, title, defaultValue)
            {
                Owner = activeWindow
            };
            dialog.ShowDialog();
            isConfirmed = dialog.IsConfirmed;
            text = dialog.InputText;
        }
        else
        {
            var dialog = new CustomInputDialog(prompt, title, defaultValue)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            dialog.ShowDialog();
            isConfirmed = dialog.IsConfirmed;
            text = dialog.InputText;
        }

        return (isConfirmed, text);
    }
}
