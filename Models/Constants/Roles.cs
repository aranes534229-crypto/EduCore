namespace EduCore.Models.Constants;

/// <summary>Fixed role names used across the app. Matches the 5 portals in the proposal.
/// Named <c>AppRoles</c> to avoid colliding with IdentityDbContext's <c>Roles</c> property.</summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Registrar = "Registrar";
    public const string Faculty = "Faculty";
    public const string Finance = "Finance";
    public const string Parent = "Parent";

    public static readonly string[] All = { Admin, Registrar, Faculty, Finance, Parent };
}