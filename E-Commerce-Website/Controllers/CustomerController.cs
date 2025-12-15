using E_Commerce_Website.Models;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Build.Tasks.Deployment.Bootstrapper;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce_Website.Controllers
{
    public class CustomerController : Controller
    {
        private readonly myContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public CustomerController(myContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            List<Category> categories = _context.Categories.ToList();
            ViewData["Categories"] = categories;

            var products = _context.Products
                            .Include(p => p.Category)
                            .ToList();
            ViewData["Products"] = products;

            ViewBag.checkSession = HttpContext.Session.GetString("customerSession");
            return View();
        }

        public IActionResult CustomerLogin()
        {
            return View();
        }
        [HttpPost]
        public IActionResult CustomerLogin(string customerEmail, string customerPassword)
        {
            var customer = _context.Customers.FirstOrDefault(e => e.CustomerEmail == customerEmail);
            if (customer != null && customer.CustomerPassword == customerPassword)
            {
                HttpContext.Session.SetString("customerSession", customer.CustomerId.ToString());
                return RedirectToAction("Index");
            }

            ViewBag.message = "Incorrect username or password";
            return View("CustomerLogin");
        }
        public IActionResult CustomerRegistration(Customer customer)
        {
            _context.Customers.Add(customer);
            _context.SaveChanges();
            return RedirectToAction("CustomerLogin");
        }
        public IActionResult CustomerLogout()
        {
            HttpContext.Session.Remove("customerSession");
            return RedirectToAction("Index");
        }
        public IActionResult CustomerProfile()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("customerSession")))
            {
                return RedirectToAction("CustomerLogin");
            }
            HttpContext.Session.GetString("customerSession");
            int customerId = Convert.ToInt32(HttpContext.Session.GetString("customerSession"));
            var customer = _context.Customers.Find(customerId);
            return View(customer);
        }
        [HttpPost]
        public IActionResult CustomerProfile(Customer customer)
        {
            _context.Customers.Update(customer);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> ChangeProfileImage(int customerId, IFormFile customerImage)
        {
            var customer = _context.Customers.Find(customerId);
            if (customer == null)
                return NotFound();

            if (customerImage != null && customerImage.Length > 0)
            {
                string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "CustomerImage");

                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                string uniqueFileName = Guid.NewGuid() + Path.GetExtension(customerImage.FileName);
                string filePath = Path.Combine(uploadFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await customerImage.CopyToAsync(fileStream);
                }

                customer.CustomerImage = uniqueFileName;
            }

            _context.SaveChanges();
            return RedirectToAction("CustomerProfile");
        }

        public IActionResult Feedback()
        {
            return View();
        }


        [HttpPost]
        public IActionResult Feedback(Feedback feedback)
        {
            if (ModelState.IsValid)
            {
                _context.Feedbacks.Add(feedback);
                _context.SaveChanges();


                TempData["Message"] = $"Thank you, {feedback.UserName}! Your feedback has been received 💖";
                return RedirectToAction("Feedback");
            }


            return View(feedback);
        }
        public IActionResult fetchAllProducts()
        {
            var products = _context.Products.ToList();
            return View(products);
        }
        // Add product to cart
        [HttpPost]
        public IActionResult AddToCart(int productId, int quantity = 1)
        {
            // Check if customer is logged in
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("customerSession")))
            {
                return RedirectToAction("CustomerLogin");
            }

            int customerId = Convert.ToInt32(HttpContext.Session.GetString("customerSession"));

            // Check if product already exists in cart
            var existingCartItem = _context.Carts
                .FirstOrDefault(c => c.CustomerId == customerId && c.ProductId == productId && c.CartStatus == 1);

            if (existingCartItem != null)
            {
                // Update quantity if product already in cart
                existingCartItem.ProductQuantity += quantity;
                _context.SaveChanges();
            }
            else
            {
                // Add new cart item
                var cartItem = new Cart
                {
                    ProductId = productId,
                    CustomerId = customerId,
                    ProductQuantity = quantity,
                    CartStatus = 1 // 1 = active, 0 = ordered/removed
                };
                _context.Carts.Add(cartItem);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // View cart contents
        public IActionResult ViewCart()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("customerSession")))
            {
                return RedirectToAction("CustomerLogin");
            }

            int customerId = Convert.ToInt32(HttpContext.Session.GetString("customerSession"));

            // Get cart items with product details
            var cartItems = _context.Carts
                .Where(c => c.CustomerId == customerId && c.CartStatus == 1)
                .Include(c => c.Product)
                .ThenInclude(p => p.Category)
                .ToList();

            return View(cartItems);
        }

        // Update cart item quantity
        [HttpPost]
        public IActionResult UpdateCartItem(int cartId, int quantity)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("customerSession")))
            {
                return RedirectToAction("CustomerLogin");
            }

            var cartItem = _context.Carts.Find(cartId);
            if (cartItem != null && quantity > 0)
            {
                cartItem.ProductQuantity = quantity;
                _context.SaveChanges();
            }

            return RedirectToAction("ViewCart");
        }
        [HttpPost]


        // Remove item from cart
        [HttpPost]
        public IActionResult RemoveFromCart(int cartId)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("customerSession")))
            {
                return RedirectToAction("CustomerLogin");
            }

            var cartItem = _context.Carts.Find(cartId);
            if (cartItem != null)
            {
                _context.Carts.Remove(cartItem);
                _context.SaveChanges();
            }

            return RedirectToAction("ViewCart");
        }

        // Get cart items count for display in navigation
        [HttpGet]
        public IActionResult GetCartCount()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("customerSession")))
            {
                return Json(new { count = 0 });
            }

            int customerId = Convert.ToInt32(HttpContext.Session.GetString("customerSession"));
            int cartCount = _context.Carts
                .Where(c => c.CustomerId == customerId && c.CartStatus == 1)
                .Sum(c => c.ProductQuantity);

            return Json(new { count = cartCount });
        }
        // In CustomerController.cs
        public IActionResult ProductDetails(int id)
        {
            var product = _context.Products
                .Include(p => p.Category)
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
                return NotFound();

            ViewBag.checkSession = HttpContext.Session.GetString("customerSession");
            return View(product);
        }

        [HttpPost]
        public IActionResult AddToCartFromDetails(int productId, int quantity = 1)
        {
            return AddToCart(productId, quantity);
        }
    }

}
