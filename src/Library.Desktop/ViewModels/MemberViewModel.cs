using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Library.Application.Common.Models;
using Library.Application.Features.Members.Commands;
using Library.Application.Features.Members.DTOs;
using Library.Application.Features.Members.Queries;
using Library.Desktop.Services;
using Library.Domain.Enums;
using MediatR;

namespace Library.Desktop.ViewModels;

public partial class MemberViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IDialogService _dialog;
    private readonly IAudioService _audio;

    [ObservableProperty] private string _searchTerm = string.Empty;
    [ObservableProperty] private int _pageIndex = 1;
    [ObservableProperty] private int _pageSize = 10;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private MemberType? _selectedTypeFilter;
    [ObservableProperty] private CardStatus? _selectedStatusFilter;

    // Form thêm bạn đọc mới
    [ObservableProperty] private bool _isAddingMember;
    [ObservableProperty] private string _newCardNumber = string.Empty;
    [ObservableProperty] private string _newFullName = string.Empty;
    [ObservableProperty] private DateTime _newDob = new(2008, 1, 1);
    [ObservableProperty] private MemberType _newType = MemberType.Student;
    [ObservableProperty] private string _newClass = "10A1";
    [ObservableProperty] private string? _newPhone;
    [ObservableProperty] private string? _newEmail;

    public ObservableCollection<MemberDto> Members { get; } = new();

    public MemberViewModel(IMediator mediator, IDialogService dialog, IAudioService audio)
    {
        _mediator = mediator;
        _dialog = dialog;
        _audio = audio;
    }

    [RelayCommand]
    public async Task LoadMembersAsync(CancellationToken ct = default)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var pagedRequest = new PagedRequest(PageIndex, PageSize, SearchTerm);
            var result = await _mediator.Send(new GetMembersQuery(pagedRequest, SelectedTypeFilter, SelectedStatusFilter), ct);
            if (result.IsSuccess)
            {
                Members.Clear();
                foreach (var m in result.Value.Items)
                    Members.Add(m);

                TotalCount = result.Value.TotalCount;
                TotalPages = result.Value.TotalPages == 0 ? 1 : result.Value.TotalPages;
            }
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync("Lỗi tải danh sách bạn đọc: " + ex.Message);
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
        await LoadMembersAsync(ct);
    }

    [RelayCommand]
    public async Task NextPageAsync(CancellationToken ct = default)
    {
        if (PageIndex < TotalPages)
        {
            PageIndex++;
            await LoadMembersAsync(ct);
        }
    }

    [RelayCommand]
    public async Task PreviousPageAsync(CancellationToken ct = default)
    {
        if (PageIndex > 1)
        {
            PageIndex--;
            await LoadMembersAsync(ct);
        }
    }

    [RelayCommand]
    public void ToggleAddMember()
    {
        IsAddingMember = !IsAddingMember;
        if (IsAddingMember)
        {
            NewCardNumber = $"HS-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        }
    }

    [RelayCommand]
    public async Task SaveNewMemberAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(NewCardNumber) || string.IsNullOrWhiteSpace(NewFullName) || string.IsNullOrWhiteSpace(NewClass))
        {
            await _dialog.ShowWarningAsync("Vui lòng điền đầy đủ Mã thẻ, Họ tên và Lớp/Tổ.");
            return;
        }

        IsBusy = true;
        try
        {
            var command = new CreateMemberCommand(
                NewCardNumber.Trim(),
                NewFullName.Trim(),
                NewDob,
                NewType,
                NewClass.Trim(),
                NewPhone,
                NewEmail,
                null,
                NewType == MemberType.Teacher ? 24 : 12);

            var result = await _mediator.Send(command, ct);
            if (result.IsFailure)
            {
                await _dialog.ShowErrorAsync(result.Error.Message);
                _audio.PlayError();
                return;
            }

            _audio.PlaySuccess();
            await _dialog.ShowInfoAsync($"Tạo thẻ bạn đọc '{NewFullName}' thành công!");

            IsAddingMember = false;
            NewFullName = string.Empty;
            NewPhone = string.Empty;
            NewEmail = string.Empty;

            IsBusy = false;
            await LoadMembersAsync(ct);
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync("Lỗi tạo bạn đọc: " + ex.Message);
            _audio.PlayError();
        }
        finally
        {
            IsBusy = false;
        }
    }
}
