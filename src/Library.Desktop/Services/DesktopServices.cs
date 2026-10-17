using System.Media;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace Library.Desktop.Services;

public sealed class DialogService : IDialogService
{
    public Task ShowInfoAsync(string message, string title = "Thông báo")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        return Task.CompletedTask;
    }

    public Task ShowWarningAsync(string message, string title = "Cảnh báo")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        return Task.CompletedTask;
    }

    public Task ShowErrorAsync(string message, string title = "Lỗi")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        return Task.CompletedTask;
    }

    public Task<bool> ShowConfirmationAsync(string message, string title = "Xác nhận")
    {
        var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return Task.FromResult(result == MessageBoxResult.Yes);
    }
}

public sealed class AudioService : IAudioService
{
    public void PlaySuccess()
    {
        try
        {
            SystemSounds.Asterisk.Play();
        }
        catch
        {
            // Ignore sound error in headless or silent environments
        }
    }

    public void PlayError()
    {
        try
        {
            SystemSounds.Hand.Play();
        }
        catch
        {
            // Ignore sound error
        }
    }

    public void PlayWarning()
    {
        try
        {
            SystemSounds.Exclamation.Play();
        }
        catch
        {
            // Ignore sound error
        }
    }
}

public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private object? _currentViewModel;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public object? CurrentViewModel => _currentViewModel;

    public event Action? CurrentViewModelChanged;

    public void NavigateTo<TViewModel>() where TViewModel : class
    {
        var vm = _serviceProvider.GetRequiredService<TViewModel>();
        NavigateTo(vm);
    }

    public void NavigateTo(object viewModel)
    {
        _currentViewModel = viewModel;
        CurrentViewModelChanged?.Invoke();
    }
}
