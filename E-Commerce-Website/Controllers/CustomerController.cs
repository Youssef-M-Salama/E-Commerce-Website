using System.ComponentModel.DataAnnotations;
using E_Commerce_Website.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGeneration;

namespace E_Commerce_Website.Controllers
{
    public class CustomerController : Controller
    {
        private readonly myContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IFileService _fileService;
        private readonly string[] _allowedFileExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private readonly int _defaultMaxFileSizeInMB = 5;


        public CustomerController(myContext context, IWebHostEnvironment webHostEnvironment, IFileService fileService)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _fileService = fileService;
           
        }

        // ✅ Helper: Get logged-in customer ID or redirect
        private IActionResult RedirectToLoginIfUnauthorized()
        {
            var sessionId = HttpContext.Session.GetString("customerSession");
            if (string.IsNullOrEmpty(sessionId) || !int.TryParse(sessionId, out _))
                return RedirectToAction("CustomerLogin");
            return null;
        }

        // ✅ Index — safe & efficient
        public IActionResult Index()
        {
            var categories = _context.Categories.AsNoTracking().ToList();
            var products = _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .ToList();

            ViewData["Categories"] = categories;
            ViewData["Products"] = products;
            ViewBag.checkSession = HttpContext.Session.GetString("customerSession");

            return View();
        }

        // ✅ Login (GET)
        [HttpGet]
        public IActionResult CustomerLogin()
        {
            // Prevent login loop
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("customerSession")))
                return RedirectToAction("Index");
            return View();
        }

        // ✅ Login (POST) — secured
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CustomerLogin(string customerEmail, string customerPassword)
        {
            // 🔹 Validate input
            customerEmail = customerEmail?.Trim();
            customerPassword = customerPassword?.Trim();

            if (string.IsNullOrWhiteSpace(customerEmail) || string.IsNullOrWhiteSpace(customerPassword))
            {
                ModelState.AddModelError("", "Email and password are required.");
                return View();
            }

            if (!IsValidEmail(customerEmail))
            {
                ModelState.AddModelError("customerEmail", "Invalid email format.");
                return View();
            }

            // 🔹 Find customer (by email, unique assumed)
            var customer = _context.Customers
                .FirstOrDefault(e => e.CustomerEmail == customerEmail);

            // 🔹 Constant-time comparison (prevents timing attacks)
            bool isValid = customer != null &&
                           customer.CustomerPassword != null &&
                           customerPassword != null &&
                           customer.CustomerPassword.Length == customerPassword.Length &&
                           customer.CustomerPassword.SequenceEqual(customerPassword);

            // ⚠️ TODO: Replace with password hashing (e.g., PasswordHasher<Customer>)
            // For now: plain-text comparison — only for dev/prototype.

            if (isValid)
            {
                HttpContext.Session.SetString("customerSession", customer.CustomerId.ToString());
                return RedirectToAction("Index");
            }

            // 🔹 Generic message to prevent enumeration attack
            ModelState.AddModelError("", "Invalid email or password.");
            return View();
        }

        // ✅ Registration
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CustomerRegistration([Bind("CustomerName,CustomerEmail,CustomerPassword,CustomerPhone,CustomerGender,CustomerCountry,CustomerCity,CustomerAddress")] Customer customer)
        {
            // 🔹 Sanitize
            customer.CustomerEmail = customer.CustomerEmail?.Trim();
            customer.CustomerPassword = customer.CustomerPassword?.Trim();

            // 🔹 Validate uniqueness
            if (_context.Customers.Any(c => c.CustomerEmail == customer.CustomerEmail))
            {

                ModelState.AddModelError("CustomerEmail", "Email already registered.");
                return View("CustomerLogin", customer);
            }

            if (ModelState.IsValid)
            {
                _context.Customers.Add(customer);
                _context.SaveChanges();
                TempData["Success"] = "Account created! Please log in.";
                return RedirectToAction("CustomerLogin");
            }

            return View("CustomerLogin", customer);
        }

        // ✅ Logout
        public IActionResult CustomerLogout()
        {
            HttpContext.Session.Remove("customerSession");
            return RedirectToAction("Index");
        }

        // ✅ Profile (GET)
        public IActionResult CustomerProfile()
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            var customerIdStr = HttpContext.Session.GetString("customerSession");
            if (!int.TryParse(customerIdStr, out int customerId))
                return RedirectToAction("CustomerLogout");

            var customer = _context.Customers.Find(customerId);
            if (customer == null)
                return RedirectToAction("CustomerLogout");

            return View(customer);
        }

        // ✅ Profile (POST) — secure update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CustomerProfile([Bind("CustomerId,CustomerName,CustomerEmail,CustomerPhone,CustomerGender,CustomerCountry,CustomerCity,CustomerAddress")] Customer model)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            int currentCustomerId = int.Parse(HttpContext.Session.GetString("customerSession") ?? "0");

            if (model.CustomerId != currentCustomerId)
                return Forbid(); // Prevent ID tampering

            var customer = _context.Customers.Find(model.CustomerId);
            if (customer == null) return NotFound();

            // 🔹 Re-check email uniqueness (excluding self)
            if (_context.Customers.Any(c => c.CustomerEmail == model.CustomerEmail && c.CustomerId != model.CustomerId))
            {
                ModelState.AddModelError("CustomerEmail", "Email already in use.");
                return View(model);
            }

            if (ModelState.IsValid)
            {
                customer.CustomerName = model.CustomerName;
                customer.CustomerEmail = model.CustomerEmail;
                customer.CustomerPhone = model.CustomerPhone;
                customer.CustomerGender = model.CustomerGender;
                customer.CustomerCountry = model.CustomerCountry;
                customer.CustomerCity = model.CustomerCity;
                customer.CustomerAddress = model.CustomerAddress;

                _context.SaveChanges();
                TempData["Success"] = "Profile updated!";
                return RedirectToAction("CustomerProfile");
            }

            return View(model);
        }

        // ✅ Change Profile Image
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeProfileImage(int customerId, IFormFile customerImage)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            int currentCustomerId = int.Parse(HttpContext.Session.GetString("customerSession") ?? "0");
            if (customerId != currentCustomerId)
                return Forbid();

            var customer = _context.Customers.Find(customerId);
            if (customer == null) return NotFound();

            if (customerImage != null && customerImage.Length > 0)
            {
                try
                {
                    // ===============================
                    // Use FileService
                    // ===============================

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
                        return View("CustomerProfile", customer);
                    }

                    // Delete old image
                    if (!string.IsNullOrEmpty(customer.CustomerImage))
                    {
                        _fileService.DeleteFile(Path.Combine("CustomerImage", customer.CustomerImage));
                    }

                    // Update DB
                    customer.CustomerImage = Path.GetFileName(result.FilePath);
                    _context.SaveChanges();

                    TempData["Success"] = "Profile image updated!";
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Failed to upload image.");
                }
            }

            return RedirectToAction("CustomerProfile");
        }


 
        [HttpGet]
        public IActionResult Feedback()
        {
            return View();
        }

        // ✅ Feedback
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Feedback([Bind("UserName,UserMessage")] Feedback feedback)
        {
            if (ModelState.IsValid)
            {
                _context.Feedbacks.Add(feedback);
                _context.SaveChanges();
                TempData["Success"] = $"Thank you, {feedback.UserName}! Your feedback has been received.";
                return RedirectToAction("Feedback");
            }
            return View(feedback);
        }

        // ✅ Product Details
        public IActionResult ProductDetails(int id)
        {
            var product = _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
                return NotFound();

            ViewBag.checkSession = HttpContext.Session.GetString("customerSession");
            return View(product);
        }

        // ✅ Add to Cart (from list or details)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(int productId, [Range(1, 99)] int quantity = 1)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            int customerId = int.Parse(HttpContext.Session.GetString("customerSession")!);

            var product = _context.Products.Find(productId);
            if (product == null)
                return NotFound("Product not found.");

            var cartItem = _context.Carts
                .FirstOrDefault(c => c.CustomerId == customerId && c.ProductId == productId && c.CartStatus == 1);

            if (cartItem != null)
            {
                cartItem.ProductQuantity = Math.Min(99, cartItem.ProductQuantity + quantity);
            }
            else
            {
                cartItem = new Cart
                {
                    ProductId = productId,
                    CustomerId = customerId,
                    ProductQuantity = quantity,
                    CartStatus = 1
                };
                _context.Carts.Add(cartItem);
            }

            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        // ✅ View Cart
        public IActionResult ViewCart()
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            int customerId = int.Parse(HttpContext.Session.GetString("customerSession")!);

            var cartItems = _context.Carts
                .Where(c => c.CustomerId == customerId && c.CartStatus == 1)
                .Include(c => c.Product)
                .ThenInclude(p => p.Category)
                .ToList();

            return View(cartItems);
        }

        // ✅ Update Cart Quantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateCartItem(int cartId, [Range(1, 99)] int quantity)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            int customerId = int.Parse(HttpContext.Session.GetString("customerSession")!);

            var cartItem = _context.Carts
                .FirstOrDefault(c => c.CartId == cartId && c.CustomerId == customerId && c.CartStatus == 1);

            if (cartItem == null)
                return NotFound();

            cartItem.ProductQuantity = quantity;
            _context.SaveChanges();
            return RedirectToAction("ViewCart");
        }

        // ✅ Remove from Cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromCart(int cartId)
        {
            var guard = RedirectToLoginIfUnauthorized();
            if (guard != null) return guard;

            int customerId = int.Parse(HttpContext.Session.GetString("customerSession")!);

            var cartItem = _context.Carts
                .FirstOrDefault(c => c.CartId == cartId && c.CustomerId == customerId && c.CartStatus == 1);

            if (cartItem != null)
            {
                _context.Carts.Remove(cartItem);
                _context.SaveChanges();
            }

            return RedirectToAction("ViewCart");
        }

        // ✅ Get Cart Count (for AJAX badge)
        [HttpGet]
        public IActionResult GetCartCount()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("customerSession")))
                return Json(new { count = 0 });

            int customerId = int.Parse(HttpContext.Session.GetString("customerSession")!);

            int count = _context.Carts
                .Where(c => c.CustomerId == customerId && c.CartStatus == 1)
                .Sum(c => c.ProductQuantity);

            return Json(new { count });
        }
        [HttpPost]
        public IActionResult AddToCartFromDetails(int productId, int quantity = 1)
        {
            return AddToCart(productId, quantity);
        }

        //  Helper: Simple email validation (no regex overkill)
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}