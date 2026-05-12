using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Office.Interop.Word;
using Task = System.Threading.Tasks.Task;

namespace ExamTickets.Core.Services;

public class DocumentService
{
    public async Task SaveAsDocxAsync(byte[] documentBytes, string filePath)
    {
        ArgumentNullException.ThrowIfNull(documentBytes);

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Путь к файлу не задан.", nameof(filePath));

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        await File.WriteAllBytesAsync(filePath, documentBytes).ConfigureAwait(false);
    }

    public async Task SaveAsPdfAsync(byte[] docxBytes, string filePath)
    {
        ArgumentNullException.ThrowIfNull(docxBytes);
        var tempDocxPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.docx");
        await File.WriteAllBytesAsync(tempDocxPath, docxBytes);

        var tcs = new TaskCompletionSource<bool>();

        // Создаем отдельный поток с состоянием STA (обязательно для COM/Word)
        var thread = new Thread(() =>
        {
            Microsoft.Office.Interop.Word.Application? wordApp = null;
            Microsoft.Office.Interop.Word.Document? doc = null;
            try
            {
                wordApp = new Microsoft.Office.Interop.Word.Application { Visible = false };
                doc = wordApp.Documents.Open(FileName: (object)tempDocxPath, ReadOnly: true, Visible: false);

                doc.ExportAsFixedFormat(
                    OutputFileName: filePath,
                    ExportFormat: WdExportFormat.wdExportFormatPDF,
                    OpenAfterExport: false,
                    OptimizeFor: WdExportOptimizeFor.wdExportOptimizeForPrint,
                    Range: WdExportRange.wdExportAllDocument);

                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            finally
            {
                if (doc is not null)
                {
                    try { doc.Close(SaveChanges: false); } catch { }
                    Marshal.ReleaseComObject(doc);
                }

                if (wordApp is not null)
                {
                    try { wordApp.Quit(SaveChanges: false); } catch { }
                    Marshal.ReleaseComObject(wordApp);
                }
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        try
        {
            await tcs.Task;
        }
        catch (COMException)
        {
            throw new InvalidOperationException("Microsoft Word не установлен или произошла ошибка COM.");
        }
        finally
        {
            try { if (File.Exists(tempDocxPath)) File.Delete(tempDocxPath); } catch { }
        }
    }

    public async Task PrintDocumentAsync(byte[] docxBytes)
    {
        ArgumentNullException.ThrowIfNull(docxBytes);
        var tempDocxPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.docx");

        try
        {
            await File.WriteAllBytesAsync(tempDocxPath, docxBytes).ConfigureAwait(false);

            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempDocxPath,
                Verb = "print",
                UseShellExecute = true,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(processInfo);
            if (process is null)
                throw new InvalidOperationException("Не удалось запустить печать.");
        }
        finally
        {

        }
    }

    public string GetTemplatePath()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "TicketTemplate.docx");
    }
}
