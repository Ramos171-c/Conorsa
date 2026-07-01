using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseBillingSystem.Wpf.Services.Navigation;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Stack<object> _history = new();
    private object? _currentViewModel;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public object? CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            if (_currentViewModel != value)
            {
                _currentViewModel = value;
                CurrentViewModelChanged?.Invoke();
            }
        }
    }

    public event Action? CurrentViewModelChanged;

    public void Navigate<TViewModel>() where TViewModel : class
    {
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        if (CurrentViewModel != null)
        {
            _history.Push(CurrentViewModel);
        }
        CurrentViewModel = viewModel;
    }

    public void GoBack()
    {
        if (_history.Count > 0)
        {
            CurrentViewModel = _history.Pop();
        }
    }
}
