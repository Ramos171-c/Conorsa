namespace EnterpriseBillingSystem.Wpf.Services.Dialogs;

public interface INotificationService
{
    void ShowSuccess(string message);
    void ShowWarning(string message);
    void ShowError(string message);
}
