using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using libraryMVC.DTOs.Reports;
using libraryMVC.Interfaces;

namespace libraryMVC.Services.Reports;

public sealed class ExcelReportGenerator : IExcelReportGenerator
{
    private const int HeaderRow = 4;

    public byte[] Generate(TabularReport report, string worksheetName)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(worksheetName);
        ValidateRows(report);

        using var workbook = new XLWorkbook();
        IXLWorksheet worksheet = workbook.Worksheets.Add(worksheetName);
        worksheet.Cell(1, 1).Value = "libraryMVC";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;
        worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.Black;
        worksheet.Cell(2, 1).Value = report.Title;
        worksheet.Cell(2, 1).Style.Font.Bold = true;
        worksheet.Cell(2, 1).Style.Font.FontColor = XLColor.Black;
        worksheet.Cell(3, 1).Value = "Generated";
        worksheet.Cell(3, 1).Style.Font.FontColor = XLColor.Gray;
        worksheet.Cell(3, 2).Value = report.GeneratedAt.LocalDateTime;
        worksheet.Cell(3, 2).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
        worksheet.Cell(3, 2).Style.Font.FontColor = XLColor.Gray;

        for (int columnIndex = 0; columnIndex < report.Columns.Count; columnIndex++)
        {
            worksheet.Cell(HeaderRow, columnIndex + 1).Value = report.Columns[columnIndex].Header;
        }

        for (int rowIndex = 0; rowIndex < report.Rows.Count; rowIndex++)
        {
            for (int columnIndex = 0; columnIndex < report.Columns.Count; columnIndex++)
            {
                worksheet.Cell(HeaderRow + rowIndex + 1, columnIndex + 1).Value =
                    report.Rows[rowIndex][columnIndex];
            }
        }

        int lastColumn = report.Columns.Count;
        int lastRow = HeaderRow + report.Rows.Count;
        IXLRange tableRange = worksheet.Range(HeaderRow, 1, lastRow, lastColumn);
        IXLTable table = tableRange.CreateTable();
        table.Theme = XLTableTheme.None;
        table.ShowAutoFilter = true;
        table.ShowRowStripes = false;
        tableRange.Style.Font.FontColor = XLColor.Black;
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.OutsideBorderColor = XLColor.Gray;
        tableRange.Style.Border.InsideBorderColor = XLColor.Gray;
        IXLRange headerRange = worksheet.Range(HeaderRow, 1, HeaderRow, lastColumn);
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#333333");
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Font.Bold = true;
        for (int row = HeaderRow + 1; row <= lastRow; row++)
        {
            worksheet.Range(row, 1, row, lastColumn).Style.Fill.BackgroundColor = row % 2 == 0
                ? XLColor.White
                : XLColor.FromHtml("#F2F2F2");
        }

        worksheet.SheetView.FreezeRows(HeaderRow);
        worksheet.Columns(1, lastColumn).AdjustToContents();
        worksheet.Columns(1, lastColumn).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        worksheet.Columns(1, lastColumn).Style.Alignment.WrapText = true;
        for (int columnIndex = 1; columnIndex <= lastColumn; columnIndex++)
        {
            worksheet.Column(columnIndex).Width = Math.Min(worksheet.Column(columnIndex).Width, 60);
        }

        worksheet.Rows(HeaderRow, lastRow).AdjustToContents();
        foreach (ReportEmbeddedImage image in report.Images ?? [])
        {
            int worksheetRow = HeaderRow + image.RowIndex + 1;
            int worksheetColumn = image.ColumnIndex + 1;
            worksheet.Column(worksheetColumn).Width = 14;
            worksheet.Row(worksheetRow).Height = Math.Max(worksheet.Row(worksheetRow).Height, 48);
            using var imageStream = new MemoryStream(image.Content, writable: false);
            worksheet.AddPicture(imageStream, GetPictureFormat(image.ImageExtension))
                .MoveTo(worksheet.Cell(worksheetRow, worksheetColumn))
                .WithSize(80, 48);
        }

        worksheet.PageSetup.PagesWide = 1;
        worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        worksheet.PageSetup.SetRowsToRepeatAtTop(1, HeaderRow);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void ValidateRows(TabularReport report)
    {
        if (report.Columns.Count == 0)
        {
            throw new ArgumentException("A report must define at least one column.", nameof(report));
        }

        if (report.Rows.Any(row => row.Count != report.Columns.Count))
        {
            throw new ArgumentException("Each report row must contain one value per column.", nameof(report));
        }

        if (report.Images?.Any(image =>
                image.RowIndex < 0 ||
                image.RowIndex >= report.Rows.Count ||
                image.ColumnIndex < 0 ||
                image.ColumnIndex >= report.Columns.Count ||
                image.Content.Length == 0) == true)
        {
            throw new ArgumentException("Embedded images must reference a valid non-empty report cell.", nameof(report));
        }
    }

    private static XLPictureFormat GetPictureFormat(string imageExtension) =>
        imageExtension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => XLPictureFormat.Jpeg,
            ".png" => XLPictureFormat.Png,
            ".webp" => XLPictureFormat.Webp,
            _ => throw new InvalidDataException(
                $"The image format '{imageExtension}' is not supported in Excel reports.")
        };
}
