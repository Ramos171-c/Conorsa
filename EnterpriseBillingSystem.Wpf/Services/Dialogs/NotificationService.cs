using CommunityToolkit.Mvvm.Messaging;
using EnterpriseBillingSystem.Wpf.Helpers;

namespace EnterpriseBillingSystem.Wpf.Services.Dialogs;

public class NotificationService : INotificationService
{
    public void ShowSuccess(string message)
    {
        WeakReferenceMessenger.Default.Send(new NotificationMessage(NotificationType.Success, message));
    }

    public void ShowWarning(string message)
    {
        WeakReferenceMessenger.Default.Send(new NotificationMessage(NotificationType.Warning, message));
    }

    public void ShowError(string message)
    {
        WeakReferenceMessenger.Default.Send(new NotificationMessage(NotificationType.Error, message));
    }
}
