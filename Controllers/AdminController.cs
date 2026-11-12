using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Melody.Models;

namespace Melody.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly MelodyDB db;
    private readonly Helper hp;

    public AdminController(MelodyDB db, Helper hp)
    {
        this.db = db;
        this.hp = hp;
    }

    // =========================
    // User List
    // =========================
    public async Task<IActionResult> Users()
    {
        var users = await db.Users.ToListAsync();
        return View(users);
    }

    // =========================
    // Create User
    // =========================
    public IActionResult CreateUser()
    {
        return View(new CreateUserVM());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateUser(CreateUserVM vm)
    {
        if (db.Users.Any(u => u.Email == vm.Email))
            ModelState.AddModelError("Email", "Email already exists.");

        if (!ModelState.IsValid)
            return View(vm);

        if (vm.Role == "Member")
        {
            db.Members.Add(new Member
            {
                Email = vm.Email,
                Name = vm.Name,
                Hash = hp.HashPassword(vm.Password),
                PhotoURL = "/images/default-avatar.png"
            });
        }
        else if (vm.Role == "Admin")
        {
            db.Admins.Add(new Admin
            {
                Email = vm.Email,
                Name = vm.Name,
                Hash = hp.HashPassword(vm.Password),
                Phone = ""
            });
        }
        else
        {
            ModelState.AddModelError("Role", "Invalid role selected.");
            return View(vm);
        }

        db.SaveChanges();
        TempData["Info"] = "User created successfully!";
        return RedirectToAction("Users");
    }

    // =========================
    // Edit User
    // =========================
    public IActionResult EditUser(string email)
    {
        if (string.IsNullOrEmpty(email))
            return RedirectToAction("Users");

        var member = db.Members.Find(email);
        if (member != null)
        {
            var vm = new EditUserVM
            {
                Email = member.Email,
                Name = member.Name,
                Phone = member.Phone,
                Address = member.Address,
                Role = "Member" 
            };
            return View(vm);
        }

        var admin = db.Admins.Find(email);
        if (admin != null)
        {
            var vm = new EditUserVM
            {
                Email = admin.Email,
                Name = admin.Name,
                Phone = admin.Phone,
                Role = "Admin" 
            };
            return View(vm);
        }

        TempData["Error"] = "User not found.";
        return RedirectToAction("Users");
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditUser(EditUserVM vm)
    {
        // 先从数据库找到用户
        var member = db.Members.Find(vm.Email);
        var admin = db.Admins.Find(vm.Email);

        if (admin != null)
        {
            ModelState.Remove("Address");
            vm.Role = "Admin";

            if (!ModelState.IsValid)
                return View(vm);

            admin.Name = vm.Name;
            admin.Phone = vm.Phone;

            db.SaveChanges();
            TempData["Info"] = "Admin updated successfully!";
            return RedirectToAction("Users");
        }
        else if (member != null)
        {
            vm.Role = "Member";

            if (!ModelState.IsValid)
                return View(vm);

            member.Name = vm.Name;
            member.Phone = vm.Phone;
            member.Address = vm.Address;

            db.SaveChanges();
            TempData["Info"] = "Member updated successfully!";
            return RedirectToAction("Users");
        }
        else
        {
            ModelState.AddModelError("", "User not found.");
            return View(vm);
        }
    }

    // =========================
    // Delete User
    // =========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string email)
    {
        if (string.IsNullOrEmpty(email))
            return NotFound();

        if (User.Identity!.Name == email)
        {
            TempData["Error"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(Users));
        }

        var user = await db.Users.FindAsync(email);
        if (user == null) return NotFound();

        db.Users.Remove(user);
        await db.SaveChangesAsync();

        TempData["Info"] = "User deleted successfully!";
        return RedirectToAction(nameof(Users));
    }

    public IActionResult Dashboard()
    {
        var vm = new AdminDashboardVM
        {
            UsersCount = db.Users.Count(),
            PetsCount = db.Pets.Count(),
            AppointmentsCount = db.Appointments.Count(),
            OrdersCount = db.Orders.Count(),

            RecentAppointments = db.Appointments
                .OrderByDescending(a => a.Date)
                .Take(5)
                .ToList(),

            RecentOrders = db.Orders
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToList()
        };

        return View(vm);
    }
}