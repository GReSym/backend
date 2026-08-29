using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace GReSym.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var claim =
            user.FindFirst(ClaimTypes.NameIdentifier) ??
            user.FindFirst(JwtRegisteredClaimNames.Sub);

        return int.Parse(claim!.Value);
    }
}