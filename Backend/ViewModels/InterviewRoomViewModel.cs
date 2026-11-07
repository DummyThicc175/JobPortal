namespace JobPortal.Models;

public sealed record InterviewRoomViewModel(Guid Id, int ApplicationId, string JobTitle, string CompanyName,
    string PartnerName, bool IsEmployer, object RtcConfiguration);
