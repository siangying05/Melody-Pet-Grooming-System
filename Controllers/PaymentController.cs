using Microsoft.AspNetCore.Mvc;
using Melody.Models;
using System.Text.Json;

namespace Melody.Controllers
{
    public class PaymentController : Controller
    {
        private readonly MelodyDB _db;

        public PaymentController(MelodyDB db)
        {
            _db = db;
        }

        // ================================
        // Checkout → Payment
        // ================================
        [HttpPost]
        public IActionResult Payment(CheckoutVM data)
        {
            // 把 Checkout 数据暂存在 Session
            HttpContext.Session.SetString(
                "CheckoutData",
                JsonSerializer.Serialize(data)
            );

            return View("~/Views/Checkout/payment.cshtml", data);
        }

        // ================================
        // Payment → Submit Order (写 DB)
        // ================================
        [HttpPost]
        public IActionResult Submit()
        {
            // ===== 1. Cart =====
            var cartJson = HttpContext.Session.GetString(SessionKeys.Cart);
            if (string.IsNullOrEmpty(cartJson))
                return RedirectToAction("Index", "Cart");

            var cartItems = JsonSerializer.Deserialize<List<CartVM>>(cartJson)!;

            // ===== 2. CheckoutData =====
            var checkoutJson = HttpContext.Session.GetString("CheckoutData");
            if (string.IsNullOrEmpty(checkoutJson))
                return RedirectToAction("Index", "Checkout");

            var checkout = JsonSerializer.Deserialize<CheckoutVM>(checkoutJson)!;

            var email = User.Identity!.Name!;

            // ===== 3. Recalculate total =====
            var subtotal = cartItems.Sum(i => i.Price * i.Quantity);

            var fullAddress =
                $"{checkout.AddressLine}, " +
                $"{checkout.Postcode} {checkout.City}, " +
                $"{checkout.State}";

            // ===== 4. Create Order =====
            var order = new Order
            {
                OrderNumber = "ORD-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                CustomerEmail = email,

                Subtotal = subtotal,
                Tax = 0,
                Total = subtotal,

                Status = "Pending",

                ShippingAddress = fullAddress,    
                ShippingPhone = checkout.Phone,   

                Notes = "",
                CreatedAt = DateTime.UtcNow
            };

            _db.Orders.Add(order);
            _db.SaveChanges();

            // ===== 5. OrderItems + Inventory =====
            foreach (var item in cartItems)
            {
                var inventory = _db.Inventories
                    .FirstOrDefault(i => i.ProductId == item.ProductId);

                if (inventory == null || inventory.Quantity < item.Quantity)
                    return BadRequest("Insufficient stock");

                inventory.Quantity -= item.Quantity;

                _db.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price
                });
            }

            _db.SaveChanges();

            // ===== 6. Clear session =====
            HttpContext.Session.Remove(SessionKeys.Cart);
            HttpContext.Session.Remove("CheckoutData");

            return RedirectToAction("Index", "Checkout", new { success = true });
        }


    }
}
