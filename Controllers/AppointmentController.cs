using Melody;
using Melody.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Melody.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly MelodyDB db;

        public AppointmentController(MelodyDB db)
        {
            this.db = db;
        }


        //GET : MyAppointments(Member)
        [Authorize(Roles = "Member")]
        public IActionResult MyAppointments()
        {
            var email = User.Identity!.Name;

            var appointments = db.Appointments
                .Include(a => a.Service)
                .Include(a => a.Pet)
                .Where(a => a.CustomerEmail == email)
                .OrderByDescending(a => a.Date)
                .Select(a => new AppointmentVM
                {
                    Id = a.Id,
                    Date = a.Date,
                    Status = a.Status,
                    Notes = a.Notes,
                    ServiceName = a.Service.Name,
                    PetName = a.Pet.Name
                })
                .ToList();

            return View("~/Views/Appointment/MyAppointments.cshtml", appointments);
        }


        //Get: editAppointment
        [Authorize(Roles = "Member")]
        public IActionResult EditAppointments(int id)
        {
            var appt = db.Appointments
                         .Include(a => a.Service)
                         .Include(a => a.Pet)
                         .FirstOrDefault(a => a.Id == id);

            if (appt == null) return NotFound();

            var vm = new AppointmentVM
            {
                Id = appt.Id,
                Date = appt.Date,
                ServiceName = appt.Service?.Name,
                PetName = appt.Pet?.Name,
            };

            return View(vm);
        }



        //Post:EditAppointment
        [HttpPost]
        [Authorize(Roles = "Member")]
        [ValidateAntiForgeryToken]
        public IActionResult EditAppointments(AppointmentVM vm)
        {
            if (vm.Date < DateTime.Today)
            {
                ModelState.AddModelError("Date", "You cannot select a date after today.");
                return View(vm);
            }

            var appt = db.Appointments.Find(vm.Id);
            if (appt == null) return NotFound();

            // update date & time
            appt.Date = vm.Date.Value;
            appt.Status = "Requesting";

            db.SaveChanges();
            return RedirectToAction("MyAppointments");
        }



        //Post: DeleteAppointment[member]
        [HttpPost]
        public IActionResult DeleteAppointment(int id)
        {
            var appointment = db.Appointments.Find(id);
            if (appointment != null)
            {
                db.Appointments.Remove(appointment);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Appointment deleted successfully!";
            }
            return RedirectToAction("MyAppointments");
        }




        //GET
        [Authorize(Roles = "Admin")]
        public IActionResult AdminViewAppointment()
        {
            var allAppointments = db.Appointments
                .Select(a => new AppointmentVM
                {
                    Id = a.Id,
                    ServiceName = a.Service.Name,
                    PetName = a.Pet.Name,
                    Date = a.Date,
                    Notes = a.Notes,
                    Status = a.Status,
                    CustomerEmail = a.CustomerEmail,
                    CustomerName = db.Users
                                  .Where(m => m.Email == a.CustomerEmail)
                                  .Select(m => m.Name)
                                  .FirstOrDefault()
                })
                .OrderBy(a => a.Date)
                .ToList();

            return View(allAppointments);
        }


        //POST: AcceptAppointment
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult AcceptAppointment(int id)
        {
            var appointment = db.Appointments.Find(id);
            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return RedirectToAction("AdminViewAppointment");
            }

            appointment.Status = "Pending";
            db.SaveChanges();

            TempData["SuccessMessage"] = "Appointment accepted and moved to Pending.";
            return RedirectToAction("AdminViewAppointment");
        }

        //POST: DeclineAppointment
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult DeclineAppointment(int id)
        {
            var appointment = db.Appointments.Find(id);
            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return RedirectToAction("AdminViewAppointment");
            }

            appointment.Status = "Declined";
            db.SaveChanges();

            TempData["SuccessMessage"] = "Appointment has been declined.";
            return RedirectToAction("AdminViewAppointment");
        }


        //GET: AdminEditAppointment
        [Authorize(Roles = "Admin")]
        public IActionResult AdminEditAppointment(int id)
        {
            var appt = db.Appointments
                .Where(a => a.Id == id)
                .Select(a => new AppointmentVM
                {
                    Id = a.Id,
                    Date = a.Date,
                    Notes = a.Notes,
                    CustomerEmail = a.CustomerEmail,
                    ServiceName = a.Service.Name,
                    PetName = a.Pet.Name
                })
                .FirstOrDefault();

            if (appt == null)
                return RedirectToAction("AllMemberAppointments");

            return View(appt);
        }



        //POST: AdminEditAppointment
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult AdminEditAppointment(Appointment apptModel)
        {
            var appt = db.Appointments.FirstOrDefault(a => a.Id == apptModel.Id);
            if (appt == null)
                return RedirectToAction("AdminViewAppointment");

            appt.Date = apptModel.Date;
            appt.Notes = apptModel.Notes ?? appt.Notes;


            db.SaveChanges();

            TempData["SuccessMessage"] = "Appointment updated successfully.";
            return RedirectToAction("AdminViewAppointment");
        }


        // GET: AdminCreateAppointment
        [Authorize(Roles = "Admin")]
        public IActionResult AdminCreateAppointment()
        {
            // Member + Pets
            var members = db.Members
                .Include(m => m.Pets)
                .Select(m => new
                {
                    Email = m.Email,
                    Name = m.Name,
                    Pets = m.Pets.Select(p => new { p.Id, p.Name, Type = p.Type.ToString() }).ToList()
                }).ToList();
            ViewBag.Members = members;

            // Services
            var services = db.Services.ToList();
            ViewBag.Services = services;

            // Categories
            ViewBag.Categories = services.Select(s => s.Category).Distinct().ToList();

            return View();
        }

        // POST: AdminCreateAppointment
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult AdminCreateAppointment(AppointmentVM model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Members = db.Members
                    .Include(m => m.Pets)
                    .Select(m => new
                    {
                        Email = m.Email,
                        Name = m.Name,
                        Pets = m.Pets.Select(p => new { p.Id, p.Name, Type = p.Type.ToString() }).ToList()
                    }).ToList();

                ViewBag.Services = db.Services.ToList();
                ViewBag.Categories = db.Services.Select(s => s.Category).Distinct().ToList();

                return View(model);
            }

            var appointment = new Appointment
            {
                CustomerEmail = model.CustomerEmail,
                PetId = model.PetId,
                ServiceId = model.ServiceId,
                Date = model.Date.Value,
                Notes = model.Notes,
                Status = "Pending"
            };

            db.Appointments.Add(appointment);
            db.SaveChanges();

            return RedirectToAction("AdminViewAppointment");
        }


        [Authorize(Roles = "Admin")]
        public IActionResult Calendar()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetAppointments()
        {
            var events = db.Appointments
                .Include(a => a.Service)
                .Include(a => a.Customer)
                .Where(a => a.Status == "Successful" || a.Status == "Pending")
                .Select(a => new
                {
                    id = a.Id,
                    title = a.Service.Name + " - " + a.Customer.Name,
                    start = a.Date,
                    extendedProps = new
                    {
                        status = a.Status,
                        customer = a.Customer.Name,
                        service = a.Service.Name,
                        duration = a.Service != null ? a.Service.Duration : 30,
                        notes = a.Notes
                    }
                })
                .ToList();

            return Json(events);
        }




    }

}
