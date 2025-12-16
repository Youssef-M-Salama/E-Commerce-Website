using System.ComponentModel.DataAnnotations;

namespace E_Commerce_Website.Models
{
    public class Cart
    {
        [Key]
        public int CartId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        [Range(1, 99, ErrorMessage = "Quantity must be between 1 and 99.")]
        public int ProductQuantity { get; set; }

        [Required]
        [Range(0, 1, ErrorMessage = "Invalid cart status.")]
        public int CartStatus { get; set; } // 0 = ordered, 1 = active

        // Navigation (optional for EF, but keep for .Include())
        public Product? Product { get; set; }
        public Customer? Customer { get; set; }
    }
}