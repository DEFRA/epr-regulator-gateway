using System.Diagnostics.CodeAnalysis;

namespace EprRegulatorGateway.Authentication;

[ExcludeFromCodeCoverage]
public static class PolicyNames
{
    public const string Read = nameof(Scopes.Read);
    public const string Write = nameof(Scopes.Write);
}
