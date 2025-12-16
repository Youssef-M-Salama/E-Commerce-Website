using System.ComponentModel.DataAnnotations;

namespace E_Commerce_Website.Models
{
    public class Feedback
    {
        [Key]
        public int FeedbackId { get; set; }

        [Required(ErrorMessage = "Your name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required.")]
        [StringLength(2000, ErrorMessage = "Message cannot exceed 2,000 characters.")]
        public string UserMessage { get; set; } = string.Empty;
    }
}