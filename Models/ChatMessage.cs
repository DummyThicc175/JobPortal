using System.ComponentModel.DataAnnotations;

namespace JobPortal.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; } // ID người gửi

        [Required]
        public string ReceiverId { get; set; } // ID người nhận

        [Required(ErrorMessage = "Nội dung tin nhắn không được để trống")]
        public string Content { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;
    }
}