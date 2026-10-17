using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Library.Application.Features.Auth.Commands;
using Library.Desktop.Services;
using Library.Infrastructure.Services;
using MediatR;

namespace Library.Desktop.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialog;
    private readonly IAudioService _audio;
    private readonly CurrentUserService _currentUser;

    [ObservableProperty] private string _username = "admin";
    [ObservableProperty] private string _password = "Admin@123";
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    public LoginViewModel(
        IMediator mediator,
        INavigationService navigation,
        IDialogService dialog,
        IAudioService audio,
        CurrentUserService currentUser)
    {
        _mediator = mediator;
        _navigation = navigation;
        _dialog = dialog;
        _audio = audio;
        _currentUser = currentUser;
    }

    [RelayCommand]
    private async Task LoginAsync(CancellationToken ct)
    {
        if (IsBusy) return;

        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.";
            _audio.PlayError();
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _mediator.Send(new LoginCommand(Username.Trim(), Password), ct);
            if (result.IsFailure)
            {
                ErrorMessage = result.Error.Message;
                _audio.PlayError();
                return;
            }

            var user = result.Value.User;
            _currentUser.SetUser(user.Id.ToString(), user.Username, user.Role);
            _audio.PlaySuccess();

            _navigation.NavigateTo<MainViewModel>();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = "Đã xảy ra lỗi kết nối: " + ex.Message;
            _audio.PlayError();
        }
        finally
        {
            IsBusy = false;
        }
    }
}
