using Library.Application.Common.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Library.Infrastructure.Services;

// FIN-03, CIR-08, RPT-06: Xuất hóa đơn, biên lai và báo cáo PDF bằng QuestPDF
public sealed class PdfService : IPdfService
{
    static PdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateReceiptPdf(
        string title,
        string receiptNumber,
        string readerName,
        string cardNumber,
        decimal amount,
        string details,
        DateTime date)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text(title).FontSize(16).Bold().AlignCenter();
                    col.Item().Text($"Số phiếu: {receiptNumber}").FontSize(10).AlignCenter();
                    col.Item().LineHorizontal(1);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(6);
                    col.Item().Text($"Họ và tên bạn đọc: {readerName} (Mã thẻ: {cardNumber})");
                    col.Item().Text($"Thời gian: {date:dd/MM/yyyy HH:mm}");
                    col.Item().Text($"Nội dung: {details}");
                    col.Item().Text($"Số tiền: {amount:N0} VNĐ").FontSize(13).Bold();
                });

                page.Footer().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Người nộp tiền").AlignCenter();
                        c.Item().Text("(Ký và ghi rõ họ tên)").FontSize(9).Italic().AlignCenter();
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Thủ thư / Thu ngân").AlignCenter();
                        c.Item().Text("(Ký và ghi rõ họ tên)").FontSize(9).Italic().AlignCenter();
                    });
                });
            });
        });

        return doc.GeneratePdf();
    }

    public byte[] GenerateReportPdf<T>(
        string title,
        IEnumerable<T> rows,
        string[] headers,
        Func<T, string[]> rowMapper)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text(title).FontSize(16).Bold().AlignCenter();
                    col.Item().Text($"Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9).AlignCenter();
                    col.Item().LineHorizontal(1);
                });

                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        foreach (var _ in headers)
                        {
                            columns.RelativeColumn();
                        }
                    });

                    table.Header(header =>
                    {
                        foreach (var h in headers)
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(h).Bold();
                        }
                    });

                    foreach (var item in rows)
                    {
                        var values = rowMapper(item);
                        foreach (var v in values)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(v);
                        }
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return doc.GeneratePdf();
    }
}
