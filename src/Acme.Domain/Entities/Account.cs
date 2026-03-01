
namespace Acme.Domain.Entities;

using Microsoft.AspNetCore.Identity;

public class Account : IdentityUser
{
    public string? FullName { get; set; }
    public string? PreferredUsername { get; set; }
}
