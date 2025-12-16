using E_Commerce_Website.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace E_Commerce_Website.Controllers
{
    public class AdminController : Controller
    {
        private readonly myContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        // private readonly ILogger<AdminController> _logger; // 🔔 Add later if using DI logging

        public AdminController(myContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // ✅ Helper: Guard admin access
        private bool IsAdminLoggedIn()
        {
            return HttpContext.Session.GetString("admin_session") != null;
        }

        private IActionResult RedirectToLoginIfUnauthorized()
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login");
            return null;
        }

        // ✅ Index (guarded)
        public IActionResult Index()
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("Login");
            return View();
        }

        // ✅ Login (GET)
        [HttpGet]
        public IActionResult Login()
        {
            // Prevent logged-in admin from re-accessing login
            if (IsAdminLoggedIn()) return RedirectToAction("Index");
            return View();
        }

        // ✅ Login (POST) — with validation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string adminEmail, string adminPassword)
        {
            if (IsAdminLoggedIn()) return RedirectToAction("Index");

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                ModelState.AddModelError("", "Email and password are required.");
                return View();
            }

            var admin = _context.Admins.FirstOrDefault(a => a.AdminEmail == adminEmail);
            if (admin != null && admin.AdminPassword == adminPassword) // ⚠️ TODO: Hash passwords later!
            {
                HttpContext.Session.SetString("admin_session", admin.AdminId.ToString());
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "Invalid email or password.");
            return View();
        }

        // ✅ Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("admin_session");
            return RedirectToAction("Login");
        }

        // ✅ Profile (GET)
        [HttpGet]
        public IActionResult Profile()
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            var adminIdStr = HttpContext.Session.GetString("admin_session");
            if (!int.TryParse(adminIdStr, out int adminId))
                return RedirectToAction("Logout");

            var admin = _context.Admins.Find(adminId);
            if (admin == null) return RedirectToAction("Logout");

            return View(admin);
        }

        // ✅ Profile (POST) — with validation & overpost protection
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile([Bind("AdminId,AdminName,AdminEmail,AdminPassword")] Admin admin)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            if (admin == null)
                return BadRequest();

            // 🔐 Prevent editing other admins
            var currentAdminId = int.Parse(HttpContext.Session.GetString("admin_session") ?? "0");
            if (admin.AdminId != currentAdminId)
                return Forbid();

            var existing = _context.Admins.Find(admin.AdminId);
            if (existing == null) return NotFound();

            // Check email uniqueness (excluding self)
            if (_context.Admins.Any(a => a.AdminEmail == admin.AdminEmail && a.AdminId != admin.AdminId))
            {
                ModelState.AddModelError("AdminEmail", "An admin with this email already exists.");
            }

            if (ModelState.IsValid)
            {
                // Update only allowed fields
                existing.AdminName = admin.AdminName;
                existing.AdminEmail = admin.AdminEmail;
                if (!string.IsNullOrWhiteSpace(admin.AdminPassword))
                    existing.AdminPassword = admin.AdminPassword; // ⚠️ Hash in prod!

                try
                {
                    _context.SaveChanges();
                    TempData["Success"] = "Profile updated successfully!";
                    return RedirectToAction("Profile");
                }
                catch (Exception ex)
                {
                    // _logger?.LogError(ex, "Failed to update admin profile.");
                    ModelState.AddModelError("", "An error occurred. Please try again.");
                }
            }

            return View(existing);
        }

        // ✅ ChangeProfileImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeProfileImage(IFormFile imageFile)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            var adminId = int.Parse(HttpContext.Session.GetString("admin_session") ?? "0");
            var admin = _context.Admins.Find(adminId);
            if (admin == null) return NotFound();

            if (imageFile == null || imageFile.Length == 0)
            {
                ModelState.AddModelError("imageFile", "Please select an image.");
                return View("Profile", admin);
            }

            // ✅ Validate file type & size (optional but recommended)
            var allowedTypes = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (!allowedTypes.Contains(extension))
            {
                ModelState.AddModelError("imageFile", "Only JPG, PNG, and GIF files are allowed.");
                return View("Profile", admin);
            }

            if (imageFile.Length > 5 * 1024 * 1024) // 5 MB
            {
                ModelState.AddModelError("imageFile", "File size must not exceed 5 MB.");
                return View("Profile", admin);
            }

            try
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "AdminImage");
                Directory.CreateDirectory(uploadsFolder);

                string newFileName = Guid.NewGuid() + extension;
                string filePath = Path.Combine(uploadsFolder, newFileName);

                // Save new image
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Delete old image (if exists)
                if (!string.IsNullOrEmpty(admin.AdminImage))
                {
                    string oldPath = Path.Combine(uploadsFolder, admin.AdminImage);
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                admin.AdminImage = newFileName;
                _context.SaveChanges();

                TempData["Success"] = "Profile image updated!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                // _logger?.LogError(ex, "Image upload failed.");
                ModelState.AddModelError("", "Failed to upload image. Please try again.");
                return View("Profile", admin);
            }
        }

        // ✅ FetchCustomer
        public IActionResult FetchCustomer()
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            return View(_context.Customers.ToList());
        }

        // ✅ UpdateCustomer (GET)
        [HttpGet]
        public IActionResult UpdateCustomer(int id)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            var customer = _context.Customers.Find(id);
            if (customer == null) return NotFound();
            return View(customer);
        }

        // ✅ UpdateCustomer (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCustomer(
            [Bind("CustomerId,CustomerName,CustomerEmail,CustomerPhone,CustomerPassword,CustomerGender,CustomerCountry,CustomerCity,CustomerAddress")]
            Customer customer,
            IFormFile customerImage)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            if (customer == null) return BadRequest();

            var existing = _context.Customers.Find(customer.CustomerId);
            if (existing == null) return NotFound();

            // Check email uniqueness (excluding self)
            if (_context.Customers.Any(c => c.CustomerEmail == customer.CustomerEmail && c.CustomerId != customer.CustomerId))
            {
                ModelState.AddModelError("CustomerEmail", "A customer with this email already exists.");
            }

            if (ModelState.IsValid)
            {
                // Update scalar properties
                existing.CustomerName = customer.CustomerName;
                existing.CustomerEmail = customer.CustomerEmail;
                existing.CustomerPhone = customer.CustomerPhone;
                existing.CustomerGender = customer.CustomerGender;
                existing.CustomerCountry = customer.CustomerCountry;
                existing.CustomerCity = customer.CustomerCity;
                existing.CustomerAddress = customer.CustomerAddress;

                // Update password only if provided
                if (!string.IsNullOrWhiteSpace(customer.CustomerPassword))
                    existing.CustomerPassword = customer.CustomerPassword; // ⚠️ Hash later!

                // Handle image
                if (customerImage?.Length > 0)
                {
                    var uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "CustomerImage");
                    Directory.CreateDirectory(uploadFolder);

                    var extension = Path.GetExtension(customerImage.FileName);
                    var newFileName = Guid.NewGuid() + extension;
                    var filePath = Path.Combine(uploadFolder, newFileName);

                    // Save new
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await customerImage.CopyToAsync(stream);
                    }

                    // Delete old
                    if (!string.IsNullOrEmpty(existing.CustomerImage))
                    {
                        var oldPath = Path.Combine(uploadFolder, existing.CustomerImage);
                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);
                    }

                    existing.CustomerImage = newFileName;
                }

                try
                {
                    _context.SaveChanges();
                    TempData["Success"] = "Customer updated!";
                    return RedirectToAction("FetchCustomer");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Failed to save changes.");
                }
            }

            return View(customer);
        }

        // ✅ CustomerDetails
        public IActionResult CustomerDetails(int id)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            var customer = _context.Customers.Find(id);
            if (customer == null) return NotFound();
            return View(customer);
        }

        // ✅ DeletePermission (confirmation)
        public IActionResult DeletePermission(int id)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            var customer = _context.Customers.Find(id);
            if (customer == null) return NotFound();
            return View(customer);
        }

        // ✅ DeleteCustomer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCustomer(int id)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            var customer = _context.Customers.Find(id);
            if (customer == null) return NotFound();

            // ✅ Optional: Delete associated cart items
            var cartItems = _context.Carts.Where(c => c.CustomerId == id);
            _context.Carts.RemoveRange(cartItems);

            // ✅ Delete image
            if (!string.IsNullOrEmpty(customer.CustomerImage))
            {
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "CustomerImage", customer.CustomerImage);
                if (System.IO.File.Exists(imagePath))
                    System.IO.File.Delete(imagePath);
            }

            _context.Customers.Remove(customer);
            _context.SaveChanges();

            TempData["Success"] = "Customer deleted.";
            return RedirectToAction("FetchCustomer");
        }

        // ✅ FetchCategory
        public IActionResult FetchCategory()
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            return View(_context.Categories.ToList());
        }

        // ✅ AddCategory (GET/POST)
        [HttpGet]
        public IActionResult AddCategory()
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddCategory([Bind("CategoryName")] Category category)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            if (ModelState.IsValid)
            {
                if (_context.Categories.Any(c => c.CategoryName == category.CategoryName))
                {
                    ModelState.AddModelError("CategoryName", "Category already exists.");
                    return View(category);
                }

                _context.Categories.Add(category);
                _context.SaveChanges();
                TempData["Success"] = $"Category '{category.CategoryName}' added.";
                return RedirectToAction("FetchCategory");
            }
            return View(category);
        }

        // ✅ UpdateCategory
        [HttpGet]
        public IActionResult UpdateCategory(int id)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            var category = _context.Categories.Find(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateCategory([Bind("CategoryId,CategoryName")] Category category)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            if (ModelState.IsValid)
            {
                var existing = _context.Categories.Find(category.CategoryId);
                if (existing == null) return NotFound();

                if (_context.Categories.Any(c => c.CategoryName == category.CategoryName && c.CategoryId != category.CategoryId))
                {
                    ModelState.AddModelError("CategoryName", "Category name already in use.");
                    return View(category);
                }

                existing.CategoryName = category.CategoryName;
                _context.SaveChanges();
                TempData["Success"] = "Category updated.";
                return RedirectToAction("FetchCategory");
            }
            return View(category);
        }

        // ✅ DeleteCategory (with permission)
        [HttpGet]
        public IActionResult DeletePermissionCategory(int id)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            var category = _context.Categories
                .Include(c => c.Products)
                .FirstOrDefault(c => c.CategoryId == id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCategory(int id)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            var category = _context.Categories
                .Include(c => c.Products)
                .FirstOrDefault(c => c.CategoryId == id);
            if (category == null) return NotFound();

            // ⚠️ If products exist, decide: delete cascade or prevent deletion
            if (category.Products?.Any() == true)
            {
                // Option 1: Prevent deletion
                TempData["Error"] = "Cannot delete category with products.";
                return RedirectToAction("FetchCategory");

                // Option 2: Delete products too (Cascade is already set in OnModelCreating)
                // (Your current config uses Cascade for Category → Products)
            }

            _context.Categories.Remove(category);
            _context.SaveChanges();

            TempData["Success"] = "Category deleted.";
            return RedirectToAction("FetchCategory");
        }

        // ✅ FetchProduct
        public IActionResult FetchProduct()
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            var products = _context.Products.Include(p => p.Category).ToList();
            return View(products);
        }

        // ✅ AddProduct (GET/POST)
        [HttpGet]
        public IActionResult AddProduct()
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            ViewData["Categories"] = _context.Categories.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(
     [Bind("ProductName,ProductPrice,ProductDescription,CategoryId")] Product product,
     IFormFile productImage)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            // Populate categories for dropdown if validation fails
            ViewData["Categories"] = _context.Categories.ToList();

            // Validate image first
            if (productImage == null || productImage.Length == 0)
            {
                ModelState.AddModelError("productImage", "Product image is required.");
            }
            else
            {
                var ext = Path.GetExtension(productImage.FileName).ToLowerInvariant();
                if (!new[] { ".jpg", ".jpeg", ".png" }.Contains(ext))
                    ModelState.AddModelError("productImage", "Only JPG/PNG images allowed.");
                else if (productImage.Length > 10 * 1024 * 1024)
                    ModelState.AddModelError("productImage", "Max file size: 10 MB.");
            }

            // ✅ Assign image to product BEFORE checking ModelState
            if (productImage != null && productImage.Length > 0)
            {
                var uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "ProductImage");
                Directory.CreateDirectory(uploadFolder);

                var newFileName = Guid.NewGuid() + Path.GetExtension(productImage.FileName);
                var filePath = Path.Combine(uploadFolder, newFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await productImage.CopyToAsync(stream);
                }

                // Set the product's image property here
                product.ProductImage = newFileName;
            }

            // Now check ModelState
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"{product.ProductName} added.";
                    return RedirectToAction("FetchProduct");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Failed to save product.");
                }
            }

            // If validation fails, return the view with product (and categories)
            return View(product);
        }


        // ✅ ProductDetails
        public IActionResult ProductDetails(int id)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            var product = _context.Products.Include(p => p.Category).FirstOrDefault(p => p.ProductId == id);
            if (product == null) return NotFound();
            return View(product);
        }

        // ✅ DeletePermissionProduct
        public IActionResult DeletePermissionProduct(int id)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            var product = _context.Products.Find(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // ✅ DeleteProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteProduct(int id)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            var product = _context.Products.Find(id);
            if (product == null) return NotFound();

            // ✅ Delete image
            if (!string.IsNullOrEmpty(product.ProductImage))
            {
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "ProductImage", product.ProductImage);
                if (System.IO.File.Exists(imagePath))
                    System.IO.File.Delete(imagePath);
            }

            // ✅ Remove from carts (optional but clean)
            var cartItems = _context.Carts.Where(c => c.ProductId == id);
            _context.Carts.RemoveRange(cartItems);

            _context.Products.Remove(product);
            _context.SaveChanges();

            TempData["Success"] = "Product deleted.";
            return RedirectToAction("FetchProduct");
        }

        // ✅ UpdateProduct (GET/POST)
        [HttpGet]
        public IActionResult UpdateProduct(int id)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            var product = _context.Products.Find(id);
            if (product == null) return NotFound();

            ViewData["Categories"] = _context.Categories.ToList();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProduct(
            [Bind("ProductId,ProductName,ProductPrice,ProductDescription,CategoryId")] Product product,
            IFormFile productImage)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            var existing = _context.Products.Find(product.ProductId);
            if (existing == null) return NotFound();

            ViewData["Categories"] = _context.Categories.ToList();

            if (ModelState.IsValid)
            {
                // Update properties
                existing.ProductName = product.ProductName;
                existing.ProductPrice = product.ProductPrice;
                existing.ProductDescription = product.ProductDescription;
                existing.CategoryId = product.CategoryId;

                // Handle image
                if (productImage?.Length > 0)
                {
                    var uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "ProductImage");
                    Directory.CreateDirectory(uploadFolder);

                    // Delete old
                    if (!string.IsNullOrEmpty(existing.ProductImage))
                    {
                        var oldPath = Path.Combine(uploadFolder, existing.ProductImage);
                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);
                    }

                    // Save new
                    var newFileName = Guid.NewGuid() + Path.GetExtension(productImage.FileName);
                    var filePath = Path.Combine(uploadFolder, newFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await productImage.CopyToAsync(stream);
                    }

                    existing.ProductImage = newFileName;
                }

                try
                {
                    _context.SaveChanges();
                    TempData["Success"] = "Product updated!";
                    return RedirectToAction("FetchProduct");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Failed to update product.");
                }
            }

            // Repopulate for re-display
            existing.ProductName = product.ProductName; // rebind changed values
            existing.ProductPrice = product.ProductPrice;
            existing.ProductDescription = product.ProductDescription;
            existing.CategoryId = product.CategoryId;
            return View("UpdateProduct", existing);
        }

        // ✅ FetchFeedback
        public IActionResult FetchFeedback()
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            return View(_context.Feedbacks.ToList());
        }

        // ✅ DeletePermissionFeedback
        public IActionResult DeletePermissionFeedback(int id)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            var feedback = _context.Feedbacks.Find(id);
            if (feedback == null) return NotFound();
            return View(feedback);
        }

        // ✅ DeleteFeedback
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteFeedback(int id)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            var feedback = _context.Feedbacks.Find(id);
            if (feedback == null) return NotFound();

            _context.Feedbacks.Remove(feedback);
            _context.SaveChanges();

            TempData["Success"] = "Feedback deleted.";
            return RedirectToAction("FetchFeedback");
        }
    }
}