using System.Security.Claims;

namespace EprRegulatorGateway.Utils.Http;

internal static class ClaimsPrincipalExtensions
{
    private static readonly string[] s_userIdClaimTypes =
    [
        ClaimTypes.NameIdentifier,
        "sub",
        "oid"
    ];

    public static Guid? TryGetUserId(this ClaimsPrincipal user)
    {
        foreach (var claimType in s_userIdClaimTypes)
        {
            var v = user.FindFirstValue(claimType);
            if (v is not null && Guid.TryParse(v, out var id))
            {
                return id;
            }
        }

        return null;
    }
}
