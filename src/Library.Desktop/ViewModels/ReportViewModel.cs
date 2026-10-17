using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Library.Application.Common.Interfaces;
using Library.Application.Features.Reports.DTOs;
using Library.Application.Features.Reports.Queries;
using Library.Desktop.Services;
using MediatR;

namespace Library.Desktop.ViewModels;

public partial class ReportViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IPdfService _pdf;
    private readonly IExcelService _excel;
    private readonly IDialogService _dialog;
    private readonly IAudioService _audio;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _selectedReportType = "Danh sách sách quá hạn và tiền phạt dự kiến";
    [ObservableProperty] private DateTime _startDate = DateTime.Now.AddMonths(-1);
    [ObservableProperty] private DateTime _endDate = DateTime.Now;

    public ObservableCollection<OverdueLoanReportDto> OverdueReportItems { get; } = new();

    public ReportViewModel(
        IMediator mediator,
        IPdfService pdf,
        IExcelService excel,
        IDialogService dialog,
        IAudioService audio)
    {
        _mediator = mediator;
        _pdf = pdf;
        _excel = excel;
        _dialog = dialog;
        _audio = audio;
    }

    [RelayCommand]
    public async Task GenerateReportAsync(CancellationToken ct = default)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var result = await _mediator.Send(new GetOverdueReportQuery(), ct);
            if (result.IsSuccess)
            {
                OverdueReportItems.Clear();
                foreach (var item in result.Value)
                    OverdueReportItems.Add(item);

                _audio.PlaySuccess();
            }
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync("Lỗi tổng hợp báo cáo: " + ex.Message);
            _audio.PlayError();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ExportPdfAsync()
    {
        if (OverdueReportItems.Count == 0)
        {
            await _dialog.ShowWarningAsync("Không có dữ liệu báo cáo để xuất.");
            return;
        }

        try
        {
            var headers = new[] { "Bạn đọc", "Mã thẻ", "Lớp/Tổ", "Tên sách", "Mã vạch", "Hạn trả", "Số ngày trễ", "Tiền phạt dự kiến" };
            var pdfBytes = _pdf.GenerateReportPdf(
                "BÁO CÁO DANH SÁCH MƯỢN SÁCH QUÁ HẠN",
                OverdueReportItems,
                headers,
                item => new[]
                {
                    item.MemberName,
                    item.CardNumber,
                    item.ClassOrDepartment,
                    item.BookTitle,
                    item.Barcode,
                    item.DueDateUtc.ToLocalTime().ToString("dd/MM/yyyy"),
                    item.OverdueDays.ToString(),
                    $"{item.EstimatedFine:N0} đ"
                });

            var desktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var filePath = Path.Combine(desktopFolder, $"BaoCao_QuaHan_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await File.WriteAllBytesAsync(filePath, pdfBytes);

            _audio.PlaySuccess();
            await _dialog.ShowInfoAsync($"Xuất file PDF thành công!\nTập tin đã lưu tại:\n{filePath}");
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync("Lỗi xuất PDF: " + ex.Message);
            _audio.PlayError();
        }
    }

    [RelayCommand]
    public async Task ExportExcelAsync()
    {
        if (OverdueReportItems.Count == 0)
        {
            await _dialog.ShowWarningAsync("Không có dữ liệu báo cáo để xuất.");
            return;
        }

        try
        {
            var excelBytes = _excel.ExportToExcel(OverdueReportItems, "BaoCaoQuaHan");
            var desktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var filePath = Path.Combine(desktopFolder, $"BaoCao_QuaHan_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            await File.WriteAllBytesAsync(filePath, excelBytes);

            _audio.PlaySuccess();
            await _dialog.ShowInfoAsync($"Xuất file Excel thành công!\nTập tin đã lưu tại:\n{filePath}");
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync("Lỗi xuất Excel: " + ex.Message);
            _audio.PlayError();
        }
    }
}
