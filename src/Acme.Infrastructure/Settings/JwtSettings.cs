using System.ComponentModel.DataAnnotations;

namespace Acme.Infrastructure.Settings;

public class JwtSettings
{
    [Required(ErrorMessage = "JWT Key is required")]
    [MinLength(32, ErrorMessage = "JWT Key must be at least 32 characters")]
    public required string Key { get; set; }

    [Range(1, 1440, ErrorMessage = "Access token minutes must be between 1 and 1440 (24 hours)")]
    public required int AccessTokenMinutes { get; set; }

    [Range(1, 90, ErrorMessage = "Refresh token days must be between 1 and 90")]
    public required int RefreshTokenDays { get; set; }

    [Required(ErrorMessage = "Issuer is required")]
    public required string Issuer { get; set; }

    [Required(ErrorMessage = "Audience is required")]
    public required string Audience { get; set; }
}
