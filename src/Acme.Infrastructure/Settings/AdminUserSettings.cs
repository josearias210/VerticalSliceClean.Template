using System.ComponentModel.DataAnnotations;

namespace Acme.Infrastructure.Settings;

public class AdminUserSettings
{
    [Required(ErrorMessage = "Admin email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public required string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Admin password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public required string Password { get; set; } = string.Empty;
}
