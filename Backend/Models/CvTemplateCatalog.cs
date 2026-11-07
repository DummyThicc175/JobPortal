using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace JobPortal.Models;

public sealed class CvBlockLayout
{
    [Required, StringLength(30)] public string Key { get; set; } = "";
    [Required, StringLength(10)] public string Zone { get; set; } = "main";
}

public sealed record CvTemplateStyle(int Id, string Name, string Category, string Description,
    string Style, bool TwoColumns, string Accent, string Header, string Background);

public static class CvTemplateCatalog
{
    public static IReadOnlyList<CvTemplateStyle> Templates { get; } = [
        new(1, "Chuyên nghiệp", "Popular", "Phần đầu nổi bật, hai cột cân đối cho kinh nghiệm và kỹ năng.", "professional", true, "#087f6d", "#193b43", "#ffffff"),
        new(2, "Thanh lịch", "Creative", "Sắc be nhẹ nhàng, cột thông tin phụ và tiêu đề thanh mảnh.", "elegant", true, "#946b50", "#f0e5da", "#ffffff"),
        new(3, "Công nghệ", "Creative", "Xanh đậm, tiêu đề mạnh và phân chia các mục rõ ràng.", "technology", true, "#2563a6", "#142d4e", "#ffffff"),
        new(4, "Tối giản", "Popular", "Một cột, ít trang trí, ưu tiên nội dung và khả năng đọc.", "minimal", false, "#273444", "#ffffff", "#ffffff"),
        new(5, "Hiện đại", "Creative", "Điểm nhấn xanh ngọc, ảnh vuông và bố cục hai cột thoáng.", "modern", true, "#087f6d", "#e5f3ee", "#ffffff"),
        new(6, "Cổ điển", "Popular", "Một cột với chữ serif, đường kẻ mảnh và nhịp trình bày trang trọng.", "classic", false, "#754f3a", "#ffffff", "#fffdf8")
    ];
    public static IReadOnlyList<string> BlockKeys { get; } = ["summary", "experience", "education", "skills", "languages", "certifications", "activities", "interests", "references"];
    public static CvTemplateStyle? LayMau(int id) => Templates.FirstOrDefault(m => m.Id == id);
    public static List<CvBlockLayout> TaoBoCucMacDinh(int templateId) => BlockKeys.Select(k => new CvBlockLayout
    { Key = k, Zone = LayMau(templateId)?.TwoColumns == true && k is "skills" or "languages" or "interests" or "references" ? "side" : "main" }).ToList();

    public static bool KiemTraBoCuc(int templateId, IReadOnlyList<CvBlockLayout>? layout) => layout != null &&
        (layout.Count == 0 || layout.Count == BlockKeys.Count && layout.All(b => b != null && BlockKeys.Contains(b.Key) &&
            (b.Zone == "main" || b.Zone == "side" && LayMau(templateId)?.TwoColumns == true)) && layout.Select(b => b.Key).Distinct().Count() == BlockKeys.Count);

    public static List<CvBlockLayout> DocBoCuc(string? json, int templateId)
    {
        try
        {
            var layout = json == null ? null : JsonSerializer.Deserialize<List<CvBlockLayout>>(json);
            return layout is { Count: > 0 } && KiemTraBoCuc(templateId, layout) ? layout : TaoBoCucMacDinh(templateId);
        }
        catch (JsonException) { return TaoBoCucMacDinh(templateId); }
    }
}
