using System.ComponentModel.DataAnnotations;

namespace E_Commerce_Website.Models
{
    public class Faqs
    {
        [Key]
        public int FaqId { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
    }
}
