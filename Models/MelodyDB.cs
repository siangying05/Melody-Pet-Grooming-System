using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Melody.Models;

#nullable disable warnings

public class MelodyDB : DbContext
{
    public MelodyDB(DbContextOptions options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<Member> Members { get; set; }
    public DbSet<Pet> Pets { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<ServicePetType> ServicePetTypes { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductPetType> ProductPetTypes { get; set; }
    public DbSet<ProductCategory> ProductCategories { get; set; }
    public DbSet<Inventory> Inventories { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<ShoppingCart> ShoppingCarts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ===== User TPH =====
        modelBuilder.Entity<User>()
            .HasDiscriminator<string>("UserType")
            .HasValue<Admin>("Admin")
            .HasValue<Member>("Member");

        // ===== Appointment / Member =====
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Customer)
            .WithMany(m => m.Appointments)
            .HasForeignKey(a => a.CustomerEmail)
            .HasPrincipalKey(u => u.Email)
            .OnDelete(DeleteBehavior.Restrict);

        // ===== Pet / Member =====
        modelBuilder.Entity<Pet>()
            .HasOne(p => p.Owner)
            .WithMany(m => m.Pets)
            .HasForeignKey(p => p.OwnerEmail)
            .HasPrincipalKey(u => u.Email)
            .OnDelete(DeleteBehavior.Restrict);

        // ===== Service / PetType =====
        modelBuilder.Entity<ServicePetType>()
            .HasKey(spt => new { spt.ServiceId, spt.PetType });

        modelBuilder.Entity<Service>()
            .HasMany(s => s.ServicePetTypes)
            .WithOne(spt => spt.Service)
            .HasForeignKey(spt => spt.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ServicePetType>()
            .Property(spt => spt.PetType)
            .HasConversion<string>()
            .HasMaxLength(20);

        modelBuilder.Entity<Service>()
            .Property(s => s.Price)
            .HasPrecision(18, 2); // ✅ FIX

        // ===== Appointment links =====
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Service)
            .WithMany()
            .HasForeignKey(a => a.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Pet)
            .WithMany()
            .HasForeignKey(a => a.PetId)
            .OnDelete(DeleteBehavior.Restrict);

        // ===== Pet enum =====
        modelBuilder.Entity<Pet>()
            .Property(p => p.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        // ===== Product / Category =====
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(pc => pc.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(18, 2); // ✅ FIX

        modelBuilder.Entity<Product>()
            .Property(p => p.Cost)
            .HasPrecision(18, 2); // ✅ FIX

        // ===== Product / PetType =====
        modelBuilder.Entity<ProductPetType>()
            .HasKey(ppt => new { ppt.ProductId, ppt.PetType });

        modelBuilder.Entity<ProductPetType>()
            .Property(ppt => ppt.PetType)
            .HasConversion<string>()
            .HasMaxLength(20);

        modelBuilder.Entity<Product>()
            .HasMany(p => p.ProductPetTypes)
            .WithOne(ppt => ppt.Product)
            .HasForeignKey(ppt => ppt.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // ===== Inventory =====
        modelBuilder.Entity<Inventory>()
            .HasKey(i => i.ProductId);

        modelBuilder.Entity<Inventory>()
            .HasOne(i => i.Product)
            .WithOne(p => p.Inventory)
            .HasForeignKey<Inventory>(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // ===== Order =====
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Customer)
            .WithMany(m => m.Orders)
            .HasForeignKey(o => o.CustomerEmail)
            .HasPrincipalKey(u => u.Email)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .Property(o => o.Subtotal)
            .HasPrecision(18, 2); // ✅ FIX

        modelBuilder.Entity<Order>()
            .Property(o => o.Tax)
            .HasPrecision(18, 2); // ✅ FIX

        modelBuilder.Entity<Order>()
            .Property(o => o.Total)
            .HasPrecision(18, 2); // ✅ FIX

        // ===== OrderItem =====
        modelBuilder.Entity<OrderItem>()
            .HasKey(oi => new { oi.OrderId, oi.ProductId });

        modelBuilder.Entity<OrderItem>()
            .Property(oi => oi.UnitPrice)
            .HasPrecision(18, 2); // ✅ FIX

        // ===== ShoppingCart =====
        modelBuilder.Entity<ShoppingCart>()
            .HasOne(sc => sc.Customer)
            .WithOne(m => m.ShoppingCart)
            .HasForeignKey<ShoppingCart>(sc => sc.CustomerEmail)
            .HasPrincipalKey<Member>(m => m.Email)
            .OnDelete(DeleteBehavior.Cascade);

        // ===== CartItem =====
        modelBuilder.Entity<CartItem>()
            .HasKey(ci => new { ci.CartId, ci.ProductId });

        modelBuilder.Entity<CartItem>()
            .Property(ci => ci.UnitPrice)
            .HasPrecision(18, 2); // ✅ FIX
    }

}

public enum PetType
{
    Dog = 1,
    Cat = 2,
    Bird = 3,
    Rabbit = 4,
    Hamster = 5,
    Fish = 6,
    Reptile = 7,
    Other = 8
}

public class User
{
    [Key, MaxLength(100)]
    public string Email { get; set; }

    [Required, MaxLength(100)]
    public string Hash { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    public string Role => GetType().Name;
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutEnd { get; set; } = null;
}

public class Admin : User
{
}

public class Member : User
{
    [MaxLength(200)]
    public string Address { get; set; }

    [MaxLength(100)]
    public string PhotoURL { get; set; }

    public virtual ICollection<Pet> Pets { get; set; }
    public virtual ICollection<Appointment> Appointments { get; set; }
    public virtual ICollection<Order> Orders { get; set; }
    public virtual ShoppingCart ShoppingCart { get; set; }
}

public class Pet
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; }

    [Required]
    public PetType Type { get; set; }

    [Required, MaxLength(100)]
    public string Breed { get; set; }

    public int Age { get; set; }

    [Required, MaxLength(100)]
    public string OwnerEmail { get; set; }

    [ForeignKey("OwnerEmail")]
    public virtual Member Owner { get; set; }
}

public class Service
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; }

    [Required]
    public decimal Price { get; set; }

    public int Duration { get; set; }

    [MaxLength(100)]
    public string Category { get; set; }

    public virtual ICollection<ServicePetType> ServicePetTypes { get; set; }
}

public class ServicePetType
{
    [Key, Column(Order = 1)]
    public int ServiceId { get; set; }

    [Key, Column(Order = 2)]
    public PetType PetType { get; set; }

    [ForeignKey("ServiceId")]
    public virtual Service Service { get; set; }
}

public class Appointment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required, MaxLength(50)]
    public string Status { get; set; }

    [Required, MaxLength(100)]
    public string CustomerEmail { get; set; }

    public DateTime CreatedAt { get; set; }

    [MaxLength(500)]
    public string Notes { get; set; }

    public int? ServiceId { get; set; }

    public int? PetId { get; set; }

    [ForeignKey("CustomerEmail")]
    public virtual Member Customer { get; set; }

    [ForeignKey("ServiceId")]
    public virtual Service Service { get; set; }

    [ForeignKey("PetId")]
    public virtual Pet Pet { get; set; }
}

public class ProductCategory
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(500)]
    public string Description { get; set; }

    public int DisplayOrder { get; set; }

    public virtual ICollection<Product> Products { get; set; }
}

public class Product
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; }

    [Required, MaxLength(1000)]
    public string Description { get; set; }

    [Required]
    public decimal Price { get; set; }

    [Required]
    public decimal Cost { get; set; }

    [Required, MaxLength(100)]
    public string SKU { get; set; }

    [MaxLength(100)]
    public string Brand { get; set; }

    public int? CategoryId { get; set; }

    [MaxLength(200)]
    public string ImageUrl { get; set; }

    [MaxLength(500)]
    public string Tags { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("CategoryId")]
    public virtual ProductCategory Category { get; set; }

    public virtual ICollection<ProductPetType> ProductPetTypes { get; set; }
    public virtual Inventory Inventory { get; set; }
}

public class ProductPetType
{
    public int ProductId { get; set; }
    public PetType PetType { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; }
}

public class Inventory
{
    [Key, ForeignKey("Product")]
    public int ProductId { get; set; }

    [Required]
    public int Quantity { get; set; }

    [Required]
    public int MinimumStock { get; set; }

    [Required]
    public int MaximumStock { get; set; }

    public DateTime LastRestocked { get; set; }
    public DateTime? NextRestockDate { get; set; }

    public virtual Product Product { get; set; }
}

public class Order
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string OrderNumber { get; set; }

    [Required, MaxLength(100)]
    public string CustomerEmail { get; set; }

    [Required]
    public decimal Subtotal { get; set; }

    [Required]
    public decimal Tax { get; set; }

    [Required]
    public decimal Total { get; set; }

    [Required, MaxLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Processing, Shipped, Delivered, Cancelled

    [Required, MaxLength(200)]
    public string ShippingAddress { get; set; }

    [MaxLength(20)]
    public string ShippingPhone { get; set; }

    [MaxLength(500)]
    public string Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("CustomerEmail")]
    public virtual Member Customer { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; }
}

public class OrderItem
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }

    [Required]
    public int Quantity { get; set; }

    [Required]
    public decimal UnitPrice { get; set; }

    [ForeignKey("OrderId")]
    public virtual Order Order { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; }
}

public class ShoppingCart
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string CustomerEmail { get; set; }

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    [ForeignKey("CustomerEmail")]
    public virtual Member Customer { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; }
}

public class CartItem
{
    public int CartId { get; set; }
    public int ProductId { get; set; }

    [Required]
    public int Quantity { get; set; }

    [Required]
    public decimal UnitPrice { get; set; }

    [ForeignKey("CartId")]
    public virtual ShoppingCart ShoppingCart { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; }
}