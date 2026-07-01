using System.Windows;

namespace EnterpriseBillingSystem.Wpf.Views.Customers;

public partial class CustomerEditorDialog : Window
{
    public CustomerEditorDialog()
    {
        InitializeComponent();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
