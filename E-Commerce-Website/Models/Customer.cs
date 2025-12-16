using System.ComponentModel.DataAnnotations;

namespace E_Commerce_Website.Models
{
    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [StringLength(100)]
        public string CustomerEmail { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number.")]
        [StringLength(20)]
        public string? CustomerPhone { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        public string CustomerPassword { get; set; } = string.Empty;

        [StringLength(10)]
        public string? CustomerGender { get; set; } // Consider enum later

        [StringLength(100)]
        public string? CustomerCountry { get; set; }

        [StringLength(100)]
        public string? CustomerCity { get; set; }

        [StringLength(255)]
        public string? CustomerAddress { get; set; }

        [StringLength(255)]
        public string? CustomerImage { get; set; }
    }
}