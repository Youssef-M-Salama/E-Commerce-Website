using System.ComponentModel.DataAnnotations;

namespace E_Commerce_Website.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public List<Product> Products { get; set; }
    }
}
