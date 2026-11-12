using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Melody.Models;

namespace Melody.Controllers;

[Authorize(Roles = "Member")]
public class PetsController : Controller
{
    private readonly MelodyDB db;

    public PetsController(MelodyDB db)
    {
        this.db = db;
    }

    // GET: Pets/Create
    public IActionResult Create(string returnUrl)
    {
        return View(new PetCreateVM
        {
            ReturnUrl = returnUrl
        });

        
    }

    // POST: Pets/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(PetCreateVM vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Failed to add pet. Please check the form.";
            return View(vm);
        }

        try
        {
            var pet = new Pet
            {
                Name = vm.Name,
                Type = vm.Type.Value,
                Breed = vm.Breed,
                Age = vm.Age,
                OwnerEmail = User.Identity!.Name
            };

            db.Pets.Add(pet);
            db.SaveChanges();

            TempData["Info"] = "Pet added successfully 🐾";
            if (!string.IsNullOrEmpty(vm.ReturnUrl))
                return Redirect(vm.ReturnUrl);

            return RedirectToAction("Profile", "Account");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to add pet. Error: {ex.Message}";
            return View(vm);
        }
    }

    public IActionResult MyPets()
    {
        var userEmail = User.Identity!.Name;

        var pets = db.Pets
            .Where(p => p.Owner.Email == userEmail)
            .ToList();

        return View(pets);
    }

    public IActionResult Edit(int id)
    {
        var pet = db.Pets.FirstOrDefault(p => p.Id == id);
        if (pet == null)
            return NotFound();

        var vm = new PetEditVM
        {
            Id = pet.Id,
            Name = pet.Name,
            Type = pet.Type,
            Breed = pet.Breed,
            Age = pet.Age,
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(PetEditVM vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var pet = db.Pets.FirstOrDefault(p => p.Id == vm.Id);
        if (pet == null)
            return NotFound();

        pet.Name = vm.Name;
        pet.Type = vm.Type;
        pet.Breed = vm.Breed;
        pet.Age = vm.Age;

        db.SaveChanges();
        TempData["Info"] = "Pet Edited successfully 🐾";
        return RedirectToAction("MyPets");
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        var pet = db.Pets.FirstOrDefault(p => p.Id == id);
        if (pet == null)
            return NotFound();

        if (pet.OwnerEmail != User.Identity!.Name)
            return Forbid();

        db.Pets.Remove(pet);
        db.SaveChanges();

        TempData["Info"] = "Pet deleted successfully 🗑️";

        return RedirectToAction("MyPets");
    }

}
