using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using EduCore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EduCore.Controllers;

/// <summary>Login/logout entry points. Uses the stock SignInManager so the same
/// seed accounts keep working. Login/Register are anonymous; everything else
/// (Logout, Settings) requires a signed-in user.</summary>
[Authorize]
[Route("[controller]/[action]")]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;
    private readonly AppDbContext _db;

    public AccountController(SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users, AppDbContext db)
    {
        _signIn = signIn;
        _users = users;
        _db = db;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (_signIn.IsSignedIn(User))
            return RedirectToLocal(returnUrl) ?? RedirectToAction("Index", "Home");

        return View(new LoginViewModel { ReturnUrl = returnUrl ?? "/" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _signIn.PasswordSignInAsync(
            vm.Email, vm.Password, vm.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
            return RedirectToLanding(User) ?? RedirectToLocal(vm.ReturnUrl) ?? RedirectToAction("Index", "Home");

        ModelState.AddModelError("", "Invalid email or password.");
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(string? returnUrl = null)
    {
        await _signIn.SignOutAsync();
        TempData["Info"] = "You have been signed out.";
        return RedirectToLocal(returnUrl) ?? RedirectToAction("Login", "Account");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string? returnUrl = null)
    {
        if (_signIn.IsSignedIn(User))
            return RedirectToLocal(returnUrl) ?? RedirectToAction("Index", "Home");

        return View(new RegisterViewModel { ReturnUrl = returnUrl ?? "/" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var person = new Person
        {
            FirstName = vm.FullName.Split(' ').FirstOrDefault() ?? "",
            LastName = vm.FullName.Split(' ').Skip(1).FirstOrDefault() ?? "",
            Email = vm.Email
        };
        _db.Persons.Add(person);
        await _db.SaveChangesAsync();

        var user = new ApplicationUser
        {
            UserName = vm.Email,
            Email = vm.Email,
            PersonId = person.Id,
            EmailConfirmed = true // self-registered — no email-confirmation flow yet
        };
        var result = await _users.CreateAsync(user, vm.Password);

        if (result.Succeeded)
        {
            await _users.AddToRoleAsync(user, AppRoles.Parent);
            TempData["Info"] = "Account created. Sign in to continue.";
            return RedirectToAction("Login", new { returnUrl = vm.ReturnUrl });
        }

        foreach (var err in result.Errors)
            ModelState.AddModelError("", err.Description);
        return View(vm);
    }

    /// <summary>Self-service password change (sidebar Settings link). Authenticated for any role —
    /// provisioning others' accounts is the Admin-only AccountsController.</summary>
    [HttpGet]
    public IActionResult Settings() => View(new ChangePasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View("Settings", vm);

        var user = await _users.GetUserAsync(User);
        if (user is null) return Challenge();

        if (vm.CurrentPassword is null || !await _users.CheckPasswordAsync(user, vm.CurrentPassword))
        {
            ModelState.AddModelError(nameof(ChangePasswordViewModel.CurrentPassword), "Current password is incorrect.");
            return View("Settings", vm);
        }

        var result = await _users.ChangePasswordAsync(user, vm.CurrentPassword, vm.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError(nameof(ChangePasswordViewModel.NewPassword), err.Description);
            return View("Settings", vm);
        }

        // Changing the password regenerates the security stamp and drops the auth cookie, so re-sign-in
        // to keep the user in rather than bouncing them to the login page.
        await _signIn.SignInAsync(user, isPersistent: false);
        TempData["Info"] = "Password updated.";
        return RedirectToAction(nameof(Settings));
    }

    private static IActionResult? RedirectToLocal(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl)) return null;
        // Only allow local relative paths — blocks open-redirect to external sites.
        if (returnUrl.StartsWith("/") && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
            return new RedirectResult(returnUrl);
        return null;
    }

    /// <summary>Role-aware landing after sign-in. Finance -> money report; everyone else -> Home.
    /// Returns null to fall through to the generic ReturnUrl.</summary>
    private static IActionResult? RedirectToLanding(System.Security.Claims.ClaimsPrincipal user)
    {
        if (user.IsInRole(AppRoles.Finance))
            return new RedirectToRouteResult("default", new { controller = "Reports", action = "Money" });
        // Admin/Registrar/Faculty/Parent all land on Home (the role-scoped nav adapts).
        return null;
    }
}