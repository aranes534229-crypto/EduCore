using EduCore.Data;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Detect SQL Server vs SQLite from connection string format
var cs = builder.Configuration.GetConnectionString("DefaultConnection");
var useSqlite = cs?.Contains("DataSource=", StringComparison.OrdinalIgnoreCase) == true 
             || cs?.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) == true;

if (builder.Environment.IsDevelopment() && !useSqlite)
{
    try
    {
        using var probe = new SqlConnection(cs);
        probe.Open();
    }
    catch (SqlException)
    {
        useSqlite = true;
        cs = "DataSource=educore-dev.db";
    }
}

builder.Services.AddDbContext<AppDbContext>(o =>
{
    if (useSqlite)
        o.UseSqlite(cs);
    else
        o.UseSqlServer(cs);
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
    // Development: allow non‑secure cookies on HTTP so the auth cookie is actually set.
    if (builder.Environment.IsDevelopment())
    {
        opts.Cookie.SecurePolicy = CookieSecurePolicy.None;
        opts.Cookie.SameSite = SameSiteMode.Lax;
    }
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<EduCore.Services.FacultyScopeService>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, EduCore.Policies.FacultyStudentAccessHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(EduCore.Policies.FacultyStudentAccessPolicy.Name, policy =>
        policy.Requirements.Add(new EduCore.Policies.FacultyStudentAccessRequirement()));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        if (useSqlite)
        {
            db.Database.EnsureCreated();
        }
        else
        {
            db.Database.Migrate();
        }
        await DbSeeder.SeedAsync(db, users, roles);
        logger.LogInformation("Database migration and seeding completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration/seeding failed — app will start but DB may be uninitialized");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
else
{
    // In Development, skip HTTPS redirection to avoid redirect loops when only HTTP is used.
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Temporary seed endpoint - REMOVE AFTER USE
app.MapGet("/seed-db", async (HttpContext ctx) =>
{
    var token = ctx.Request.Query["token"].ToString();
    if (token != "SEED_TOKEN_2024")
    {
        return Results.Unauthorized();
    }

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        if (!app.Environment.IsDevelopment())
        {
            db.Database.Migrate();
        }
        else
        {
            db.Database.EnsureCreated();
        }
        
        await DbSeeder.SeedAsync(db, users, roles);
        
        return Results.Ok(new { 
            success = true, 
            message = "Database migration and seeding completed successfully",
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Manual seed failed");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
}).AllowAnonymous();

app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
