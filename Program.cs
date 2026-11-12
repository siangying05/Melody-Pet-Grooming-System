global using Melody.Models;
using Melody;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews()
    .AddSessionStateTempDataProvider();
builder.Services.AddSqlServer<MelodyDB>($@"
    Data Source=(LocalDB)\MSSQLLocalDB;
    AttachDbFilename={builder.Environment.ContentRootPath}\MelodyDB.mdf;
");
builder.Services.AddScoped<Helper>();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication().AddCookie();
builder.Services.AddHttpContextAccessor();
var app = builder.Build();


app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();          
app.UseAuthentication();   
app.UseAuthorization();
app.MapDefaultControllerRoute();
app.Run();
