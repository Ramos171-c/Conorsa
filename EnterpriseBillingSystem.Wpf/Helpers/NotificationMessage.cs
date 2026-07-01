namespace EnterpriseBillingSystem.Wpf.Helpers;

public enum NotificationType
{
    Success,
    Warning,
    Error
}

public record NotificationMessage(NotificationType Type, string Message);
