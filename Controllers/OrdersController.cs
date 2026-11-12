using Microsoft.AspNetCore.Mvc;
using Melody.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;


namespace Melody.Controllers
{
    public class OrdersController : Controller
    {
        private readonly MelodyDB _db;

        public OrdersController(MelodyDB db)
        {
            _db = db;
        }

        // ===============================
        // PLACE ORDER
        // ===============================
        [HttpPost]
        public IActionResult PlaceOrder(CheckoutVM vm)
        {
            // 1️⃣ Get cart from session
            var json = HttpContext.Session.GetString(SessionKeys.Cart);
            if (string.IsNullOrEmpty(json))
                return RedirectToAction("Index", "Cart");

            var cartItems = JsonSerializer.Deserialize<List<CartVM>>(json)
                            ?? new List<CartVM>();

            if (!cartItems.Any())
                return RedirectToAction("Index", "Cart");

            var email = User.Identity?.Name ?? "guest@melody.com";

            // ===============================
            // ⭐ Check inventory first
            // ===============================
            foreach (var item in cartItems)
            {
                var inventory = _db.Inventories
                    .FirstOrDefault(i => i.ProductId == item.ProductId);

                if (inventory == null || inventory.Quantity < item.Quantity)
                {
                    TempData["Error"] = "❌ Some items are out of stock.";
                    return RedirectToAction("Index", "Cart");
                }
            }

            // ===============================
            // 2️⃣ Create Order  ⭐ Notes 已修正
            // ===============================
            var order = new Order
            {
                OrderNumber = "ORD-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                CustomerEmail = email,
                Subtotal = cartItems.Sum(x => x.Total),
                Tax = 0,
                Total = cartItems.Sum(x => x.Total),
                Status = "Pending",
                ShippingAddress = $"{vm.AddressLine}, {vm.Postcode}, {vm.City}, {vm.State}",
                ShippingPhone = vm.Phone,
                Notes = "",                     
                CreatedAt = DateTime.UtcNow
            };

            _db.Orders.Add(order);
            _db.SaveChanges();

            // ===============================
            // 3️⃣ Create OrderItems + deduct inventory
            // ===============================
            foreach (var item in cartItems)
            {
                _db.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price
                });

                var inventory = _db.Inventories
                    .First(i => i.ProductId == item.ProductId);

                inventory.Quantity -= item.Quantity;
            }

            _db.SaveChanges();

            // ===============================
            // 4️⃣ Clear cart
            // ===============================
            HttpContext.Session.Remove(SessionKeys.Cart);

            // ===============================
            // 5️⃣ Redirect to checkout success
            // ===============================
            return RedirectToAction("Index", "Checkout", new { success = true });
        }

        // ===============================
        // MY ORDERS
        // ===============================
        public IActionResult MyOrders(string status)
        {
            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login", "Account");

            var query = _db.Orders
                .Where(o => o.CustomerEmail == email)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var orders = query
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            ViewBag.CurrentStatus = status;

            return View(orders);
        }


        // ===============================
        // ORDER DETAILS
        // ===============================
        public IActionResult Details(int id)
        {
            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login", "Account");

            var order = _db.Orders
                .FirstOrDefault(o => o.Id == id && o.CustomerEmail == email);

            if (order == null)
                return NotFound();

            var items = _db.OrderItems
                .Include(i => i.Product)  
                .Where(i => i.OrderId == id)
                .ToList();

            return View(new OrderDetailsVM
            {
                Order = order,
                Items = items
            });
        }

        // ===============================
        // REQUEST REFUND
        // ===============================
        [HttpPost]
        public IActionResult RequestRefund(RefundVM vm)
        {
            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login", "Account");

            var order = _db.Orders
                .FirstOrDefault(o => o.Id == vm.OrderId && o.CustomerEmail == email);

            if (order == null)
                return NotFound();

            order.Status = "Refunding";
            order.Notes = vm.Reason ?? "";    
            order.UpdatedAt = DateTime.UtcNow;

            _db.SaveChanges();

            return RedirectToAction("Details", new { id = vm.OrderId });
        }

        // =============AJAX==================
        [HttpGet]
        public IActionResult AjaxList(string status)
        {
            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var query = _db.Orders
                .Where(o => o.CustomerEmail == email)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var orders = query
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            return PartialView("_OrdersTable", orders);
        }

    }
}


