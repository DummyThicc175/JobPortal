using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using JobPortal.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace JobPortal.Services;

public sealed class AiInterviewContextService(PrivateFileStorage storage)
{
    public (string Text, bool Available) LayNoiDungCV(CV? cv)
    {
        if (cv == null) return ("", false);
        var text = new StringBuilder();
        text.AppendLine(cv.JobTitle).AppendLine(cv.Summary);
        foreach (var item in cv.Experiences.OrderBy(e => e.SortOrder).Take(5))
            text.AppendLine($"{item.Role} - {item.Company}: {item.Description}");
        foreach (var item in cv.Educations.OrderBy(e => e.SortOrder).Take(3)) text.AppendLine($"{item.Major} - {item.School}");
        foreach (var item in cv.Skills.OrderBy(e => e.SortOrder).Take(8)) text.AppendLine($"{item.SkillName}: {item.Description}");
        text.AppendLine(cv.Certifications);
        var result = RutGonVanBan(text.ToString(), 1800);
        if (result.Length >= 30) return (result, true);
        var path = storage.LayDuongDanPDF(cv.FileName);
        if (path == null || new FileInfo(path).Length > PrivateFileStorage.MaxPdfBytes) return ("", false);
        try
        {
            using var pdf = PdfDocument.Open(path);
            text.Clear();
            for (var page = 1; page <= Math.Min(pdf.NumberOfPages, 6) && text.Length < 5000; page++)
                text.AppendLine(ContentOrderTextExtractor.GetText(pdf.GetPage(page)));
            result = RutGonVanBan(text.ToString(), 1800);
            return result.Length >= 30 ? (result, true) : ("", false);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        { return ("", false); }
    }

    public static string RutGonVanBan(string? value, int limit)
    {
        var text = Regex.Replace(value ?? "", "<[^>]*>", " ", RegexOptions.None, TimeSpan.FromSeconds(1));
        text = WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"\s+", " ", RegexOptions.None, TimeSpan.FromSeconds(1)).Trim();
        return text.Length <= limit ? text : text[..limit];
    }
}
