using EduCore.Models.Constants;
using EduCore.Models.Entities;
using EduCore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Controllers;

/// <summary>Account Management — Admin-only provisioning of login accounts.
/// List all users, create an account with any role, reset a password, lock/unlock.
/// Self-service password change lives on AccountController (Account/Settings).</summary>
[Authorize(Roles = AppRoles.Admin)]
public class AccountsController : Controller
{
    private readonly UserManager<ApplicationUser> _users;

    public AccountsController(UserManager<ApplicationUser> users)
    {
        _users = users;
    }

    public async Task<IActionResult> Index()
    {
        var apps = await _users.Users.OrderBy(u => u.Email).ToListAsync();
        var rows = new List<UserListItemVM>();
        foreach (var u in apps)
        {
            rows.Add(new UserListItemVM
            {
                Id = u.Id,
                Email = u.Email ?? "",
                DisplayName = u.DisplayName,
                Roles = await _users.GetRolesAsync(u),
                LockoutEnd = u.LockoutEnd
            });
        }
        ViewData["CurrentUserId"] = _users.GetUserId(User);
        return View(rows);
    }

    [HttpGet]
    public IActionResult Create() => View(new AccountCreateViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccountCreateViewModel vm)
    {
        if (!Array.Exists(AppRoles.All, r => r == vm.Role))
            ModelState.AddModelError(nameof(AccountCreateViewModel.Role), "Pick a valid role.");

        if (!ModelState.IsValid) return View(vm);

        if ((await _users.FindByEmailAsync(vm.Email)) is not null)
            ModelState.AddModelError(nameof(AccountCreateViewModel.Email), "An account already uses this email.");

        if (!ModelState.IsValid) return View(vm);

        var user = new ApplicationUser
        {
            UserName = vm.Email,
            Email = vm.Email,
            DisplayName = vm.DisplayName,
            EmailConfirmed = true
        };
        var result = await _users.CreateAsync(user, vm.Password);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError(nameof(AccountCreateViewModel.Password), err.Description);
            return View(vm);
        }

        await _users.AddToRoleAsync(user, vm.Role);
        TempData["Info"] = $"Account created for {user.Email} ({vm.Role}). They can change their password from Settings.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string id)
    {
        if (await _users.FindByIdAsync(id) is null) return NotFound();
        return View(new ResetPasswordViewModel { UserId = id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = await _users.FindByIdAsync(vm.UserId);
        if (user is null) return NotFound();

        var remove = await _users.RemovePasswordAsync(user);
        if (!remove.Succeeded)
        {
            foreach (var err in remove.Errors)
                ModelState.AddModelError(string.Empty, err.Description);
            return View(vm);
        }

        var result = await _users.AddPasswordAsync(user, vm.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError(nameof(ResetPasswordViewModel.NewPassword), err.Description);
            return View(vm);
        }

        // A reset also unblocks a locked account.
        if (user.LockoutEnd is not null)
            await _users.SetLockoutEndDateAsync(user, null);

        TempData["Info"] = $"Password reset for {user.Email}. They can change it again from Settings.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null) return NotFound();

        // Can't lock yourself out of the last admin console.
        if (user.Id == _users.GetUserId(User))
        {
            TempData["Error"] = "You cannot lock your own account.";
            return RedirectToAction(nameof(Index));
        }

        bool locked = user.LockoutEnd is not null && user.LockoutEnd > DateTimeOffset.UtcNow;
        await _users.SetLockoutEndDateAsync(user, locked ? null : DateTimeOffset.UtcNow.AddYears(100));
        TempData["Info"] = locked ? $"{user.Email} unlocked." : $"{user.Email} locked.";
        return RedirectToAction(nameof(Index));
    }
}