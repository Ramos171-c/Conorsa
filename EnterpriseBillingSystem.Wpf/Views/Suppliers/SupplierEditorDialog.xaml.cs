using System.Windows;

namespace EnterpriseBillingSystem.Wpf.Views.Suppliers;

public partial class SupplierEditorDialog : Window
{
    public SupplierEditorDialog()
    {
        InitializeComponent();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
