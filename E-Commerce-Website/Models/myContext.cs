using Microsoft.EntityFrameworkCore;

namespace E_Commerce_Website.Models
{
    public class myContext : DbContext
    {
        public myContext(DbContextOptions<myContext> options) : base(options)
        {
        }

        public DbSet<Admin> Admins { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Faqs> Faqs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Category → Products
            modelBuilder.Entity<Category>()
                .HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId);

            modelBuilder.Entity<Cart>()
                .HasOne(c => c.Product)
                .WithMany()
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cart>()
                .HasOne(c => c.Customer)
                .WithMany()
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed default Admin
            modelBuilder.Entity<Admin>().HasData(new Admin
            {
                AdminId = 98798791,
                AdminName = "SuperAdmin",
                AdminEmail = "admin@example.com",
                AdminPassword = "Admin@123", // In production, hash this password!
                AdminImage = null
            });

            // Seed default Customer
            modelBuilder.Entity<Customer>().HasData(new Customer
            {
                CustomerId = 187987987,
                CustomerName = "Default Customer",
                CustomerEmail = "customer@example.com",
                CustomerPassword = "Customer@123", // In production, hash this password!
                CustomerPhone = null,
                CustomerGender = null,
                CustomerCountry = null,
                CustomerCity = null,
                CustomerAddress = null,
                CustomerImage = null
            });
        }
    }
}
