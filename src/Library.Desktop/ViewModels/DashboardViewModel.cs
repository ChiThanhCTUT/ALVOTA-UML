using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Library.Application.Features.Reports.DTOs;
using Library.Application.Features.Reports.Queries;
using Library.Desktop.Services;
using MediatR;

namespace Library.Desktop.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IDialogService _dialog;

    [ObservableProperty] private int _totalBooks;
    [ObservableProperty] private int _totalCopies;
    [ObservableProperty] private int _availableCopies;
    [ObservableProperty] private int _activeLoans;
    [ObservableProperty] private int _overdueLoans;
    [ObservableProperty] private decimal _monthlyRevenue;
    [ObservableProperty] private int _totalActiveMembers;
    [ObservableProperty] private bool _isBusy;

    public ObservableCollection<OverdueLoanReportDto> OverdueList { get; } = new();

    public DashboardViewModel(IMediator mediator, IDialogService dialog)
    {
        _mediator = mediator;
        _dialog = dialog;
    }

    [RelayCommand]
    public async Task LoadDataAsync(CancellationToken ct = default)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var statsResult = await _mediator.Send(new GetDashboardStatisticsQuery(), ct);
            if (statsResult.IsSuccess)
            {
                var s = statsResult.Value;
                TotalBooks = s.TotalBooks;
                TotalCopies = s.TotalCopies;
                AvailableCopies = s.AvailableCopies;
                ActiveLoans = s.BorrowedCopies;
                OverdueLoans = s.OverdueCount;
                MonthlyRevenue = s.MonthlyFineRevenue;
                TotalActiveMembers = s.TotalActiveMembers;
            }

            var overdueResult = await _mediator.Send(new GetOverdueReportQuery(), ct);
            if (overdueResult.IsSuccess)
            {
                OverdueList.Clear();
                foreach (var item in overdueResult.Value.Take(10))
                {
                    OverdueList.Add(item);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await _dialog.ShowErrorAsync("Lỗi tải thông tin tổng quan: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
