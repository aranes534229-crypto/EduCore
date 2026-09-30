using EduCore.Services;
using EduCore.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace EduCore.Policies;

public class FacultyStudentAccessRequirement : IAuthorizationRequirement { }

public class FacultyStudentAccessHandler : AuthorizationHandler<FacultyStudentAccessRequirement, int>
{
    private readonly FacultyScopeService _scope;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _users;

    public FacultyStudentAccessHandler(FacultyScopeService scope, IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> users)
    {
        _scope = scope;
        _httpContextAccessor = httpContextAccessor;
        _users = users;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, FacultyStudentAccessRequirement requirement, int studentId)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return;

        var faculty = await _scope.GetMyFacultyAsync(user);
        if (faculty == null) return;

        var canAccess = await _scope.CanAccessStudentAsync(faculty.Id, studentId);
        if (canAccess)
        {
            context.Succeed(requirement);
        }
    }
}

// Extension for easy use
public static class FacultyStudentAccessPolicy
{
    public const string Name = "FacultyStudentAccess";
}