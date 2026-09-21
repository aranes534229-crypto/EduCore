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

/// <summary>Accepted payment methods when recording a payment toward an invoice
/// (Finance + Parent forms). Only these two are permitted; anything else must
/// be recorded separately.</summary>
public static class PaymentMethods
{
    public const string Manual = "Manual";
    public const string AccountPortal = "Account Portal Payment";

    /// <summary>Methods selectable by staff (Finance/Admin). Parents have no choice —
    /// their payment is always debited from their own account (AccountPortal).</summary>
    public static readonly string[] All = { Manual, AccountPortal };

    /// <summary>The sole method a parent's portal form may record.</summary>
    public const string ParentOnly = AccountPortal;
}