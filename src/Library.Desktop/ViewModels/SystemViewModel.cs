using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Library.Application.Common.Models;
using Library.Application.Features.Auth.Commands;
using Library.Application.Features.Auth.DTOs;
using Library.Application.Features.Auth.Queries;
using Library.Application.Features.System.Commands;
using Library.Application.Features.System.DTOs;
using Library.Application.Features.System.Queries;
using Library.Desktop.Services;
using MediatR;

namespace Library.Desktop.ViewModels;

public partial class SystemViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IDialogService _dialog;
    private readonly IAudioService _audio;

    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private bool _isBusy;

    // --- TAB 1: CẤU HÌNH (SETTINGS - SYS-01) ---
    public ObservableCollection<SettingDto> SettingsList { get; } = new();

    // --- TAB 2: QUẢN LÝ NGƯỜI DÙNG (USERS - AUTH-05) ---
    public ObservableCollection<UserDto> UsersList { get; } = new();
    [ObservableProperty] private string _newUsername = string.Empty;
    [ObservableProperty] private string _newFullName = string.Empty;
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _newRole = "Librarian";

    // --- TAB 3: SAO LƯU & PHỤC HỒI (BACKUP - SYS-02) ---
    [ObservableProperty] private string? _lastBackupPath;

    // --- TAB 4: NHẬT KÝ KIỂM TOÁN (AUDIT LOGS - SYS-03, SYS-04) ---
    public ObservableCollection<AuditLogDto> AuditLogsList { get; } = new();

    public SystemViewModel(IMediator mediator, IDialogService dialog, IAudioService audio)
    {
        _mediator = mediator;
        _dialog = dialog;
        _audio = audio;
    }

    [RelayCommand]
    public async Task LoadSettingsAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        try
        {
            var result = await _mediator.Send(new GetSettingsQuery(), ct);
            if (result.IsSuccess)
            {
                SettingsList.Clear();
                foreach (var s in result.Value)
                    SettingsList.Add(s);
            }
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync("Lỗi tải cấu hình: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task UpdateSettingAsync(SettingDto? setting, CancellationToken ct = default)
    {
        if (setting == null) return;

        IsBusy = true;
        try
        {
            var command = new UpdateSettingCommand(setting.Key, setting.Value, setting.Category, setting.Description);
            var result = await _mediator.Send(command, ct);
            if (result.IsSuccess)
            {
                _audio.PlaySuccess();
                await _dialog.ShowInfoAsync($"Cập nhật thiết lập '{setting.Key}' thành công!");
            }
            else
            {
                await _dialog.ShowErrorAsync(result.Error.Message);
            }
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync("Lỗi lưu thiết lập: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task LoadUsersAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        try
        {
            var result = await _mediator.Send(new GetUsersQuery(), ct);
            if (result.IsSuccess)
            {
                UsersList.Clear();
                foreach (var u in result.Value)
                    UsersList.Add(u);
            }
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync("Lỗi tải danh sách người dùng: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task CreateUserAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(NewUsername) || string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(NewFullName))
        {
            await _dialog.ShowWarningAsync("Vui lòng điền đầy đủ Tên đăng nhập, Mật khẩu và Họ tên.");
            return;
        }

        IsBusy = true;
        try
        {
            var command = new CreateUserCommand(NewUsername, NewPassword, NewFullName, null, NewRole);
            var result = await _mediator.Send(command, ct);
            if (result.IsSuccess)
            {
                _audio.PlaySuccess();
                await _dialog.ShowInfoAsync($"Tạo tài khoản '{NewUsername}' thành công!");
                NewUsername = string.Empty;
                NewPassword = string.Empty;
                NewFullName = string.Empty;
                await LoadUsersAsync(ct);
            }
            else
            {
                await _dialog.ShowErrorAsync(result.Error.Message);
            }
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync("Lỗi tạo người dùng: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ResetPasswordAsync(UserDto? user, CancellationToken ct = default)
    {
        if (user == null) return;

        var confirmed = await _dialog.ShowConfirmationAsync(
            $"Đặt lại mật khẩu của tài khoản '{user.Username}' về mặc định 'Library@123' và mở khóa tài khoản?",
            "Xác nhận đặt lại mật khẩu");
        if (!confirmed) return;

        IsBusy = true;
        try
        {
            var result = await _mediator.Send(new ResetPasswordCommand(user.Id, "Library@123"), ct);
            if (result.IsSuccess)
            {
                _audio.PlaySuccess();
                await _dialog.ShowInfoAsync($"Đã đặt lại mật khẩu cho '{user.Username}' thành 'Library@123'!");
                await LoadUsersAsync(ct);
            }
            else
            {
                _audio.PlayError();
                await _dialog.ShowErrorAsync(result.Error.Message);
            }
        }
        catch (Exception ex)
        {
            _audio.PlayError();
            await _dialog.ShowErrorAsync("Lỗi đặt lại mật khẩu: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task CreateBackupAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        try
        {
            var backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            var result = await _mediator.Send(new CreateBackupCommand(backupDir), ct);
            if (result.IsSuccess)
            {
                LastBackupPath = result.Value;
                _audio.PlaySuccess();
                await _dialog.ShowInfoAsync($"Sao lưu dữ liệu CSDL thành công!\nFile sao lưu: {result.Value}");
            }
            else
            {
                await _dialog.ShowErrorAsync(result.Error.Message);
            }
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync("Lỗi sao lưu: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task RestoreBackupAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(LastBackupPath) || !File.Exists(LastBackupPath))
        {
            var backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            if (Directory.Exists(backupDir))
            {
                var latest = new DirectoryInfo(backupDir)
                    .GetFiles("library_backup_*.db")
                    .OrderByDescending(f => f.CreationTimeUtc)
                    .FirstOrDefault();
                if (latest != null)
                {
                    LastBackupPath = latest.FullName;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(LastBackupPath) || !File.Exists(LastBackupPath))
        {
            await _dialog.ShowWarningAsync("Chưa tìm thấy file sao lưu nào trong thư mục Backups.");
            return;
        }

        var confirmed = await _dialog.ShowConfirmationAsync(
            $"Bạn có chắc chắn muốn khôi phục CSDL từ bản sao lưu:\n{LastBackupPath}?",
            "Xác nhận phục hồi dữ liệu");
        if (!confirmed) return;

        IsBusy = true;
        try
        {
            var result = await _mediator.Send(new RestoreBackupCommand(LastBackupPath), ct);
            if (result.IsSuccess)
            {
                _audio.PlaySuccess();
                await _dialog.ShowInfoAsync("Khôi phục cơ sở dữ liệu thành công!");
            }
            else
            {
                _audio.PlayError();
                await _dialog.ShowErrorAsync(result.Error.Message);
            }
        }
        catch (Exception ex)
        {
            _audio.PlayError();
            await _dialog.ShowErrorAsync("Lỗi khôi phục CSDL: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task LoadAuditLogsAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        try
        {
            var result = await _mediator.Send(new GetAuditLogsQuery(new PagedRequest(1, 100)), ct);
            if (result.IsSuccess)
            {
                AuditLogsList.Clear();
                foreach (var log in result.Value.Items)
                    AuditLogsList.Add(log);
            }
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync("Lỗi tải audit log: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
