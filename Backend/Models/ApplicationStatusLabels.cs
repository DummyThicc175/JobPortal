namespace JobPortal.Models;

public static class ApplicationStatusLabels
{
    public static string For(ApplicationStatus status) => status switch
    {
        ApplicationStatus.Submitted => "Đã nộp", ApplicationStatus.Reviewing => "Đang xem",
        ApplicationStatus.Interview => "Phỏng vấn", ApplicationStatus.Accepted => "Đạt",
        ApplicationStatus.Rejected => "Không đạt", _ => "Không xác định"
    };
}
