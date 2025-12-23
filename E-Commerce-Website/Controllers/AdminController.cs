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
        private readonly IFileService _fileService;
        private readonly string[] _allowedFileExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private readonly int _defaultMaxFileSizeInMB = 5;
        public AdminController(myContext context, IWebHostEnvironment webHostEnvironment, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
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

            try
            {
                // ===============================
                // Use FileService
                // ===============================

                // Save new image
                var result = await _fileService.SaveFileAsync(
                    imageFile,
                    _allowedFileExtensions,
                    "AdminImage",
                    _defaultMaxFileSizeInMB
                );

                if (!result.IsSaved)
                {
                    ModelState.AddModelError("imageFile", result.Message);
                    return View("Profile", admin);
                }

                // Delete old image (if exists)
                if (!string.IsNullOrEmpty(admin.AdminImage))
                {
                    _fileService.DeleteFile(admin.AdminImage);
                }

                //  Store the FULL relative path (e.g., "/AdminImage/guid.jpg")
                admin.AdminImage = result.FilePath;
                _context.SaveChanges();

                TempData["Success"] = "Profile image updated!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Failed to upload image. Please try again.");
                return View("Profile", admin);
            }
        }

        //  FetchCustomer
        public IActionResult FetchCustomer()
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            return View(_context.Customers.ToList());
        }

        // UpdateCustomer (GET)
        [HttpGet]
        public IActionResult UpdateCustomer(int id)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            var customer = _context.Customers.Find(id);
            if (customer == null) return NotFound();
            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCustomer(
    [Bind("CustomerId,CustomerName,CustomerEmail,CustomerPhone,CustomerGender,CustomerCountry,CustomerCity,CustomerAddress")]
    Customer customer,
    string CustomerPassword,
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

            // ✅ Remove password validation errors if empty
            if (string.IsNullOrWhiteSpace(CustomerPassword))
            {
                ModelState.Remove("CustomerPassword");
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
                if (!string.IsNullOrWhiteSpace(CustomerPassword))
                    existing.CustomerPassword = CustomerPassword;

                // ===============================
                // Handle image with FileService
                // ===============================
                if (customerImage?.Length > 0)
                {
                    try
                    {
                        // Save new image
                        var result = await _fileService.SaveFileAsync(
                            customerImage,
                            _allowedFileExtensions,
                            "CustomerImage",
                            _defaultMaxFileSizeInMB
                        );

                        if (!result.IsSaved)
                        {
                            ModelState.AddModelError("customerImage", result.Message);
                            return View(customer);
                        }

                        // Delete old image
                        if (!string.IsNullOrEmpty(existing.CustomerImage))
                        {
                            _fileService.DeleteFile(existing.CustomerImage);
                        }

                        // Store full relative path
                        existing.CustomerImage = result.FilePath;
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Failed to upload image.");
                        return View(customer);
                    }
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCustomer(int id)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            var customer = _context.Customers.Find(id);
            if (customer == null) return NotFound();

            //  Optional: Delete associated cart items
            var cartItems = _context.Carts.Where(c => c.CustomerId == id);
            _context.Carts.RemoveRange(cartItems);

            // ===============================
            // Delete image using FileService
            // ===============================
            if (!string.IsNullOrEmpty(customer.CustomerImage))
            {
                _fileService.DeleteFile(customer.CustomerImage);
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

            if (ModelState.IsValid && productImage != null && productImage.Length > 0)
            {
                try
                {
                    // ===============================
                    // Use FileService
                    // ===============================
                    var result = await _fileService.SaveFileAsync(
                        productImage,
                        _allowedFileExtensions,
                        "ProductImage",
                        10
                    );

                    if (!result.IsSaved)
                    {
                        ModelState.AddModelError("productImage", result.Message);
                        return View(product);
                    }

                    //  Store full relative path
                    product.ProductImage = result.FilePath;

                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"{product.ProductName} added successfully!";
                    return RedirectToAction("FetchProduct");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Failed to save product.");
                }
            }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteProduct(int id)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            var product = _context.Products.Find(id);
            if (product == null) return NotFound();

            // ===============================
            // Delete image using FileService
            // ===============================
            if (!string.IsNullOrEmpty(product.ProductImage))
            {
                _fileService.DeleteFile(product.ProductImage);
            }

            _context.Products.Remove(product);
            _context.SaveChanges();

            TempData["Success"] = $"{product.ProductName} deleted successfully!";
            return RedirectToAction("FetchProduct");
        }
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

            // Populate categories for dropdown if validation fails
            ViewData["Categories"] = _context.Categories.ToList();

            if (ModelState.IsValid)
            {
                // Update scalar properties
                existing.ProductName = product.ProductName;
                existing.ProductPrice = product.ProductPrice;
                existing.ProductDescription = product.ProductDescription;
                existing.CategoryId = product.CategoryId;

                // ===============================
                // Handle image with FileService
                // ===============================
                if (productImage?.Length > 0)
                {
                    try
                    {
                        // Save new image
                        var result = await _fileService.SaveFileAsync(
                            productImage,
                            _allowedFileExtensions,
                            "ProductImage",
                            10
                        );

                        if (!result.IsSaved)
                        {
                            ModelState.AddModelError("productImage", result.Message);
                            return View(product);
                        }

                        // Delete old image
                        if (!string.IsNullOrEmpty(existing.ProductImage))
                        {
                            _fileService.DeleteFile(existing.ProductImage);
                        }

                        // ✅ Store full relative path
                        existing.ProductImage = result.FilePath;
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Failed to upload image.");
                        return View(product);
                    }
                }

                try
                {
                    _context.SaveChanges();
                    TempData["Success"] = $"{product.ProductName} updated successfully!";
                    return RedirectToAction("FetchProduct");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Failed to save changes.");
                }
            }

            return View(product);
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