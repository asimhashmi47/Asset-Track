using System.IO;
using System.Windows;
using ClosedXML.Excel;
using Microsoft.Win32;

namespace AssetTrack.App.Infrastructure;

/// <summary>
/// Exports whatever a table is currently showing — same search/filter/sort applied, all
/// matching rows (not just the current page) — to a real .xlsx workbook the user saves.
/// </summary>
public static class ExcelExporter
{
    public static void Export(string suggestedFileName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
    {
        var dialog = new SaveFileDialog
        {
            FileName = suggestedFileName,
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            DefaultExt = ".xlsx"
        };
        if (dialog.ShowDialog() != true) return;

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Export");

        for (var col = 0; col < headers.Count; col++)
        {
            var cell = sheet.Cell(1, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
        }

        var rowIndex = 2;
        foreach (var row in rows)
        {
            for (var col = 0; col < row.Count; col++)
                sheet.Cell(rowIndex, col + 1).Value = row[col];
            rowIndex++;
        }

        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(1);

        try
        {
            workbook.SaveAs(dialog.FileName);
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Could not save the file — it may be open in another program.\n\n{ex.Message}",
                "Export failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
