using EduCore.Data;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
{
    // ponytail: Production uses SQLite (Render has no SQL Server add-on).
    // Dev keeps localdb. Swap to PostgreSQL/SQL Server in real prod.
    if (builder.Environment.IsDevelopment())
        o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    else
        o.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddDefaultIdentity<ApplicationUser>(o =>
    {
        o.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

// Anonymous visitors to any protected route (including "/") land on the
// styled login page instead of the Home dashboard.
builder.Services.ConfigureApplicationCookie(opts =>
{
    opts.LoginPath = "/Account/Login";
    opts.LogoutPath = "/Account/Logout";
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    // ponytail: SQLite provider (Production/Render) can't reuse SQL Server migrations.
    // EnsureCreated builds the schema from the model directly; seed still runs.
    if (app.Environment.IsDevelopment())
        db.Database.Migrate();
    else
        db.Database.EnsureCreated();
    await DbSeeder.SeedAsync(db, users, roles);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();