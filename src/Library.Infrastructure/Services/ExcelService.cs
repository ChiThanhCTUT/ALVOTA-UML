using System.Reflection;
using ClosedXML.Excel;
using Library.Application.Common.Interfaces;
using Library.Domain.Common;

namespace Library.Infrastructure.Services;

// CAT-06, MEM-04, RPT-06: Xuất/Nhập ClosedXML, báo lỗi từng dòng không dừng file
public sealed class ExcelService : IExcelService
{
    public byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(string.IsNullOrWhiteSpace(sheetName) ? "Sheet1" : sheetName);

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Header row
        for (int i = 0; i < properties.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = properties[i].Name;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data rows
        var row = 2;
        foreach (var item in data)
        {
            for (int col = 0; col < properties.Length; col++)
            {
                var val = properties[col].GetValue(item);
                worksheet.Cell(row, col + 1).Value = val?.ToString() ?? string.Empty;
            }
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public (List<T> SuccessRows, List<(int RowNumber, string ErrorMessage)> Errors) ImportFromExcel<T>(
        Stream stream, Func<IReadOnlyDictionary<string, string>, Result<T>> rowParser)
    {
        var successRows = new List<T>();
        var errors = new List<(int RowNumber, string ErrorMessage)>();

        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null) return (successRows, errors);

        var headerRow = worksheet.Row(1);
        var colMap = new Dictionary<int, string>();
        foreach (var cell in headerRow.CellsUsed())
        {
            colMap[cell.Address.ColumnNumber] = cell.GetString().Trim();
        }

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        for (int r = 2; r <= lastRow; r++)
        {
            var row = worksheet.Row(r);
            if (row.IsEmpty()) continue;

            var rowDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var col in colMap)
            {
                rowDict[col.Value] = row.Cell(col.Key).GetString().Trim();
            }

            try
            {
                var result = rowParser(rowDict);
                if (result.IsSuccess)
                {
                    successRows.Add(result.Value);
                }
                else
                {
                    errors.Add((r, result.Error.Message));
                }
            }
            catch (Exception ex)
            {
                errors.Add((r, ex.Message));
            }
        }

        return (successRows, errors);
    }
}
