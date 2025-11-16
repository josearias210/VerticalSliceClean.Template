namespace Acme.Application.Features.Account.RefreshToken;

public class RefreshTokenCommandResponse
{
    public required string UserId { get; set; }
    public required string Email { get; set; }
    public string? FullName { get; set; }
    public required IEnumerable<string> Roles { get; set; }
    public bool EmailConfirmed { get; set; }
}
