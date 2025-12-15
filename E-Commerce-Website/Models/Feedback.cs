using System.ComponentModel.DataAnnotations;

namespace E_Commerce_Website.Models
{
    public class Feedback
    {
        [Key]
        public int FeedbackId { get; set; }
        public string UserName { get; set; }
        public string UserMessage { get; set; }

    }
}
