using System.ComponentModel.DataAnnotations;

namespace E_Commerce_Website.Models
{
    public class Faqs
    {
        [Key]
        public int FaqId { get; set; }

        [Required(ErrorMessage = "Question is required.")]
        [StringLength(500, ErrorMessage = "Question too long.")]
        public string Question { get; set; } = string.Empty;

        [Required(ErrorMessage = "Answer is required.")]
        [StringLength(2000, ErrorMessage = "Answer cannot exceed 2,000 characters.")]
        public string Answer { get; set; } = string.Empty;
    }
}