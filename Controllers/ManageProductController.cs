using Microsoft.AspNetCore.Mvc;
using Melody.Models;
using Microsoft.EntityFrameworkCore;

namespace Melody.Controllers
{
    public class ManageProductsController : Controller
    {
        private readonly MelodyDB _db;

        public ManageProductsController(MelodyDB db)
        {
            _db = db;
        }

        // ===============================
        // LIST + SEARCH
        // ===============================
        public IActionResult Index(string? search)
        {
            var query = _db.Products
                .Where(p => p.IsActive);   // ⭐ 关键

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    p.SKU.Contains(search)
                );
            }

            var products = query
                .OrderByDescending(p => p.Id)
                .ToList();

            return View("manageproduct", products);
        }


        // ===============================
        // ADD PRODUCT (GET)
        // ===============================
        [HttpGet]
        public IActionResult AddProduct()
        {
            ViewBag.Categories = _db.ProductCategories.ToList();
            return View("addproduct", new AddProductVM());
        }


        // ===============================
        // ⭐ ADD PRODUCT (POST)
        // ===============================
        [HttpPost]
        public IActionResult AddProduct(AddProductVM vm)
        {
            ViewBag.Categories = _db.ProductCategories.ToList();

            if (!ModelState.IsValid)
                return View("addproduct", vm);

            // ===== Product =====
            var product = new Product
            {
                Name = vm.Name,
                SKU = string.IsNullOrWhiteSpace(vm.SKU)
                    ? "SKU-" + Guid.NewGuid().ToString("N")[..8]
                    : vm.SKU,
                Description = vm.Description,
                Price = vm.Price,
                Brand = vm.Brand,
                CategoryId = vm.CategoryId,
                IsActive = true,

                Tags = string.Empty
            };

            // ===== Image =====
            if (vm.Image != null && vm.Image.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(vm.Image.FileName);
                var path = Path.Combine("wwwroot/images", fileName);

                using var stream = new FileStream(path, FileMode.Create);
                vm.Image.CopyTo(stream);

                product.ImageUrl = fileName;
            }

            _db.Products.Add(product);
            _db.SaveChanges();   

            // ===== Inventory =====
            var inventory = new Inventory
            {
                ProductId = product.Id,
                Quantity = vm.Quantity < 0 ? 0 : vm.Quantity,
                MinimumStock = vm.MinimumStock < 0 ? 0 : vm.MinimumStock,
                MaximumStock = vm.MaximumStock < 0 ? 0 : vm.MaximumStock,
                LastRestocked = DateTime.UtcNow
            };

            _db.Inventories.Add(inventory);
            _db.SaveChanges();

            TempData["Toast"] = "✅ Product added successfully";
            return RedirectToAction("Index");
        }


        // ===============================
        // UPDATE PRODUCT (GET)
        // ===============================
        [HttpGet]
        public IActionResult UpdateProduct(int id)
        {
            var product = _db.Products.Find(id);
            if (product == null) return NotFound();

            ViewBag.Categories = _db.ProductCategories.ToList();
            ViewBag.Inventory = _db.Inventories.FirstOrDefault(i => i.ProductId == id);

            return View("updateproduct", product);
        }

        // ===============================
        // UPDATE PRODUCT (POST)
        // ===============================
        [HttpPost]
        public IActionResult UpdateProduct(Product product, Inventory inventory, IFormFile image)
        {
            var dbProduct = _db.Products.FirstOrDefault(p => p.Id == product.Id);
            if (dbProduct == null) return NotFound();

            dbProduct.Name = product.Name;
            dbProduct.SKU = product.SKU;
            dbProduct.Description = product.Description;
            dbProduct.Price = product.Price;
            dbProduct.Brand = product.Brand;
            dbProduct.CategoryId = product.CategoryId;
            dbProduct.IsActive = product.IsActive;

            if (image != null && image.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                var path = Path.Combine("wwwroot/images", fileName);

                using var stream = new FileStream(path, FileMode.Create);
                image.CopyTo(stream);

                dbProduct.ImageUrl = fileName;
            }

            var dbInventory = _db.Inventories.FirstOrDefault(i => i.ProductId == product.Id);
            if (dbInventory == null)
            {
                if (inventory == null)
                    inventory = new Inventory();

                inventory.ProductId = product.Id;
                inventory.LastRestocked = DateTime.UtcNow;
                _db.Inventories.Add(inventory);
            }
            else
            {
                dbInventory.Quantity = inventory.Quantity;
                dbInventory.MinimumStock = inventory.MinimumStock;
                dbInventory.MaximumStock = inventory.MaximumStock;
                dbInventory.LastRestocked = DateTime.UtcNow;
            }

            _db.SaveChanges();

            TempData["Toast"] = "✏️ Product updated successfully";
            return RedirectToAction("Index");
        }

        // ===============================
        // DELETE PRODUCT (AJAX)
        // ===============================
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var product = _db.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
                return Json(new { success = false });

            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;

            _db.SaveChanges();

            return Json(new { success = true });
        }



        // ================AJAX===============
        [HttpGet]
        public IActionResult AjaxList(string? search)
        {
            var query = _db.Products
                .Where(p => p.IsActive);   

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    p.SKU.Contains(search)
                );
            }

            var products = query
                .OrderByDescending(p => p.Id)
                .ToList();

            return PartialView("_ManageProductTable", products);
        }


    }
}
