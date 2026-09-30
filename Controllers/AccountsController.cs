using EduCore.Data;
using EduCore.Models.Constants;
using EduCore.Models.Entities;
using EduCore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Controllers;

/// <summary>Account Management — Admin-only provisioning of login accounts.
/// List all users, create an account with any role, reset a password, lock/unlock.
/// Self-service password change lives on AccountController (Account/Settings).</summary>
[Authorize(Roles = AppRoles.Admin)]
public class AccountsController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly AppDbContext _db;

    public AccountsController(UserManager<ApplicationUser> users, AppDbContext db)
    {
        _users = users;
        _db = db;
    }

    public async Task<IActionResult> Index(string? q, string? role, bool? locked, int page = 1, int pageSize = 10)
    {
        // Clamp pageSize to allowed values
        var allowedPageSizes = new[] { 5, 10, 25, 50 };
        if (!allowedPageSizes.Contains(pageSize)) pageSize = 10;

        // Base query
        var query = _users.Users
            .Include(u => u.Person)
            .AsQueryable();

        // Search filter — DisplayName may come from Person.FullName (computed), so search
        // the mapped columns (Email, Person.FirstName/LastName) instead.
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(u =>
                u.Email != null && u.Email.ToLower().Contains(term) ||
                u.Person!.FirstName.ToLower().Contains(term) ||
                u.Person!.LastName.ToLower().Contains(term));
        }

        // Role filter
        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleId = await _db.Roles.Where(r => r.Name == role).Select(r => r.Id).FirstOrDefaultAsync();
            query = query.Where(u => _db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId));
        }

        // Locked filter
        if (locked.HasValue)
        {
            query = locked.Value
                ? query.Where(u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow)
                : query.Where(u => u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.UtcNow);
        }

        // Order
        query = query.OrderBy(u => u.Email);

        // Total count before paging
        var totalCount = await query.CountAsync();

        // Clamp page
        var totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;
        if (page < 1) page = 1;
        if (totalPages > 0 && page > totalPages) page = totalPages;

        // Paged users
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Batched role lookup for the paged users — one query instead of GetRolesAsync per user.
        var pageIds = items.Select(u => u.Id).ToList();
        var rolePairs = await _db.UserRoles
            .Where(ur => pageIds.Contains(ur.UserId))
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, RoleName = r.Name ?? "" })
            .ToListAsync();
        var rolesByUser = rolePairs
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => (IList<string>)g.Select(x => x.RoleName).ToList());

        var rows = items.Select(u => new UserListItemVM
        {
            Id = u.Id,
            Email = u.Email ?? "",
            DisplayName = u.Person?.FullName ?? u.Email ?? "",
            Roles = rolesByUser.GetValueOrDefault(u.Id) ?? new List<string>(),
            LockoutEnd = u.LockoutEnd
        }).ToList();

        var vm = new AccountsIndexVM
        {
            Items = rows,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Search = q,
            Role = role,
            Locked = locked,
            CurrentUserId = _users.GetUserId(User) ?? "",
            // Plain strings have no DataValueField — build Value/Text pairs so the
            // role options don't all render value="" (which filters as "All roles").
            RoleValues = new SelectList(AppRoles.All.Select(r => new { Value = r, Text = r }), "Value", "Text")
        };
        return View(vm);
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

        var person = new Person
        {
            FirstName = vm.DisplayName.Split(' ').FirstOrDefault() ?? "",
            LastName = vm.DisplayName.Split(' ').Skip(1).FirstOrDefault() ?? "",
            Email = vm.Email
        };
        _db.Persons.Add(person);
        await _db.SaveChangesAsync();

        var user = new ApplicationUser
        {
            UserName = vm.Email,
            Email = vm.Email,
            PersonId = person.Id,
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

        // If Faculty role, auto-create Faculty record linked to the same Person
        if (vm.Role == AppRoles.Faculty)
        {
            var existingFaculty = await _db.Faculty.FirstOrDefaultAsync(f => f.PersonId == person.Id);
            if (existingFaculty is null)
            {
                var facultyCount = await _db.Faculty.CountAsync();
                _db.Faculty.Add(new Faculty
                {
                    PersonId = person.Id,
                    EmployeeNumber = $"EMP-{1001 + facultyCount:D4}",
                    HireDate = DateTime.Today,
                    IsActive = true
                });
                await _db.SaveChangesAsync();
            }
        }

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