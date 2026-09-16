using EduCore.Models.Constants;
using EduCore.Models.Entities;
using EduCore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EduCore.Controllers;

/// <summary>Login/logout entry points. Uses the stock SignInManager so the same
/// seed accounts keep working. Anonymous-allowed so the page renders before sign-in.</summary>
[AllowAnonymous]
[Route("[controller]/[action]")]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;

    public AccountController(SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users)
    {
        _signIn = signIn;
        _users = users;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (_signIn.IsSignedIn(User))
            return RedirectToLocal(returnUrl) ?? RedirectToAction("Index", "Home");

        return View(new LoginViewModel { ReturnUrl = returnUrl ?? "/" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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
        return RedirectToLocal(returnUrl) ?? RedirectToAction("Login", "Account");
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        if (_signIn.IsSignedIn(User))
            return RedirectToLocal(returnUrl) ?? RedirectToAction("Index", "Home");

        return View(new RegisterViewModel { ReturnUrl = returnUrl ?? "/" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = new ApplicationUser
        {
            UserName = vm.Email,
            Email = vm.Email,
            DisplayName = vm.FullName,
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
