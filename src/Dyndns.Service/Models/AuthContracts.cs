namespace Dyndns.Service.Models;

public sealed record LoginRequest(string UserName, string Password);

public sealed record PasswordRequest(string Password);

public sealed record RunRequest(string Password, bool ForceUpdate = false);

public sealed record RunAccessResponse(string Role)
{
    public bool IsAdmin => string.Equals(Role, "admin", StringComparison.OrdinalIgnoreCase);
}

public sealed record SetupRequest(string AdminPassword, string ViewerPassword);

public sealed record AuthenticatedUser(string Name, string Roles)
{
    public bool IsAdmin => string.Equals(Roles, "admin", StringComparison.OrdinalIgnoreCase);
}
