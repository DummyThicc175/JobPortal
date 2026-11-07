using System.ComponentModel.DataAnnotations;

namespace JobPortal.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(450)]
        public string SenderId { get; set; } = "";

        [Required]
        [MaxLength(450)]
        public string ReceiverId { get; set; } = "";

        [Required(ErrorMessage = "Nội dung tin nhắn không được để trống")]
        [MaxLength(2000)]
        public string Content { get; set; } = "";

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;
    }
}
