using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using Melody.Models;
using Melody;

namespace Melody.Controllers
{
    public class CartController : Controller
    {
        private readonly MelodyDB _db;

        public CartController(MelodyDB db)
        {
            _db = db;
        }

        // ===============================
        // CART PAGE
        // ===============================
        public IActionResult Index()
        {
            var json = HttpContext.Session.GetString(SessionKeys.Cart);
            var cart = string.IsNullOrEmpty(json)
                ? new List<CartVM>()
                : JsonSerializer.Deserialize<List<CartVM>>(json) ?? new List<CartVM>();

            return View("cart", cart);
        }

        // ===============================
        // ADD TO CART  ⭐ 必须登录
        // ===============================
        // ADD TO CART
        // ===============================
        [HttpPost]
        [Authorize]
        public IActionResult Add(int productId)
        {
            // ⭐ AJAX + 未登录 → 回 401（前端处理 redirect）
            if (!User.Identity!.IsAuthenticated &&
                Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Unauthorized();
            }

            // ===== Get cart =====
            var json = HttpContext.Session.GetString(SessionKeys.Cart);
            var cart = string.IsNullOrEmpty(json)
                ? new List<CartVM>()
                : JsonSerializer.Deserialize<List<CartVM>>(json)!;

            // ===== Get product =====
            var product = _db.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null)
                return NotFound();

            // ⭐⭐ Get inventory (关键)
            var inventory = _db.Inventories
                .FirstOrDefault(i => i.ProductId == product.Id);

            int maxStock = inventory?.Quantity ?? 0;

            // ===== Add / Update cart item =====
            var item = cart.FirstOrDefault(x => x.ProductId == productId);

            if (item != null)
            {
                // 🚫 不允许超过库存
                if (item.Quantity < item.MaxStock)
                {
                    item.Quantity++;
                }
            }
            else
            {
                // 🚫 没库存就不加
                if (maxStock <= 0)
                    return BadRequest("Out of stock");

                cart.Add(new CartVM
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = 1,
                    ImageUrl = product.ImageUrl,
                    MaxStock = maxStock   // ⭐⭐ 就是这一行
                });
            }

            // ===== Save cart =====
            HttpContext.Session.SetString(
                SessionKeys.Cart,
                JsonSerializer.Serialize(cart)
            );

            return Ok();
        }




        // ===============================
        // UPDATE QUANTITY (AJAX)
        // ===============================
        [HttpPost]
        [Authorize]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            if (quantity < 1) quantity = 1;

            // 🔍 查库存
            var inventory = _db.Inventories
                .FirstOrDefault(i => i.ProductId == productId);

            if (inventory == null)
                return Json(new { success = false, message = "Inventory not found" });

            // 🚫 超过库存 → 强制限制
            if (quantity > inventory.Quantity)
                quantity = inventory.Quantity;

            var json = HttpContext.Session.GetString(SessionKeys.Cart);
            if (string.IsNullOrEmpty(json))
                return Json(new { success = false });

            var cart = JsonSerializer.Deserialize<List<CartVM>>(json) ?? new();
            var item = cart.FirstOrDefault(x => x.ProductId == productId);

            if (item == null)
                return Json(new { success = false });

            item.Quantity = quantity;

            HttpContext.Session.SetString(
                SessionKeys.Cart,
                JsonSerializer.Serialize(cart)
            );

            return Json(new
            {
                success = true,
                productId = productId,
                quantity = item.Quantity,
                itemTotal = item.Total,
                grandTotal = cart.Sum(x => x.Total),
                maxStock = inventory.Quantity   // ⭐ 给前端用
            });
        }


        // ===============================
        // REMOVE ITEM (必须登录)
        // ===============================
        [Authorize]
        public IActionResult Remove(int productId)
        {
            var json = HttpContext.Session.GetString(SessionKeys.Cart);
            if (string.IsNullOrEmpty(json))
                return RedirectToAction("Index");

            var cart = JsonSerializer.Deserialize<List<CartVM>>(json) ?? new List<CartVM>();
            cart.RemoveAll(x => x.ProductId == productId);

            HttpContext.Session.SetString(
                SessionKeys.Cart,
                JsonSerializer.Serialize(cart)
            );

            return RedirectToAction("Index");
        }

        // ===============================
        // REMOVE ITEM (AJAX)
        // ===============================
        [HttpPost]
        [Authorize]
        public IActionResult RemoveAjax(int productId)
        {
            var json = HttpContext.Session.GetString(SessionKeys.Cart);
            if (string.IsNullOrEmpty(json))
                return Json(new { success = false });

            var cart = JsonSerializer.Deserialize<List<CartVM>>(json) ?? new List<CartVM>();

            var item = cart.FirstOrDefault(x => x.ProductId == productId);
            if (item == null)
                return Json(new { success = false });

            cart.Remove(item);

            HttpContext.Session.SetString(
                SessionKeys.Cart,
                JsonSerializer.Serialize(cart)
            );

            return Json(new
            {
                success = true,
                grandTotal = cart.Sum(x => x.Total),
                isEmpty = !cart.Any()
            });
        }


        // ===============================
        // CLEAR CART (必须登录)
        // ===============================
        [Authorize]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(SessionKeys.Cart);
            return RedirectToAction("Index");
        }


    }
}
