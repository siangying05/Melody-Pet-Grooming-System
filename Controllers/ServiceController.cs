using Melody;
using Melody.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Controllers;

public class ServiceController : Controller
{
    private readonly MelodyDB db;

    public ServiceController(MelodyDB db)
    {
        this.db = db;
    }

    public IActionResult Services(string name, string sort, string dir, string category)
    {
        name = name?.Trim() ?? "";
        category = category?.Trim() ?? "";

        var services = db.Services.AsQueryable();

        if (!string.IsNullOrEmpty(name))
            services = services.Where(s => s.Name.Contains(name));

        if (!string.IsNullOrEmpty(category))
            services = services.Where(s => s.Category == category);

        sort = (sort ?? "name").ToLower();
        dir = (dir ?? "asc").ToLower();

        services = (sort, dir) switch
        {
            ("name", "asc") => services.OrderBy(s => s.Name),
            ("name", "desc") => services.OrderByDescending(s => s.Name),
            ("price", "asc") => services.OrderBy(s => s.Price),
            ("price", "desc") => services.OrderByDescending(s => s.Price),
            _ => services.OrderBy(s => s.Name)
        };

        ViewBag.Sort = sort;
        ViewBag.Dir = dir;
        ViewBag.Name = name;
        ViewBag.Category = category;

        ViewBag.AllCategories = db.Services.Select(s => s.Category).Distinct().OrderBy(c => c).ToList();

        var groupedServices = services
            .AsEnumerable()
            .GroupBy(s => s.Category)
            .ToDictionary(g => g.Key, g => g.ToList());

        return View(groupedServices);
    }


    // GET: Service/AddServices
    [HttpGet]
    public IActionResult AddServices()
    {
        ViewBag.AllCategories = db.Services.Select(s => s.Category).Distinct().OrderBy(c => c).ToList();
        return View();
    }

    // POST: Service/AddServices
    [HttpPost]
    public IActionResult AddServices(ServiceVM model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.AllCategories = db.Services.Select(s => s.Category).Distinct().OrderBy(c => c).ToList();
            return View(model);
        }

        var service = new Service
        {
            Name = model.Name,
            Price = model.Price,
            Duration = model.Duration,
            Category = model.Category
        };

        db.Services.Add(service);
        db.SaveChanges();

        return RedirectToAction("Services");
    }

    // GET: Service/EditServices
    [HttpGet]
    public IActionResult EditServices(int id)
    {
        var service = db.Services.Find(id);
        if (service == null) return NotFound();

        var model = new ServiceVM
        {
            Name = service.Name,
            Price = service.Price,
            Duration = service.Duration,
            Category = service.Category
        };

        ViewBag.AllCategories = db.Services.Select(s => s.Category).Distinct().OrderBy(c => c).ToList();
        return View(model);
    }

    // POST: Service/EditServices
    [HttpPost]
    public IActionResult EditServices(int id, ServiceVM model)
    {
        var service = db.Services.Find(id);
        if (service == null) return NotFound();

        service.Name = model.Name;
        service.Price = model.Price;
        service.Duration = model.Duration;
        service.Category = model.Category;

        db.SaveChanges();
        return RedirectToAction("Services");
    }


    // Post: Service/DeleteServicesPost
    [HttpPost]
    public IActionResult DeleteServicesPost(int id)
    {
        var service = db.Services.Find(id);
        if (service == null) return NotFound();

        bool hasAppointments = db.Appointments.Any(a => a.ServiceId == id);
        if (hasAppointments)
        {
            TempData["Error"] = "Cannot delete this service because there are existing appointments.";
            return RedirectToAction("Services");
        }

        db.Services.Remove(service);
        db.SaveChanges();

        TempData["SuccessMessage"] = "Service deleted successfully!";
        return RedirectToAction("Services");
    }


    // Post: Service/BookServicesPost
    [HttpPost]
    [Authorize(Roles = "Member")]
    public IActionResult BookServicesPost(AppointmentVM vm)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors)
                                           .Select(e => e.ErrorMessage)
                                           .ToList();
            TempData["ModelErrors"] = string.Join(" | ", errors);

            ViewBag.UserPets = db.Pets
                .Where(p => p.OwnerEmail == User.Identity.Name)
                .ToList();
            return View("BookServices", vm);
        }

        if (vm.Date < DateTime.Now)
        {
            ModelState.AddModelError("Date", "You cannot select a date/time after now.");
            ViewBag.UserPets = db.Pets.Where(p => p.OwnerEmail == User.Identity.Name).ToList();
            return View("BookServices", vm);
        }



        var appointment = new Appointment
{
    ServiceId = vm.ServiceId,
    CustomerEmail = User.Identity.Name,
    PetId = vm.PetId,
    Date = vm.Date.Value,
    Notes = vm.Notes,
    Status = "Pending",
    CreatedAt = DateTime.Now
};


        db.Appointments.Add(appointment);
        db.SaveChanges();

        TempData["Success"] = "Booking successful!";
        return RedirectToAction("MyAppointments", "Appointment");

    }




    [Authorize(Roles = "Member")]
    public IActionResult BookServices(int serviceId)
    {
        var service = db.Services.Find(serviceId);
        if (service == null) return NotFound();

        var userPets = db.Pets
            .Where(p => p.OwnerEmail == User.Identity.Name)
            .ToList();

        var vm = new AppointmentVM
        {
            ServiceId = service.Id,
            ServiceName = service.Name,
            Price = service.Price,
            Duration = service.Duration
        };

        ViewBag.UserPets = userPets;
        ViewBag.NoPets = !userPets.Any(); 

        return View(vm);
    }






}


