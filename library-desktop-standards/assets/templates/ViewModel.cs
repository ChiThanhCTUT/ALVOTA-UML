// Desktop/ViewModels/LoanViewModel.cs
// Code-behind của View để trống. Không tham chiếu control/MessageBox.
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace Library.Desktop.ViewModels;

public partial class LoanViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IDialogService _dialogs;

    [ObservableProperty] private string _barcode = string.Empty;
    [ObservableProperty] private bool _isBusy;

    public LoanViewModel(IMediator mediator, IDialogService dialogs)
        => (_mediator, _dialogs) = (mediator, dialogs);

    [RelayCommand]
    private async Task ScanAsync(CancellationToken ct)
    {
        if (IsBusy || string.IsNullOrWhiteSpace(Barcode)) return;

        IsBusy = true;
        try
        {
            var result = await _mediator.Send(new BorrowBookCommand(CurrentMemberId, Barcode), ct);
            if (result.IsFailure)
                await _dialogs.ShowErrorAsync(result.Error.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Lỗi bất thường: log rồi báo thân thiện, không nuốt.
            await _dialogs.ShowErrorAsync(Strings.UnexpectedError);
            throw;
        }
        finally
        {
            Barcode = string.Empty; // sẵn sàng cho lần quét tiếp theo
            IsBusy = false;
        }
    }
}
