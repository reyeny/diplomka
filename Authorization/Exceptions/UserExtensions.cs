using System.Security.Claims;

namespace Authorization.Exceptions;

public static class UserExtensions
{
    public static string? GetId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier);
        return claim?.Value;
    }

}