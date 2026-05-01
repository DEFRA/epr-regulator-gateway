using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using EprRegulatorGateway.Utils.Auth;

namespace EprRegulatorGateway.Authentication;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationAuthorization(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AclOptions>().BindConfiguration("Acl").ValidateDataAnnotations().ValidateOnStart();

        var rejectWhenNotConfigured = configuration.GetValue<bool>("Authentication:RejectWhenNotConfigured");
        var hasAclClientsConfigured = configuration.GetSection("Acl:Clients").GetChildren().Any();
        var rejectModeEnabled = rejectWhenNotConfigured && !hasAclClientsConfigured;

        if (rejectModeEnabled)
        {
            services
                .AddAuthentication(RejectAuthenticationDefaults.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, RejectAuthenticationHandler>(
                    RejectAuthenticationDefaults.SchemeName,
                    _ => { }
                );

            services
                .AddAuthorizationBuilder()
                .AddPolicy(
                    PolicyNames.Read,
                    builder =>
                        builder
                            .RequireAuthenticatedUser()
                            .AddAuthenticationSchemes(RejectAuthenticationDefaults.SchemeName)
                )
                .AddPolicy(
                    PolicyNames.Write,
                    builder =>
                        builder
                            .RequireAuthenticatedUser()
                            .AddAuthenticationSchemes(RejectAuthenticationDefaults.SchemeName)
                );

            return services;
        }

        services
            .AddAuthentication(BasicAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(
                BasicAuthenticationHandler.SchemeName,
                _ => { }
            )
            .AddScheme<JwtBearerOptions, JwtAuthenticationHandler>(
                JwtAuthenticationHandler.SchemeName,
                options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        SignatureValidator = (token, _) => new JsonWebToken(token),
                        ValidateAudience = false,
                        ValidateIssuer = false,
                    };
                }
            );

        services
            .AddAuthorizationBuilder()
            .AddPolicy(
                PolicyNames.Read,
                builder =>
                    builder
                        .RequireAuthenticatedUser()
                        .AddAuthenticationSchemes(
                            BasicAuthenticationHandler.SchemeName,
                            JwtAuthenticationHandler.SchemeName
                        )
                        .RequireClaim(Claims.Scope, Scopes.Read)
            )
            .AddPolicy(
                PolicyNames.Write,
                builder =>
                    builder
                        .RequireAuthenticatedUser()
                        .AddAuthenticationSchemes(
                            BasicAuthenticationHandler.SchemeName,
                            JwtAuthenticationHandler.SchemeName
                        )
                        .RequireClaim(Claims.Scope, Scopes.Write)
            );

        return services;
    }
}
