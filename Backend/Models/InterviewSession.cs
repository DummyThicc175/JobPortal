namespace JobPortal.Models;

public sealed class InterviewSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int ApplicationId { get; set; }
    public Application Application { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}
