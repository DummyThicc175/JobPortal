namespace JobPortal.Models
{
    public class AiReviewResult
    {
        public int Score { get; set; }
        public List<string> Suggestions { get; set; } = new List<string>();
    }
}