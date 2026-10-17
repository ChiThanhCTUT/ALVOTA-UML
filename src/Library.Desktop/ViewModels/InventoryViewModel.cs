using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Library.Application.Common.Models;
using Library.Application.Features.Inventory.Commands;
using Library.Application.Features.Inventory.DTOs;
using Library.Application.Features.Inventory.Queries;
using Library.Desktop.Services;
using Library.Domain.Enums;
using MediatR;

namespace Library.Desktop.ViewModels;

public partial class InventoryViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IDialogService _dialog;
    private readonly IAudioService _audio;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private InventorySessionDto? _selectedSession;
    [ObservableProperty] private string _newSessionName = $"Kiểm kê kho {DateTime.Now:MM/yyyy}";
    [ObservableProperty] private string _newShelfLocation = "Tất cả các kệ";
    [ObservableProperty] private string _scanBarcodeInput = string.Empty;
    [ObservableProperty] private string? _scanMessage;

    public ObservableCollection<InventorySessionDto> Sessions { get; } = new();

    public InventoryViewModel(IMediator mediator, IDialogService dialog, IAudioService audio)
    {
        _mediator = mediator;
        _dialog = dialog;
        _audio = audio;
    }

    [RelayCommand]
    public async Task LoadSessionsAsync(CancellationToken ct = default)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var result = await _mediator.Send(new GetInventorySessionsQuery(new PagedRequest(1, 50)), ct);
            if (result.IsSuccess)
            {
                Sessions.Clear();
                foreach (var s in result.Value.Items)
                    Sessions.Add(s);

                if (SelectedSession == null && Sessions.Count > 0)
                    SelectedSession = Sessions[0];
            }
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync("Lỗi tải các đợt kiểm kê: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task CreateSessionAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(NewSessionName))
        {
            await _dialog.ShowWarningAsync("Vui lòng nhập tên đợt kiểm kê.");
            return;
        }

        IsBusy = true;
        try
        {
            var command = new CreateInventorySessionCommand(NewSessionName, NewShelfLocation);
            var result = await _mediator.Send(command, ct);
            if (result.IsSuccess)
            {
                _audio.PlaySuccess();
                await _dialog.ShowInfoAsync($"Tạo đợt kiểm kê '{result.Value.Name}' thành công!");
                await LoadSessionsAsync(ct);
                SelectedSession = Sessions.FirstOrDefault(s => s.Id == result.Value.Id);
            }
            else
            {
                await _dialog.ShowErrorAsync(result.Error.Message);
            }
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync("Lỗi tạo kiểm kê: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ScanBarcodeAsync(CancellationToken ct = default)
    {
        if (SelectedSession == null)
        {
            ScanMessage = "Vui lòng chọn đợt kiểm kê đang diễn ra.";
            _audio.PlayError();
            return;
        }

        if (SelectedSession.Status != InventorySessionStatus.InProgress)
        {
            ScanMessage = "Đợt kiểm kê này đã hoàn tất, không thể quét thêm.";
            _audio.PlayWarning();
            return;
        }

        if (string.IsNullOrWhiteSpace(ScanBarcodeInput)) return;

        IsBusy = true;
        ScanMessage = null;
        try
        {
            var command = new ScanInventoryBarcodeCommand(SelectedSession.Id, ScanBarcodeInput.Trim());
            var result = await _mediator.Send(command, ct);
            if (result.IsSuccess)
            {
                _audio.PlaySuccess();
                ScanMessage = $"ĐÃ KIỂM: Bản sao mã [{result.Value.Barcode}] - Sách: {result.Value.BookTitle}";
                ScanBarcodeInput = string.Empty;
                await LoadSessionsAsync(ct);
            }
            else
            {
                _audio.PlayError();
                ScanMessage = "Lỗi quét: " + result.Error.Message;
            }
        }
        catch (Exception ex)
        {
            ScanMessage = "Lỗi: " + ex.Message;
            _audio.PlayError();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task CompleteSessionAsync(CancellationToken ct = default)
    {
        if (SelectedSession == null) return;

        var confirm = await _dialog.ShowConfirmationAsync(
            $"Bạn có chắc chắn muốn chốt và hoàn tất đợt kiểm kê '{SelectedSession.Name}'?");
        if (!confirm) return;

        IsBusy = true;
        try
        {
            var result = await _mediator.Send(new CompleteInventorySessionCommand(SelectedSession.Id), ct);
            if (result.IsSuccess)
            {
                _audio.PlaySuccess();
                await _dialog.ShowInfoAsync($"Đợt kiểm kê '{result.Value.Name}' đã hoàn tất và lưu trữ kết quả!");
                await LoadSessionsAsync(ct);
            }
            else
            {
                await _dialog.ShowErrorAsync(result.Error.Message);
            }
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync("Lỗi chốt kiểm kê: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
