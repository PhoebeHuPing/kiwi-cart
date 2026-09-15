using System.Security.Claims;

namespace KiwiCart.Api.Authorization;

public static class AdminAuthorization
{
    public const string PolicyName = "AdminAccess";
    public const string AdminEmail = "phoebe.ping.hu@gmail.com";
    public const string EmailClaim = "https://kiwicart.co.nz/email";

    public static bool IsAdmin(ClaimsPrincipal user)
    {
        return user.IsInRole("admin")
            || string.Equals(
                user.FindFirstValue(EmailClaim)
                    ?? user.FindFirstValue(ClaimTypes.Email)
                    ?? user.FindFirstValue("email"),
                AdminEmail,
                StringComparison.OrdinalIgnoreCase);
    }
}
