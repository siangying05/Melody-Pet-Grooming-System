using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Melody.Models;

// View Models ----------------------------------------------------------------

#nullable disable warnings

public class LoginVM
{
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    public bool RememberMe { get; set; }
    public string LockoutMessage { get; set; }
}

public class RegisterVM
{
    [StringLength(100)]
    [EmailAddress]
    [Remote("CheckEmail", "Account", ErrorMessage = "Duplicated {0}.")]
    public string Email { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [Compare("Password")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    public string Confirm { get; set; }

    [StringLength(100)]
    public string Name { get; set; }

    public IFormFile Photo { get; set; }

}

public class UpdatePasswordVM
{
    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string Current { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string New { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [Compare("New")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    public string Confirm { get; set; }
}

public class MyUpdateProfileVM
{
    [Required]
    public string Email { get; set; }   

    [Required(ErrorMessage = "Name is required")]
    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(20)]
    public string Phone { get; set; }

    [MaxLength(200)]
    public string Address { get; set; }
}

public class ResetPasswordVM
{
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }
}

public class ProfileVM
{
 
    public string Email { get; set; }
    public string Name { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public string PhotoURL { get; set; }
    public string Role { get; set; }


    public int PetsCount { get; set; }
    public int AppointmentsCount { get; set; }
    public int OrdersCount { get; set; }

    public List<Pet> Pets { get; set; } = new List<Pet>();
    public List<Appointment> Appointments { get; set; } = new List<Appointment>();
    public List<Order> Orders { get; set; } = new List<Order>();
}

public class UserViewModel
{
    [Required, MaxLength(100)]
    public string Email { get; set; }

    [MaxLength(100)]
    [DataType(DataType.Password)]
    public string Password { get; set; } 

    [Required, MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(20)]
    public string Phone { get; set; }

    [Required]
    public string UserType { get; set; }  

    [MaxLength(200)]
    public string Address { get; set; }
}

public class CreateUserVM
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [Required]
    [Display(Name = "Name")]
    public string Name { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; }

    [Required]
    [Display(Name = "Role")]
    public string Role { get; set; } 
}

public class EditUserVM
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } 

    [Required]
    [Display(Name = "Name")]
    public string Name { get; set; }

    [MaxLength(20)]
    public string Phone { get; set; }

    [MaxLength(200)]
    public string Address { get; set; }
    public string Role { get; set; }
}

public class PetCreateVM
{
    [Required, MaxLength(100)]
    public string Name { get; set; }

    [Required]
    public PetType? Type { get; set; }

    [Required, MaxLength(100)]
    public string Breed { get; set; }

    [Range(0, 100)]
    public int Age { get; set; }
    public string ReturnUrl { get; set; }
}

public class PetEditVM
{
    public int Id { get; set; }
    [Required]
    public string Name { get; set; }
    public PetType Type { get; set; }
    public string Breed { get; set; }
    public int Age { get; set; }
}

public class AdminDashboardVM
{
    public int UsersCount { get; set; }
    public int PetsCount { get; set; }
    public int AppointmentsCount { get; set; }
    public int OrdersCount { get; set; }

    public List<Appointment> RecentAppointments { get; set; }
    public List<Order> RecentOrders { get; set; }
}



public class ServiceVM
{
    [Required]
    [StringLength(100)]
    [Display(Name = "Service Name")]
    public string Name { get; set; }

    [Required]
    [Range(0.01, 10000)]
    [Display(Name = "Price (RM)")]
    public decimal Price { get; set; }

    [Required]
    [Range(1, 1440)]
    [Display(Name = "Duration (minutes)")]
    public int Duration { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Category")]
    public string Category { get; set; }

    public int Id { get; set; }
}


public class AppointmentVM
{
    [Required]
    public int ServiceId { get; set; }

    [Required]
    [Display(Name = "Pet")]
    public int PetId { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime? Date { get; set; }  // nullable


    [StringLength(500)]
    public string? Notes { get; set; }

    public decimal Price { get; set; }
    public int Duration { get; set; }
    public string? ServiceName { get; set; }
    public string? PetName { get; set; }
    public string? Status { get; set; }

    public int? Id { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
}


public class AppointmentCreateVM
{
    public string CustomerEmail { get; set; }
    public int? PetId { get; set; }
    public int? ServiceId { get; set; }
    public string SelectedCategory { get; set; }
    public DateTime? Date { get; set; }
    public string Notes { get; set; }

    public List<ProfileVM> Members { get; set; } = new List<ProfileVM>();
    public List<Pet> Pets { get; set; } = new List<Pet>();
    public List<ServiceVM> Services { get; set; } = new List<ServiceVM>();
    public List<string> Categories { get; set; } = new List<string>();
}




public class ProductPageVM
{
    // ===== Category =====
    public List<ProductCategory> Categories { get; set; } = [];
    public int? SelectedCategoryId { get; set; }

    // ===== Products =====
    public List<Product> Products { get; set; } = [];

    // ===== Search =====
    public string? Search { get; set; }

    // ===== Pagination =====
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
}

public class OrderDetailsVM
{
    public Order Order { get; set; }
    public List<OrderItem> Items { get; set; }
}


public class AddProductVM
{
    // ===== Product =====
    [Required]
    public string Name { get; set; }

    public string? SKU { get; set; }

    public string? Description { get; set; }

    [Required]
    public decimal Price { get; set; }

    public string? Brand { get; set; }

    [Required]
    public int CategoryId { get; set; }

    // ===== Inventory =====
    public int Quantity { get; set; }
    public int MinimumStock { get; set; }
    public int MaximumStock { get; set; }

    // ===== Image =====
    [Required(ErrorMessage = "Product image is required")]
    public IFormFile? Image { get; set; }
}

public class CheckoutVM
{
    // ===== Customer Info =====
    [Required(ErrorMessage = "Full name is required")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required")]
    public string Phone { get; set; } = string.Empty;

    // ===== Address =====
    [Required(ErrorMessage = "Address line is required")]
    public string AddressLine { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Postcode is required")]
    public string Postcode { get; set; } = string.Empty;

    [Required(ErrorMessage = "State is required")]
    public string State { get; set; } = string.Empty;

    // ===== Payment =====
    [Required(ErrorMessage = "Please select a payment method")]
    public string PaymentMethod { get; set; } = string.Empty;

    // ===== Cart Items =====
    public List<CartVM> Items { get; set; } = [];

    // ===== Total =====
    public decimal GrandTotal { get; set; }


}

public class CartVM
{
    public int MaxStock { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public string ImageUrl { get; set; }

    public decimal Total => Price * Quantity;
}

public class RefundVM
{
    public int OrderId { get; set; }

    [Required(ErrorMessage = "Please enter refund reason")]
    public string Reason { get; set; }

    public string? Status { get; set; }
}
