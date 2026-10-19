using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Library.Application.Common.Models;
using Library.Application.Features.Fines.Commands;
using Library.Application.Features.Fines.DTOs;
using Library.Application.Features.Fines.Queries;
using Library.Desktop.Services;
using Library.Domain.Enums;
using MediatR;

namespace Library.Desktop.ViewModels;

public partial class FineViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IDialogService _dialog;
    private readonly IAudioService _audio;

    [ObservableProperty] private string _searchTerm = string.Empty;
    [ObservableProperty] private FineStatus? _selectedStatusFilter = FineStatus.Pending;
    [ObservableProperty] private int _pageIndex = 1;
    [ObservableProperty] private int _pageSize = 10;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private FineDto? _selectedFine;

    public ObservableCollection<FineDto> Fines { get; } = new();

    public FineViewModel(IMediator mediator, IDialogService dialog, IAudioService audio)
    {
        _mediator = mediator;
        _dialog = dialog;
        _audio = audio;
    }

    [RelayCommand]
    public async Task LoadFinesAsync(CancellationToken ct = default)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var pagedRequest = new PagedRequest(PageIndex, PageSize, SearchTerm);
            var result = await _mediator.Send(new GetFinesQuery(pagedRequest, SelectedStatusFilter), ct);
            if (result.IsSuccess)
            {
                Fines.Clear();
                foreach (var f in result.Value.Items)
                    Fines.Add(f);

                TotalCount = result.Value.TotalCount;
                TotalPages = result.Value.TotalPages == 0 ? 1 : result.Value.TotalPages;
            }
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync("Lỗi tải danh sách phạt: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SearchAsync(CancellationToken ct = default)
    {
        PageIndex = 1;
        await LoadFinesAsync(ct);
    }

    [RelayCommand]
    public async Task NextPageAsync(CancellationToken ct = default)
    {
        if (PageIndex < TotalPages)
        {
            PageIndex++;
            await LoadFinesAsync(ct);
        }
    }

    [RelayCommand]
    public async Task PreviousPageAsync(CancellationToken ct = default)
    {
        if (PageIndex > 1)
        {
            PageIndex--;
            await LoadFinesAsync(ct);
        }
    }

    [RelayCommand]
    public async Task PayFineAsync(FineDto? fine, CancellationToken ct = default)
    {
        if (fine == null) return;

        if (fine.RemainingAmount <= 0)
        {
            await _dialog.ShowWarningAsync("Phiếu phạt này đã được thanh toán đầy đủ.");
            return;
        }

        var confirm = await _dialog.ShowConfirmationAsync(
            $"Xác nhận thu {fine.RemainingAmount:N0} đ tiền mặt cho bạn đọc '{fine.MemberName}'?");
        if (!confirm) return;

        IsBusy = true;
        try
        {
            var result = await _mediator.Send(new PayFineCommand(fine.Id, fine.RemainingAmount, "Thanh toán tại quầy"), ct);
            if (result.IsFailure)
            {
                await _dialog.ShowErrorAsync(result.Error.Message);
                _audio.PlayError();
                return;
            }

            _audio.PlaySuccess();
            await _dialog.ShowInfoAsync(
                $"Đã thu phạt thành công!\nMã biên lai: {result.Value.ReceiptNumber}\nSố tiền: {result.Value.Amount:N0} đ");

            IsBusy = false;
            await LoadFinesAsync(ct);
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync("Lỗi thanh toán phạt: " + ex.Message);
            _audio.PlayError();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task WaiveFineAsync(FineDto? fine, CancellationToken ct = default)
    {
        if (fine == null) return;

        var confirm = await _dialog.ShowConfirmationAsync(
            $"Bạn có chắc chắn muốn MIỄN PHẠT số tiền {fine.RemainingAmount:N0} đ cho bạn đọc '{fine.MemberName}'?\nThao tác này yêu cầu quyền Thủ thư trở lên và sẽ được ghi vào Nhật ký kiểm toán.");
        if (!confirm) return;

        IsBusy = true;
        try
        {
            var result = await _mediator.Send(new WaiveFineCommand(fine.Id, "Miễn phạt do học sinh có hoàn cảnh đặc biệt / sách hư hỏng khách quan"), ct);
            if (result.IsFailure)
            {
                await _dialog.ShowErrorAsync(result.Error.Message);
                _audio.PlayError();
                return;
            }

            _audio.PlaySuccess();
            await _dialog.ShowInfoAsync("Đã miễn giảm phạt thành công và ghi log kiểm toán!");
            IsBusy = false;
            await LoadFinesAsync(ct);
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync("Lỗi miễn phạt: " + ex.Message);
            _audio.PlayError();
        }
        finally
        {
            IsBusy = false;
        }
    }
}
