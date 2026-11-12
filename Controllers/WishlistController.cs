using Melody;
using Melody.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Melody.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly MelodyDB _db;

        public WishlistController(MelodyDB db)
        {
            _db = db;
        }

        // ===============================
        // WISHLIST PAGE
        // ===============================
        public IActionResult Index()
        {
            var json = HttpContext.Session.GetString(SessionKeys.Wishlist);

            var wishlist = string.IsNullOrEmpty(json)
                ? new List<CartVM>()
                : JsonSerializer.Deserialize<List<CartVM>>(json) ?? new();

            return View("Wishlist", wishlist);
        }

        // ===============================
        // ADD TO WISHLIST (TempData)
        // ===============================
        [HttpPost]
        public IActionResult Add(int productId, string? returnUrl)
        {
            var json = HttpContext.Session.GetString(SessionKeys.Wishlist);
            var wishlist = string.IsNullOrEmpty(json)
                ? new List<CartVM>()
                : JsonSerializer.Deserialize<List<CartVM>>(json) ?? new();

            var product = _db.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null)
            {
                TempData["Error"] = "Product not found";
                return Redirect(returnUrl ?? "/");
            }

            if (!wishlist.Any(x => x.ProductId == productId))
            {
                wishlist.Add(new CartVM
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = 1,
                    ImageUrl = product.ImageUrl
                });

                TempData["Success"] = "Added to wishlist ❤️";
            }
            else
            {
                TempData["Info"] = "Item already in wishlist";
            }

            HttpContext.Session.SetString(
                SessionKeys.Wishlist,
                JsonSerializer.Serialize(wishlist)
            );

            return Redirect(returnUrl ?? "/");
        }

        // ===============================
        // REMOVE FROM WISHLIST
        // ===============================
        [HttpGet]
        public IActionResult Remove(int productId)
        {
            var json = HttpContext.Session.GetString(SessionKeys.Wishlist);
            if (string.IsNullOrEmpty(json))
                return RedirectToAction("Index");

            var wishlist = JsonSerializer.Deserialize<List<CartVM>>(json) ?? new();
            wishlist.RemoveAll(x => x.ProductId == productId);

            HttpContext.Session.SetString(
                SessionKeys.Wishlist,
                JsonSerializer.Serialize(wishlist)
            );

            TempData["Success"] = "Removed from wishlist";

            return RedirectToAction("Index");
        }

        // ===============================
        // MOVE TO CART (TempData)
        // ===============================
        [HttpPost]
        public IActionResult MoveToCart(int productId)
        {
            // ---- Cart ----
            var cartJson = HttpContext.Session.GetString(SessionKeys.Cart);
            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<CartVM>()
                : JsonSerializer.Deserialize<List<CartVM>>(cartJson) ?? new();

            var product = _db.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null)
            {
                TempData["Error"] = "Product not found";
                return RedirectToAction("Index");
            }

            var item = cart.FirstOrDefault(x => x.ProductId == productId);
            if (item != null)
                item.Quantity++;
            else
                cart.Add(new CartVM
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = 1,
                    ImageUrl = product.ImageUrl
                });

            HttpContext.Session.SetString(
                SessionKeys.Cart,
                JsonSerializer.Serialize(cart)
            );

            // ---- Remove from Wishlist ----
            var wishJson = HttpContext.Session.GetString(SessionKeys.Wishlist);
            if (!string.IsNullOrEmpty(wishJson))
            {
                var wishlist = JsonSerializer.Deserialize<List<CartVM>>(wishJson) ?? new();
                wishlist.RemoveAll(x => x.ProductId == productId);

                HttpContext.Session.SetString(
                    SessionKeys.Wishlist,
                    JsonSerializer.Serialize(wishlist)
                );
            }

            TempData["Success"] = "Moved item to cart 🛒";

            return RedirectToAction("Index", "Cart");
        }
    }
}
