using E_Commerce_Website.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.Pkcs;

namespace E_Commerce_Website.Controllers
{
    public class AdminController : Controller
    {
        private readonly myContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public AdminController(myContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            var admin_session = HttpContext.Session.GetString("admin_session");
            if (admin_session != null)
            {
                return View();
            }
            else
            {
                return RedirectToAction("Login");
            }

        }
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Login(string adminEmail, string adminPassword)
        {
            var row = _context.Admins.FirstOrDefault(a => a.AdminEmail == adminEmail);
            if (row != null && row.AdminPassword == adminPassword)
            {
                HttpContext.Session.SetString("admin_session", row.AdminId.ToString());

                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.message = "incorrect UserName or Password";
            }
            return View();
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("admin_session");
            return RedirectToAction("Login");
        }
        [HttpGet]
        public IActionResult Profile()
        {
            var adminId = HttpContext.Session.GetString("admin_session");
            var row = _context.Admins.Find(int.Parse(adminId ?? "0"));

            return View(row);
        }
        [HttpPost]
        public IActionResult Profile(Admin admin)
        {
            _context.Update(admin);
            _context.SaveChanges();
            return RedirectToAction("Profile");
        }
        [HttpPost]
        public IActionResult ChangeProfileImage(IFormFile imageFile, Admin admin)
        {
            if (imageFile == null || imageFile.Length == 0)
                return BadRequest("No file selected");


            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "AdminImage");


            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);


            string extension = Path.GetExtension(imageFile.FileName);
            string newFileName = Guid.NewGuid().ToString() + extension;


            string filePath = Path.Combine(uploadsFolder, newFileName);


            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                imageFile.CopyTo(stream);
            }


            if (!string.IsNullOrEmpty(admin.AdminImage))
            {
                string oldImagePath = Path.Combine(uploadsFolder, admin.AdminImage);
                if (System.IO.File.Exists(oldImagePath))
                    System.IO.File.Delete(oldImagePath);
            }


            admin.AdminImage = newFileName;
            _context.Admins.Update(admin);
            _context.SaveChanges();

            return RedirectToAction("Profile");
        }
        public IActionResult FetchCustomer()
        {
            return View(_context.Customers.ToList());
        }
        public IActionResult UpdateCustomer(int id)
        {
            return View(_context.Customers.Find(id));
        }
        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> UpdateCustomer(Customer customer, IFormFile customerImage)
        {

            // 1. Check if a photo was uploaded
            if (customerImage != null && customerImage.Length > 0)
            {
                // 2. Create folder path
                string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "CustomerImage");

                // 3. Ensure folder exists
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                // 4. Generate unique file name (to avoid overwriting)
                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(customerImage.FileName);

                // 5. Full path to save the image
                string filePath = Path.Combine(uploadFolder, uniqueFileName);

                // 6. Save file async
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await customerImage.CopyToAsync(fileStream);
                }

                // 7. Save file name in DB
                customer.CustomerImage = uniqueFileName;
            }

            // 8. Update customer
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();

            return RedirectToAction("FetchCustomer");
        }

        public IActionResult CustomerDetails(int id)
        {
            return View(_context.Customers.Find(id));
        }
        public IActionResult DeleteCustomer(int Id)
        {
            var customer = _context.Customers.Find(Id);
            _context.Customers.Remove(customer);
            _context.SaveChanges();
            return RedirectToAction("FetchCustomer");
        }
        public IActionResult DeletePermission(int id)
        {
            var customer = _context.Customers.Find(id);

            return View(customer);
        }
        public IActionResult FetchCategory()
        {
            return View(_context.Categories.ToList());
        }
        public IActionResult AddCategory()
        {
            return View();
        }
        [HttpPost]
        public IActionResult AddCategory(Category category)
        {
            _context.Categories.Add(category);
            _context.SaveChanges();
            return RedirectToAction("FetchCategory");
        }
        public IActionResult UpdateCategory(int id)
        {
            return View(_context.Categories.Find(id));
        }
        [HttpPost]
        public IActionResult UpdateCategory(Category category)
        {
            _context.Categories.Update(category);
            _context.SaveChanges();
            return RedirectToAction("FetchCategory");
        }
        [HttpGet]
        public IActionResult DeletePermissionCategory(int id)
        {
            return View(_context.Categories.Find(id));
        }
        [HttpPost]
        public IActionResult DeleteCategory(int id)
        {
            var category = _context.Categories.Find(id);
            _context.Categories.Remove(category);
            _context.SaveChanges();
            return RedirectToAction("FetchCategory");
        }

        public IActionResult FetchProduct()
        {
            return View(_context.Products.ToList());
        }
        public IActionResult AddProduct()
        {

            List<Category> categories = _context.Categories.ToList();
            ViewData["category"] = categories;
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddProduct(Product product, IFormFile productImage)
        {
            // 1. Check if a photo was uploaded
            if (productImage != null && productImage.Length > 0)
            {
                // 2. Create folder path
                string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "ProductImage");

                // 3. Ensure folder exists
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                // 4. Generate unique file name (to avoid overwriting)
                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(productImage.FileName);

                // 5. Full path to save the image
                string filePath = Path.Combine(uploadFolder, uniqueFileName);

                // 6. Save file async
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await productImage.CopyToAsync(fileStream);
                }

                // 7. Save file name in DB
                product.ProductImage = uniqueFileName;
            }


            // 8. Add to database and save
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return RedirectToAction("FetchProduct");
        }
        public IActionResult ProductDetails(int id)
        {
            return View(_context.Products.Include(p => p.Category).FirstOrDefault(p => p.ProductId == id));
        }
        public IActionResult DeletePermissionProduct(int id)
        {
            return View(_context.Products.Find(id));
        }
        [HttpPost]
        public IActionResult DeleteProduct(int id)
        {
            var product = _context.Products.Find(id);
            _context.Products.Remove(product);
            _context.SaveChanges();
            return RedirectToAction("FetchProduct");
        }
        public IActionResult UpdateProduct(int id)
        {
            var product = _context.Products.Find(id);
            ViewData[nameof(Category)] = _context.Categories.ToList();
            ViewBag.CategoryId = product.CategoryId;
            return View(product);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateProduct(Product product, IFormFile productImage)
        {
            // 1. Check if a photo was uploaded
            if (productImage != null && productImage.Length > 0)
            {
                // 2. Create folder path
                string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "ProductImage");

                // 3. Ensure folder exists
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                // 4. Generate unique file name (to avoid overwriting)
                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(productImage.FileName);

                // 5. Full path to save the image
                string filePath = Path.Combine(uploadFolder, uniqueFileName);

                // 6. Save file async
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await productImage.CopyToAsync(fileStream);
                }
                // 7. Save file name in DB
                product.ProductImage = uniqueFileName;
            }
            _context.Products.Update(product);

            _context.SaveChanges();
            return RedirectToAction("FetchProduct");
        }
        public IActionResult FetchFeedback()
        {
            var feedbacks = _context.Feedbacks.ToList();
            return View(feedbacks);
        }
        public IActionResult DeletePermissionFeedback(int id)
        {
            var feedback = _context.Feedbacks.Find(id);
            if (feedback == null)
            {
                return NotFound();
            }
            return View(feedback);
        }

        [HttpPost]
        public IActionResult DeleteFeedback(int id)
        {
            var feedback = _context.Feedbacks.Find(id);
            if (feedback == null)
            {
                return NotFound();
            }

            _context.Feedbacks.Remove(feedback);
            _context.SaveChanges();
            return RedirectToAction("FetchFeedback"); 
        }


    }
}