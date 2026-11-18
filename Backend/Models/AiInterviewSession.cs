using System.ComponentModel.DataAnnotations;

namespace JobPortal.Models;

public sealed class AiInterviewSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int ApplicationId { get; set; }
    public Application Application { get; set; } = null!;
    [MaxLength(450)] public string CandidateId { get; set; } = "";
    [MaxLength(200)] public string JobTitle { get; set; } = "";
    [MaxLength(200)] public string CategoryName { get; set; } = "";
    [MaxLength(200)] public string CompanyName { get; set; } = "";
    [MaxLength(4000)] public string JobContext { get; set; } = "";
    [MaxLength(4000)] public string CvContext { get; set; } = "";
    public bool HasCvContext { get; set; }
    public int QuestionCount { get; set; } = 5;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    [ConcurrencyCheck] public Guid Version { get; set; } = Guid.NewGuid();
    public List<AiInterviewTurn> Turns { get; set; } = [];
}

public sealed class AiInterviewTurn
{
    public int Id { get; set; }
    public Guid SessionId { get; set; }
    public AiInterviewSession Session { get; set; } = null!;
    public int Number { get; set; }
    [MaxLength(1200)] public string Question { get; set; } = "";
    [MaxLength(2000)] public string ExpectedPoints { get; set; } = "";
    [MaxLength(4000)] public string? Answer { get; set; }
    [MaxLength(2000)] public string? Feedback { get; set; }
    [MaxLength(2000)] public string? Improvement { get; set; }
    public int? Score { get; set; }
    public DateTime? AnsweredAt { get; set; }
    // Optimistic concurrency: only one answer may be committed for a turn.
    [ConcurrencyCheck] public Guid Version { get; set; } = Guid.NewGuid();
}
