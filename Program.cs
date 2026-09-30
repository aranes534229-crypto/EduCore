using EduCore.Data;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ponytail: Dev prefers SQL Server (localdb), but if it's not reachable (no LocalDB
// instance in the sandbox), fall back to a local SQLite file so `dotnet run` works
// without SQL Server installed. Production already uses SQLite.
var cs = builder.Configuration.GetConnectionString("DefaultConnection");
var useSqlite = !builder.Environment.IsDevelopment();
if (builder.Environment.IsDevelopment())
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
    // ponytail: SQLite provider can't reuse SQL Server migrations; EnsureCreated builds
    // the schema from the model directly. Seed runs either way.
    db.Database.EnsureCreated();
    await DbSeeder.SeedAsync(db, users, roles);
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

app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
