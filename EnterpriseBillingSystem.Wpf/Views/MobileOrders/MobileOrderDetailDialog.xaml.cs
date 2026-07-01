using System.Windows;

namespace EnterpriseBillingSystem.Wpf.Views.MobileOrders;

public partial class MobileOrderDetailDialog : Window
{
    public MobileOrderDetailDialog()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
