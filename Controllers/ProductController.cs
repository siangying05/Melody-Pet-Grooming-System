using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Melody.Controllers;

[Route("Shop")]
public class ProductController : Controller
{
    private readonly MelodyDB db;

    public ProductController(MelodyDB db)
    {
        this.db = db;
    }

    // ===== Product List =====
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        int pageSize = 8;

        var categories = db.ProductCategories
            .OrderBy(c => c.DisplayOrder)
            .ToList();

        // ===== 1️⃣ 取得 Member 的 PetType（enum）=====
        var email = User.Identity?.Name;

        List<PetType> memberPetTypes = new();

        if (!string.IsNullOrEmpty(email))
        {
            memberPetTypes = await db.Pets
                .Where(p => p.OwnerEmail == email)
                .Select(p => p.Type)
                .Distinct()
                .ToListAsync();
        }

        // ===== 2️⃣ 载入 Product + ProductPetTypes =====
        var products = await db.Products
            .Where(p => p.IsActive)
            .Include(p => p.ProductPetTypes)
            .ToListAsync();

        // ===== 3️⃣ 优先排序（重点）=====
        products = products
            .OrderByDescending(p =>
                p.ProductPetTypes.Any(ppt =>
                    memberPetTypes.Contains(ppt.PetType)))
            .ThenBy(p => p.Name)
            .Take(pageSize)
            .ToList();

        var vm = new ProductPageVM
        {
            Categories = categories,
            Products = products,
            CurrentPage = 1,
            TotalPages = (int)Math.Ceiling(
                db.Products.Count(p => p.IsActive) / (double)pageSize
            )
        };

        return View(vm);
    }


    // ===== AJAX Product List =====
    [HttpGet("AjaxList")]
    public async Task<IActionResult> AjaxList(
     string search,
     int? categoryId,
     int page = 1)
    {
        int pageSize = 8;

        // ===== 1️⃣ 取得 Member PetType =====
        var email = User.Identity?.Name;

        List<PetType> memberPetTypes = new();

        if (!string.IsNullOrEmpty(email))
        {
            memberPetTypes = await db.Pets
                .Where(p => p.OwnerEmail == email)
                .Select(p => p.Type)
                .Distinct()
                .ToListAsync();
        }

        // ===== 2️⃣ 取出所有符合条件商品 =====
        var query = db.Products
            .Where(p => p.IsActive)
            .Include(p => p.ProductPetTypes)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(p =>
                p.Name.Contains(search) ||
                (p.Description != null && p.Description.Contains(search)) ||
                (p.Brand != null && p.Brand.Contains(search))
            );
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId);
        }

        var allProducts = await query.ToListAsync();

        // ===== 3️⃣ 先排序（重点）=====
        var sorted = allProducts
            .OrderByDescending(p =>
                p.ProductPetTypes.Any(ppt =>
                    memberPetTypes.Contains(ppt.PetType)))
            .ThenBy(p => p.Name)
            .ToList();

        // ===== 4️⃣ 再分页 =====
        int totalItems = sorted.Count;

        var pagedProducts = sorted
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var vm = new ProductPageVM
        {
            Products = pagedProducts,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        };

        return PartialView("_ProductList", vm);
    }


    // ===== Product Details =====
    [HttpGet("Details/{id}")]
    public IActionResult Details(int id)
    {
        var product = db.Products
            .FirstOrDefault(p => p.Id == id && p.IsActive);

        if (product == null)
            return NotFound();

        return View(product);
    }
}
