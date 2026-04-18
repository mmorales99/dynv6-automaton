namespace Dyndns.Service.Domain;

public static class UpdateDecision
{
    public static bool ShouldUpdate(string? lastKnownIpv4, string currentIpv4, bool forceUpdate)
        => forceUpdate || !string.Equals(lastKnownIpv4, currentIpv4, StringComparison.Ordinal);
}