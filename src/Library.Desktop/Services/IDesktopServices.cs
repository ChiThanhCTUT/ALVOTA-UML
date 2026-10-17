namespace Library.Desktop.Services;

public interface IDialogService
{
    Task ShowInfoAsync(string message, string title = "Thông báo");
    Task ShowWarningAsync(string message, string title = "Cảnh báo");
    Task ShowErrorAsync(string message, string title = "Lỗi");
    Task<bool> ShowConfirmationAsync(string message, string title = "Xác nhận");
}

public interface INavigationService
{
    object? CurrentViewModel { get; }
    event Action? CurrentViewModelChanged;
    void NavigateTo<TViewModel>() where TViewModel : class;
    void NavigateTo(object viewModel);
}

public interface IAudioService
{
    void PlaySuccess();
    void PlayError();
    void PlayWarning();
}
