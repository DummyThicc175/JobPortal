namespace JobPortal.Models
{
    public class AiReviewResult
    {
        [System.Text.Json.Serialization.JsonRequired]
        public int Score { get; set; }
        [System.Text.Json.Serialization.JsonRequired]
        public List<string> Suggestions { get; set; } = new List<string>();
    }
}
