using System.ComponentModel.DataAnnotations;

namespace JobPortal.Models;

public abstract class CvSection
{
    public int Id { get; set; }
    public int CVId { get; set; }
    public int SortOrder { get; set; }
}

public sealed class CvExperience : CvSection
{
    [MaxLength(200)] public string? Company { get; set; }
    [MaxLength(200)] public string? Role { get; set; }
    [MaxLength(100)] public string? Duration { get; set; }
    [MaxLength(4000)] public string? Description { get; set; }
}

public sealed class CvEducation : CvSection
{
    [MaxLength(200)] public string? School { get; set; }
    [MaxLength(200)] public string? Major { get; set; }
    [MaxLength(100)] public string? Duration { get; set; }
}

public sealed class CvSkill : CvSection
{
    [MaxLength(2000)] public string? SkillName { get; set; }
    [MaxLength(4000)] public string? Description { get; set; }
}

public sealed class CvLanguage : CvSection
{
    [MaxLength(2000)] public string? LanguageName { get; set; }
    [MaxLength(100)] public string? Level { get; set; }
}

public sealed class CvReference : CvSection
{
    [MaxLength(200)] public string? Name { get; set; }
    [MaxLength(200)] public string? Position { get; set; }
    [MaxLength(200)] public string? Company { get; set; }
    [MaxLength(256)] public string? Phone { get; set; }
}
