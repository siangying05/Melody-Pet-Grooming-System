using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Fonts;

namespace Melody.Controllers;

public class AccountController : Controller
{
    private Random rand = new Random();
    private readonly MelodyDB db;
    private readonly Helper hp;

    public AccountController(MelodyDB db, Helper hp)
    {
        this.db = db;
        this.hp = hp;
    }

    // GET: Account/Login
    public IActionResult Login()
    {
        return View(new LoginVM());
    }


    // POST: Account/Login
    [HttpPost]
    public IActionResult Login(LoginVM vm, string? returnURL, string captcha)
    {
        var code = HttpContext.Session.GetString("CaptchaCode");

        if (string.IsNullOrEmpty(captcha) || captcha.ToUpper() != code)
        {
            TempData["Error"] = "Invalid CAPTCHA";
            return View(vm);
        }
        var u = db.Users.Find(vm.Email);
        if (u == null)
        {
            TempData["Error"] = "Login credentials not matched.";
            return View(vm);
        }

        if (u.LockoutEnd != null && u.LockoutEnd > DateTime.Now)
        {
            TempData["Error"] = $"Account temporarily locked. Try again at {u.LockoutEnd.Value:HH:mm:ss}";
            return View(vm);
        }

        if (!hp.VerifyPassword(u.Hash, vm.Password))
        {
            u.FailedLoginAttempts++; 

            if (u.FailedLoginAttempts >= 3)
            {
                u.LockoutEnd = DateTime.Now.AddMinutes(5);
                u.FailedLoginAttempts = 0; 
            }

            db.SaveChanges();

            TempData["Error"] = "Login credentials not matched.";
            return View(vm);
        }

        u.FailedLoginAttempts = 0;
        u.LockoutEnd = null;
        db.SaveChanges();

        TempData["Info"] = "Login successfully.";
        hp.SignIn(u.Email, u.Role, vm.RememberMe);

        if (!string.IsNullOrEmpty(returnURL))
            return Redirect(returnURL);

        if (u.Role == "Admin")
            return RedirectToAction("Dashboard", "Admin");

        return RedirectToAction("Index", "Home");
    }

    // GET: Account/Logout
    public IActionResult Logout(string? returnURL)
    {
        TempData["Info"] = "Logout successfully.";

        // Sign out
        hp.SignOut();

        return RedirectToAction("Index", "Home");
    }

    // GET: Account/AccessDenied
    public IActionResult AccessDenied(string? returnURL)
    {
        return View();
    }



    // ------------------------------------------------------------------------
    // Others
    // ------------------------------------------------------------------------

    // GET: Account/CheckEmail
    public bool CheckEmail(string email)
    {
        return !db.Users.Any(u => u.Email == email);
    }

    // GET: Account/Register
    public IActionResult Register()
    {
        return View();
    }

    // POST: Account/Register
    [HttpPost]
    public IActionResult Register(RegisterVM vm)
    {
        if (ModelState.IsValid("Email") &&
            db.Users.Any(u => u.Email == vm.Email))
        {
            ModelState.AddModelError("Email", "Duplicated Email.");
        }

        if (ModelState.IsValid("Photo"))
        {
            var err = hp.ValidatePhoto(vm.Photo);
            if (err != "") ModelState.AddModelError("Photo", err);
        }

        if (ModelState.IsValid)
        {
            // Insert member
            db.Members.Add(new()
            {
                Email = vm.Email,
                Hash = hp.HashPassword(vm.Password),
                Name = vm.Name,
                PhotoURL = hp.SavePhoto(vm.Photo, "photos"),
            });
            db.SaveChanges();

            TempData["Info"] = "Register successfully. Please login.";
            return RedirectToAction("Login","Account");
        }

        return View(vm);
    }

    // GET: Account/UpdatePassword
    [Authorize]
    public IActionResult UpdatePassword()
    {
        return View();
    }

    // POST: Account/UpdatePassword
    [Authorize]
    [HttpPost]
    public IActionResult UpdatePassword(UpdatePasswordVM vm)
    {
        // Get user (admin or member) record based on email (PK)
        var u = db.Users.Find(User.Identity!.Name);
        if (u == null) return RedirectToAction("Index", "Home");

        // If current password not matched
        if (!hp.VerifyPassword(u.Hash, vm.Current))
        {
            ModelState.AddModelError("Current", "Current Password not matched.");
        }
        if (hp.VerifyPassword(u.Hash, vm.New))
        {
            ModelState.AddModelError("New", "New password cannot be the same as current password.");
        }

        if (ModelState.IsValid)
        {
            // Update user password (hash)
            u.Hash = hp.HashPassword(vm.New);
            db.SaveChanges();

            TempData["Info"] = "Password updated.";
            return RedirectToAction();
        }

        return View();
    }

    [Authorize(Roles = "Member")]
    public IActionResult UpdateProfile()
    {
        var email = User.Identity!.Name;

        var m = db.Members.Find(email);
        if (m == null)
            return RedirectToAction("Index", "Home");

        var vm = new MyUpdateProfileVM
        {
            Email = m.Email,
            Name = m.Name,
            Phone = m.Phone,
            Address = m.Address
        };

        return View(vm);
    }

    // POST: Account/UpdateProfile
    [Authorize(Roles = "Member")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateProfile(MyUpdateProfileVM vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var email = User.Identity!.Name;
        var m = db.Members.Find(email);

        if (m == null)
            return RedirectToAction("Index", "Home");

        m.Name = vm.Name;
        m.Phone = vm.Phone;
        m.Address = vm.Address;

        db.SaveChanges();

        TempData["Info"] = "Profile updated successfully!";
        return RedirectToAction("Profile");
    }

    // GET: Account/ResetPassword
    public IActionResult ResetPassword()
    {
        return View();
    }

    // POST: Account/ResetPassword
    [HttpPost]
    public IActionResult ResetPassword(ResetPasswordVM vm)
    {
        var u = db.Users.Find(vm.Email);

        if (ModelState.IsValid && u != null)
        {
            string password = hp.RandomPassword();
            u.Hash = hp.HashPassword(password);
            db.SaveChanges();

            ViewBag.ResetPassword = password;
            ViewBag.Email = vm.Email;

            return View("ResetPasswordSuccess");
        }

        return View(vm);
    }

    // GET: Account/Profile
    [Authorize]
    public IActionResult Profile()
    {
        var email = User.Identity!.Name;

        var vm = new ProfileVM();

        if (User.IsInRole("Admin"))
        {
            var admin = db.Admins.Find(email);
            if (admin == null) return RedirectToAction("Index", "Home");

            vm.Email = admin.Email;
            vm.Name = admin.Name;
            vm.Phone = admin.Phone;
            vm.PhotoURL = "/images/default-avatar.png";
            vm.Role = "Admin";

            return View(vm);
        }

        var member = db.Members
            .Include(m => m.Pets)
            .Include(m => m.Appointments)
                .ThenInclude(a => a.Service)
            .Include(m => m.Appointments)
                .ThenInclude(a => a.Pet)
            .Include(m => m.Orders)
            .FirstOrDefault(m => m.Email == email);

        if (member == null) return RedirectToAction("Index", "Home");

        vm.Email = member.Email;
        vm.Name = member.Name;
        vm.Phone = member.Phone;
        vm.Address = member.Address;
        vm.PhotoURL = string.IsNullOrEmpty(member.PhotoURL)
            ? "/images/default-avatar.png"
            : $"/photos/{member.PhotoURL}";
        vm.Role = "Member";
        vm.PetsCount = member.Pets?.Count ?? 0;
        vm.AppointmentsCount = member.Appointments?.Count ?? 0;
        vm.OrdersCount = member.Orders?.Count ?? 0;
        vm.Pets = member.Pets?.ToList() ?? new List<Pet>();
        vm.Appointments = member.Appointments?
            .Where(a => a.Date >= DateTime.Now && a.Status != "Cancelled")
            .OrderBy(a => a.Date)
            .Take(5)
            .ToList() ?? new List<Appointment>();
        vm.Orders = member.Orders?
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .ToList() ?? new List<Order>();

        return View(vm);
    }

    [Authorize(Roles = "Member")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UploadPhoto(IFormFile Photo)
    {
        if (Photo != null && Photo.Length > 0)
        {
            string fileName = hp.SavePhoto(Photo, "photos"); // 假设 hp.SavePhoto 返回文件名并保存到 wwwroot/photos

            var email = User.Identity!.Name;
            var member = db.Members.Find(email);
            if (member != null)
            {
                member.PhotoURL = fileName; 
                db.SaveChanges();
                TempData["Info"] = "Profile photo updated successfully!";
            }
        }
        else
        {
            TempData["Info"] = "No photo selected.";
        }

        return RedirectToAction("Profile");
    }

    public IActionResult CaptchaImage()
    {
        string captchaCode = GenerateCaptchaCode();
        HttpContext.Session.SetString("CaptchaCode", captchaCode);

        using var image = new Image<Rgba32>(120, 40);
        image.Mutate(ctx =>
        {
            ctx.Fill(Color.White);

            for (int i = 0; i < 2; i++)
            {
                ctx.DrawLine(
                    Color.LightGray,
                    1,
                    new PointF(0, Random.Shared.Next(40)),
                    new PointF(120, Random.Shared.Next(40))
                );
            }

            var font = SystemFonts.CreateFont("Arial", 20, FontStyle.Bold);
            var colors = new[] { Color.DarkSlateGray, Color.DimGray, Color.DarkBlue };

            for (int i = 0; i < captchaCode.Length; i++)
            {
                float x = 10 + i * 25 + Random.Shared.Next(-3, 4);
                float y = 8 + Random.Shared.Next(-3, 4);

                var color = colors[Random.Shared.Next(colors.Length)]
                                .WithAlpha(0.85f);

                ctx.DrawText(
                    captchaCode[i].ToString(),
                    font,
                    color,
                    new PointF(x, y)
                );
            }

            for (int i = 0; i < 60; i++)
            {
                image[
                    Random.Shared.Next(120),
                    Random.Shared.Next(40)
                ] = Color.LightGray;
            }
        });


        using var ms = new MemoryStream();
        image.SaveAsPng(ms);

        return File(ms.ToArray(), "image/png");
    }

    private static string GenerateCaptchaCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rnd = Random.Shared;

        return new string(
            Enumerable.Range(0, 4)
                      .Select(_ => chars[rnd.Next(chars.Length)])
                      .ToArray()
        );
    }


}
