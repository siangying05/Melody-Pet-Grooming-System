using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Melody.Models;

namespace Melody.Controllers;

public class HomeController : Controller
{
    private readonly MelodyDB db;
    private readonly Helper hp;

    public HomeController(MelodyDB db, Helper hp)
    {
        this.db = db;
        this.hp = hp;
    }

    // =========================
    // Dashboard
    // =========================
    public IActionResult Index()
    {
        return View();
    }
    public IActionResult About()
    {
        return View();
    }
}