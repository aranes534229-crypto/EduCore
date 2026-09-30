using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EduCore.Models;
using EduCore.Models.Constants;

namespace EduCore.Controllers;

[Authorize] // anonymous visitors → /Account/Login (role-aware landing after sign-in)
public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.IsInRole(AppRoles.Faculty))
            return RedirectToAction("Index", "FacultyPortal");
        if (User.IsInRole(AppRoles.Admin))
            return RedirectToAction("Index", "AdminDashboard");
        if (User.IsInRole(AppRoles.Parent))
            return RedirectToAction("Index", "ParentDashboard");
        if (User.IsInRole(AppRoles.Registrar))
            return RedirectToAction("Index", "RegistrarDashboard");
        if (User.IsInRole(AppRoles.Finance))
            return RedirectToAction("Index", "FinanceDashboard");
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
