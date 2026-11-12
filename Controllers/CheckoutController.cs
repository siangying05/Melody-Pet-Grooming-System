using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Melody.Models;

namespace Melody.Controllers
{
    public class CheckoutController : Controller
    {
        public IActionResult Index(bool success = false)
        {
            ViewBag.Success = success;

            if (success)
            {
                return View("~/Views/Checkout/checkout.cshtml", new CheckoutVM());
            }

            var json = HttpContext.Session.GetString(SessionKeys.Cart);
            if (string.IsNullOrEmpty(json))
                return RedirectToAction("Index", "Cart");

            var cart = JsonSerializer.Deserialize<List<CartVM>>(json)
                       ?? new List<CartVM>();

            var vm = new CheckoutVM
            {
                Items = cart,
                GrandTotal = cart.Sum(x => x.Total)
            };

            return View("~/Views/Checkout/checkout.cshtml", vm);
        }
    }
}
