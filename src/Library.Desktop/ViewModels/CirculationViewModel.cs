using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Library.Application.Features.Circulation.Commands.BorrowBook;
using Library.Application.Features.Circulation.Commands.RenewLoan;
using Library.Application.Features.Circulation.Commands.ReturnBook;
using Library.Application.Features.Circulation.DTOs;
using Library.Application.Features.Circulation.Queries;
using Library.Application.Features.Members.DTOs;
using Library.Application.Features.Members.Queries;
using Library.Desktop.Services;
using Library.Domain.Enums;
using MediatR;

namespace Library.Desktop.ViewModels;

public partial class CirculationViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IDialogService _dialog;
    private readonly IAudioService _audio;

    [ObservableProperty] private int _selectedTabIndex = 0; // 0: Mượn, 1: Trả, 2: Quá hạn
    [ObservableProperty] private bool _isBusy;

    // --- MƯỢN SÁCH (BORROW) ---
    [ObservableProperty] private string _memberCardInput = string.Empty;
    [ObservableProperty] private MemberDto? _selectedMember;
    [ObservableProperty] private string _bookBarcodeInput = string.Empty;
    [ObservableProperty] private string? _borrowMessage;
    [ObservableProperty] private bool _borrowHasError;
    public ObservableCollection<LoanItemDto> MemberActiveLoans { get; } = new();

    // --- TRẢ SÁCH (RETURN) ---
    [ObservableProperty] private string _returnBarcodeInput = string.Empty;
    [ObservableProperty] private ReturnCondition _selectedCondition = ReturnCondition.Normal;
    [ObservableProperty] private string? _returnNotes;
    [ObservableProperty] private ReturnResultDto? _lastReturnResult;
    [ObservableProperty] private string? _returnMessage;
    [ObservableProperty] private bool _returnHasError;

    // --- DANH SÁCH QUÁ HẠN (OVERDUE LIST - CIR-07) ---
    public ObservableCollection<LoanItemDto> OverdueLoans { get; } = new();

    public CirculationViewModel(IMediator mediator, IDialogService dialog, IAudioService audio)
    {
        _mediator = mediator;
        _dialog = dialog;
        _audio = audio;
    }

    [RelayCommand]
    public async Task SearchMemberAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(MemberCardInput)) return;

        BorrowMessage = null;
        BorrowHasError = false;
        IsBusy = true;
        try
        {
            var result = await _mediator.Send(new GetMemberByCardNumberQuery(MemberCardInput.Trim()), ct);
            if (result.IsFailure)
            {
                SelectedMember = null;
                MemberActiveLoans.Clear();
                BorrowMessage = "Không tìm thấy bạn đọc với mã thẻ: " + MemberCardInput;
                BorrowHasError = true;
                _audio.PlayError();
                return;
            }

            SelectedMember = result.Value;
            _audio.PlaySuccess();

            // Load active loans of member
            var loansResult = await _mediator.Send(new GetActiveLoansByMemberQuery(SelectedMember.Id), ct);
            MemberActiveLoans.Clear();
            if (loansResult.IsSuccess)
            {
                foreach (var item in loansResult.Value)
                    MemberActiveLoans.Add(item);
            }

            // Check if member has warnings
            if (SelectedMember.CardStatus != CardStatus.Active)
            {
                BorrowMessage = $"CẢNH BÁO: Thẻ bạn đọc đang ở trạng thái '{SelectedMember.CardStatus}'. Không được mượn!";
                BorrowHasError = true;
                _audio.PlayWarning();
            }
            else if (SelectedMember.UnpaidFineAmount > 20000m)
            {
                BorrowMessage = $"CẢNH BÁO: Bạn đọc còn nợ phạt {SelectedMember.UnpaidFineAmount:N0} đ (vượt ngưỡng 20.000 đ). Không được mượn!";
                BorrowHasError = true;
                _audio.PlayWarning();
            }
        }
        catch (Exception ex)
        {
            BorrowMessage = "Lỗi tìm bạn đọc: " + ex.Message;
            BorrowHasError = true;
            _audio.PlayError();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ConfirmBorrowAsync(CancellationToken ct = default)
    {
        if (SelectedMember == null)
        {
            BorrowMessage = "Vui lòng quét hoặc nhập mã thẻ bạn đọc trước.";
            BorrowHasError = true;
            _audio.PlayError();
            return;
        }

        if (string.IsNullOrWhiteSpace(BookBarcodeInput))
        {
            BorrowMessage = "Vui lòng quét hoặc nhập mã vạch sách.";
            BorrowHasError = true;
            _audio.PlayError();
            return;
        }

        IsBusy = true;
        BorrowMessage = null;
        try
        {
            var result = await _mediator.Send(new BorrowBookCommand(SelectedMember.Id, BookBarcodeInput.Trim()), ct);
            if (result.IsFailure)
            {
                BorrowMessage = "LỖI MƯỢN SÁCH: " + result.Error.Message;
                BorrowHasError = true;
                _audio.PlayError();
                return;
            }

            _audio.PlaySuccess();
            BorrowMessage = $"MƯỢN THÀNH CÔNG! Hạn trả: {result.Value.DueDateUtc.ToLocalTime():dd/MM/yyyy}";
            BorrowHasError = false;
            BookBarcodeInput = string.Empty;

            // Refresh member active loans
            await SearchMemberAsync(ct);
        }
        catch (Exception ex)
        {
            BorrowMessage = "Lỗi thực hiện mượn sách: " + ex.Message;
            BorrowHasError = true;
            _audio.PlayError();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ConfirmReturnAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ReturnBarcodeInput))
        {
            ReturnMessage = "Vui lòng quét hoặc nhập mã vạch cuốn sách cần trả.";
            ReturnHasError = true;
            _audio.PlayError();
            return;
        }

        IsBusy = true;
        ReturnMessage = null;
        LastReturnResult = null;
        try
        {
            var result = await _mediator.Send(new ReturnBookCommand(
                ReturnBarcodeInput.Trim(),
                SelectedCondition,
                ReturnNotes), ct);

            if (result.IsFailure)
            {
                ReturnMessage = "LỖI TRẢ SÁCH: " + result.Error.Message;
                ReturnHasError = true;
                _audio.PlayError();
                return;
            }

            LastReturnResult = result.Value;
            _audio.PlaySuccess();

            var msg = $"TRẢ SÁCH THÀNH CÔNG: [{result.Value.BookTitle}] của bạn đọc [{result.Value.MemberName}].";
            if (result.Value.OverdueDays > 0)
            {
                msg += $"\nQuá hạn: {result.Value.OverdueDays} ngày. ";
            }
            if (result.Value.FineAmount > 0)
            {
                msg += $"\nTiền phạt phát sinh: {result.Value.FineAmount:N0} VNĐ (Mã phạt đã tạo).";
            }
            if (result.Value.HasReservationNotified)
            {
                msg += "\n* Lưu ý: Đầu sách này có bạn đọc đang đặt trước, sách đã được chuyển sang trạng thái Giữ chỗ!";
            }

            ReturnMessage = msg;
            ReturnHasError = false;
            ReturnBarcodeInput = string.Empty;
            ReturnNotes = string.Empty;
        }
        catch (Exception ex)
        {
            ReturnMessage = "Lỗi xử lý trả sách: " + ex.Message;
            ReturnHasError = true;
            _audio.PlayError();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task RenewLoanAsync(LoanItemDto? item, CancellationToken ct = default)
    {
        if (item == null) return;

        var confirm = await _dialog.ShowConfirmationAsync($"Bạn có chắc muốn gia hạn 7 ngày cho cuốn sách '{item.BookTitle}'?");
        if (!confirm) return;

        IsBusy = true;
        try
        {
            var result = await _mediator.Send(new RenewLoanCommand(item.Id), ct);
            if (result.IsFailure)
            {
                await _dialog.ShowErrorAsync(result.Error.Message);
                _audio.PlayError();
                return;
            }

            _audio.PlaySuccess();
            await _dialog.ShowInfoAsync($"Gia hạn thành công! Hạn trả mới: {result.Value.DueDateUtc.ToLocalTime():dd/MM/yyyy}");
            if (SelectedMember != null)
            {
                await SearchMemberAsync(ct);
            }
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync("Lỗi gia hạn: " + ex.Message);
            _audio.PlayError();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task LoadOverdueLoansAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        try
        {
            var result = await _mediator.Send(new GetOverdueLoansQuery(), ct);
            OverdueLoans.Clear();
            if (result.IsSuccess)
            {
                foreach (var item in result.Value)
                    OverdueLoans.Add(item);
            }
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync("Lỗi tải danh sách quá hạn: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
