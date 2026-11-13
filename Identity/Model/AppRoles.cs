namespace Identity.Model;

/// <summary>Role names used in JWT role claims, <c>[Authorize(Roles = ...)]</c> and role policies.</summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Staff = "Staff";
    public const string Customer = "Customer";

    /// <summary>Role assigned on self-registration.</summary>
    public const string Default = Customer;
}
