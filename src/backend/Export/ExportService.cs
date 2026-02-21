using ClosedXML.Excel;
using MeetupReservation.Api.Events;
using MeetupReservation.Api.Registrations;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PdfSharp.Pdf;

namespace MeetupReservation.Api.Export;

/// <summary>
/// WP-3.2: Экспорт списка участников в Excel и PDF.
/// Колонки: Фамилия, Имя, Отчество, Email, Телефон, Чек-ин (FR-05.4).
/// </summary>
public class ExportService
{
    private readonly RegistrationsService _registrations;
    private readonly EventsService _events;

    public ExportService(RegistrationsService registrations, EventsService events)
    {
        _registrations = registrations;
        _events = events;
    }

    public async Task<(byte[] content, string contentType, string fileName)?> ExportAsync(
        long eventId,
        long organizerUserId,
        string format)
    {
        var registrations = await _registrations.GetEventRegistrationsForOrganizerAsync(eventId, organizerUserId);
        if (registrations == null) return null;

        var evt = await _events.GetEventBasicInfoAsync(eventId);
        var eventTitle = evt?.title ?? "Событие";
        var safeTitle = string.Join("_", eventTitle.Split(Path.GetInvalidFileNameChars()));

        return format.ToLowerInvariant() switch
        {
            "xlsx" or "excel" => (await ExportToExcelAsync(registrations, eventTitle), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{safeTitle}.xlsx"),
            "pdf" => (ExportToPdf(registrations, eventTitle), "application/pdf", $"{safeTitle}.pdf"),
            _ => null
        };
    }

    private static async Task<byte[]> ExportToExcelAsync(EventRegistrationDto[] registrations, string eventTitle)
    {
        await Task.CompletedTask;
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Участники");

        sheet.Cell(1, 1).Value = "Фамилия";
        sheet.Cell(1, 2).Value = "Имя";
        sheet.Cell(1, 3).Value = "Отчество";
        sheet.Cell(1, 4).Value = "Email";
        sheet.Cell(1, 5).Value = "Телефон";
        sheet.Cell(1, 6).Value = "Чек-ин";
        sheet.Range(1, 1, 1, 6).Style.Font.Bold = true;

        for (var i = 0; i < registrations.Length; i++)
        {
            var r = registrations[i];
            var row = i + 2;
            sheet.Cell(row, 1).Value = r.LastName;
            sheet.Cell(row, 2).Value = r.FirstName;
            sheet.Cell(row, 3).Value = r.MiddleName ?? "";
            sheet.Cell(row, 4).Value = r.Email;
            sheet.Cell(row, 5).Value = r.Phone ?? "";
            sheet.Cell(row, 6).Value = r.Status == "checked_in" ? "Да" : "Нет";
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream, false);
        return stream.ToArray();
    }

    private static byte[] ExportToPdf(EventRegistrationDto[] registrations, string eventTitle)
    {
        var document = new Document();
        var section = document.AddSection();
        section.PageSetup.Orientation = Orientation.Landscape;

        var title = section.AddParagraph(eventTitle);
        title.Format.Font.Size = 14;
        title.Format.SpaceAfter = 10;

        var table = section.AddTable();
        table.Borders.Visible = true;

        table.AddColumn("3cm");
        table.AddColumn("2.5cm");
        table.AddColumn("2.5cm");
        table.AddColumn("4cm");
        table.AddColumn("3cm");
        table.AddColumn("2cm");

        var headerRow = table.AddRow();
        headerRow.HeadingFormat = true;
        headerRow.Cells[0].AddParagraph("Фамилия");
        headerRow.Cells[1].AddParagraph("Имя");
        headerRow.Cells[2].AddParagraph("Отчество");
        headerRow.Cells[3].AddParagraph("Email");
        headerRow.Cells[4].AddParagraph("Телефон");
        headerRow.Cells[5].AddParagraph("Чек-ин");

        foreach (var r in registrations)
        {
            var row = table.AddRow();
            row.Cells[0].AddParagraph(r.LastName);
            row.Cells[1].AddParagraph(r.FirstName);
            row.Cells[2].AddParagraph(r.MiddleName ?? "");
            row.Cells[3].AddParagraph(r.Email);
            row.Cells[4].AddParagraph(r.Phone ?? "");
            row.Cells[5].AddParagraph(r.Status == "checked_in" ? "Да" : "Нет");
        }

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();

        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, false);
        return stream.ToArray();
    }
}
