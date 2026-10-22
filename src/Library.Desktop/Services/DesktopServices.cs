using System.Media;
using System.Runtime.CompilerServices;
using System.Windows;
using MediatR;
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

/// <summary>
/// Tạo IServiceScope riêng cho mỗi request MediatR trên Desktop nhằm đảm bảo mỗi
/// Command/Query sử dụng một LibraryDbContext độc lập và giải phóng ngay khi hoàn tất.
/// </summary>
public sealed class ScopedMediator : IMediator
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ScopedMediator(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var mediator = new Mediator(scope.ServiceProvider);
        return await mediator.Send(request, cancellationToken);
    }

    public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var mediator = new Mediator(scope.ServiceProvider);
        await mediator.Send(request, cancellationToken);
    }

    public async Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var mediator = new Mediator(scope.ServiceProvider);
        return await mediator.Send(request, cancellationToken);
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var scope = _scopeFactory.CreateAsyncScope();
        var mediator = new Mediator(scope.ServiceProvider);
        return CreateScopedStreamAsync(scope, mediator.CreateStream(request, cancellationToken), cancellationToken);
    }

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
    {
        var scope = _scopeFactory.CreateAsyncScope();
        var mediator = new Mediator(scope.ServiceProvider);
        return CreateScopedStreamAsync(scope, mediator.CreateStream(request, cancellationToken), cancellationToken);
    }

    public async Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var mediator = new Mediator(scope.ServiceProvider);
        await mediator.Publish(notification, cancellationToken);
    }

    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var mediator = new Mediator(scope.ServiceProvider);
        await mediator.Publish(notification, cancellationToken);
    }

    private static async IAsyncEnumerable<T> CreateScopedStreamAsync<T>(
        AsyncServiceScope scope,
        IAsyncEnumerable<T> source,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await using (scope)
        {
            await foreach (var item in source.WithCancellation(ct))
            {
                yield return item;
            }
        }
    }
}
