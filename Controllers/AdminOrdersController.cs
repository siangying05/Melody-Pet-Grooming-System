using Microsoft.AspNetCore.Mvc;
using Melody.Models;

namespace Melody.Controllers
{
    public class AdminOrdersController : Controller
    {
        private readonly MelodyDB _db;

        public AdminOrdersController(MelodyDB db)
        {
            _db = db;
        }

        // ============================
        // INDEX (WITH STATUS FILTER)
        // ============================
        public IActionResult Index(string? status)
        {
            var ordersQuery = _db.Orders.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                ordersQuery = ordersQuery.Where(o => o.Status == status);
            }

            var orders = ordersQuery
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            // 傳目前選中的狀態給 View
            ViewBag.CurrentStatus = status;

            return View(orders);
        }

        // ============================
        // DETAILS
        // ============================
        public IActionResult Details(int id)
        {
            var order = _db.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return NotFound();

            var items = _db.OrderItems
                .Where(i => i.OrderId == id)
                .ToList();

            return View(new OrderDetailsVM
            {
                Order = order,
                Items = items
            });
        }

        // ============================
        // UPDATE STATUS
        // ============================
        [HttpPost]
        public IActionResult UpdateStatus(int id, string status)
        {
            var order = _db.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return NotFound();

            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;
            _db.SaveChanges();

            return RedirectToAction("Details", new { id });
        }

        // ============================
        // APPROVE REFUND
        // ============================
        [HttpPost]
        public IActionResult ApproveRefund(int id)
        {
            var order = _db.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return NotFound();

            order.Status = "Refunded";
            order.UpdatedAt = DateTime.UtcNow;
            _db.SaveChanges();

            return RedirectToAction("Details", new { id });
        }

        // ============================
        // REJECT REFUND
        // ============================
        [HttpPost]
        public IActionResult RejectRefund(int id, string reason)
        {
            var order = _db.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return NotFound();

            order.Status = "RefundRejected";
            order.Notes = reason;
            order.UpdatedAt = DateTime.UtcNow;
            _db.SaveChanges();

            return RedirectToAction("Details", new { id });
        }

        // =============AJAX===============
        [HttpGet]
        public IActionResult AjaxList(string? status)
        {
            var ordersQuery = _db.Orders.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                ordersQuery = ordersQuery.Where(o => o.Status == status);
            }

            var orders = ordersQuery
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            return PartialView("_AdminOrdersTable", orders);
        }

    }
}
