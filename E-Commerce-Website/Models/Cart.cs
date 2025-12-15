namespace E_Commerce_Website.Models
{
    public class Cart
    {
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public int CustomerId { get; set; }
        public int ProductQuantity { get; set; }
        public int CartStatus { get; set; }

        // Navigation properties (add these)
        public Product Product { get; set; }      
        public Customer Customer { get; set; }    
    }
}