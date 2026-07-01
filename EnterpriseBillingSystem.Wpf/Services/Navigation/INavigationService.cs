using System;

namespace EnterpriseBillingSystem.Wpf.Services.Navigation;

public interface INavigationService
{
    object? CurrentViewModel { get; }
    event Action? CurrentViewModelChanged;
    void Navigate<TViewModel>() where TViewModel : class;
    void GoBack();
}
