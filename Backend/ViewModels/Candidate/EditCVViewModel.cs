using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace JobPortal.Models;

public sealed class EditCVViewModel : IValidatableObject
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    [Required, StringLength(200)] public string? FullName { get; set; }
    [Required, StringLength(200)] public string? JobTitle { get; set; }
    [Required, EmailAddress, StringLength(256)] public string? Email { get; set; }
    [StringLength(30)] public string? Phone { get; set; }
    [StringLength(500)] public string? Address { get; set; }
    [StringLength(4000)] public string? Summary { get; set; }
    public string? AvatarPath { get; set; }
    [StringLength(500)] public string? Website { get; set; }
    [MaxLength(20)] public List<ExperienceVM> Experiences { get; set; } = [];
    [MaxLength(20)] public List<EducationVM> Educations { get; set; } = [];
    [MaxLength(20)] public List<LanguageVM> Languages { get; set; } = [];
    [MaxLength(20)] public List<ITSkillVM> ITSKills { get; set; } = [];
    [MaxLength(20)] public List<ReferenceVM> References { get; set; } = [];
    [StringLength(4000)] public string? Interests { get; set; }
    [StringLength(4000)] public string? Activities { get; set; }
    [StringLength(4000)] public string? Certifications { get; set; }
    public string? ThemeColor { get; set; }
    public string? HeaderColor { get; set; }
    public string? BackgroundColor { get; set; }
    [MaxLength(9)] public List<CvBlockLayout> BlockLayout { get; set; } = [];
    public int? AiScore { get; set; }
    public List<string> AiSuggestions { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!CvTemplateCatalog.KiemTraBoCuc(TemplateId, BlockLayout))
            yield return new ValidationResult("Bố cục CV không hợp lệ. Mỗi khối chỉ xuất hiện một lần và phải nằm trong cột của mẫu.", [nameof(BlockLayout)]);
        if (Experiences == null || Educations == null || Languages == null || ITSKills == null || References == null)
            yield return new ValidationResult("Các mục CV phải là danh sách hợp lệ.");
        foreach (var color in new[] { ThemeColor, HeaderColor, BackgroundColor })
            if (color != null && !Regex.IsMatch(color, "^#[0-9a-fA-F]{6}$"))
                yield return new ValidationResult("Màu CV không hợp lệ.");
        if (Experiences?.Any(x => x == null) == true || Educations?.Any(x => x == null) == true ||
            Languages?.Any(x => x == null) == true || ITSKills?.Any(x => x == null) == true || References?.Any(x => x == null) == true)
            yield return new ValidationResult("Mục CV không được để null.");
    }
}
public sealed class ExperienceVM
{
    [StringLength(200)] public string? Company { get; set; }
    [StringLength(200)] public string? Role { get; set; }
    [StringLength(100)] public string? Duration { get; set; }
    [StringLength(4000)] public string? Description { get; set; }
}
public sealed class EducationVM
{
    [StringLength(200)] public string? School { get; set; }
    [StringLength(200)] public string? Major { get; set; }
    [StringLength(100)] public string? Duration { get; set; }
}
public sealed class LanguageVM
{
    [StringLength(2000)] public string? LanguageName { get; set; }
    [StringLength(100)] public string? Level { get; set; }
}
public sealed class ITSkillVM
{
    [StringLength(2000)] public string? SkillName { get; set; }
    [StringLength(4000)] public string? Description { get; set; }
}
public sealed class ReferenceVM
{
    [StringLength(200)] public string? Name { get; set; }
    [StringLength(200)] public string? Position { get; set; }
    [StringLength(200)] public string? Company { get; set; }
    [StringLength(256)] public string? Phone { get; set; }
}
