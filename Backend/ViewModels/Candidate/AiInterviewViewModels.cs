using System.ComponentModel.DataAnnotations;

namespace JobPortal.ViewModels.Candidate;

public sealed record AiInterviewApplicationItem(int Id, string Title, string Category, string Company);
public sealed record AiInterviewHistoryItem(Guid Id, string Title, DateTime CreatedAt, bool Completed, int Answered, int Total);
public sealed record AiInterviewIndexViewModel(IReadOnlyList<AiInterviewApplicationItem> Applications, IReadOnlyList<AiInterviewHistoryItem> Sessions);
public sealed record AiInterviewRoomViewModel(Guid Id, string JobTitle, string CategoryName, string CompanyName,
    bool HasCvContext, int QuestionCount, int Number, string Question);
public sealed record AiInterviewReportTurn(int Number, string Question, string Answer, string Feedback, string Improvement, string ExpectedPoints, int Score);
public sealed record AiInterviewReportViewModel(Guid Id, string JobTitle, string CategoryName, int QuestionCount,
    DateTime CreatedAt, DateTime? EndedAt, IReadOnlyList<AiInterviewReportTurn> Turns, int? Score);

public sealed class AiInterviewAnswerInput
{
    [Range(1, 5)] public int Number { get; set; }
    [Required, StringLength(4000, MinimumLength = 2)] public string Answer { get; set; } = "";
}
