using System.ComponentModel.DataAnnotations;

namespace E_Commerce_Website.Models
{
    public class Customer
    {
        [Key]
        public int CustomerId { get; set; } 
        public string CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerPassword { get; set; }
        public string? CustomerGender { get; set; }
        public string?CustomerCountry { get; set; }
        public string? CustomerCity { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerImage { get; set; }


    }
}
