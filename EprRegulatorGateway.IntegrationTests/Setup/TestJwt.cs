using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace EprRegulatorGateway.IntegrationTests.Setup;

internal static class TestJwt
{
    public static string GenerateJwt(params Claim[] claims)
    {
        var rand = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(rand);

        var token = new JwtSecurityToken(
            issuer: "Local",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(rand), SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
