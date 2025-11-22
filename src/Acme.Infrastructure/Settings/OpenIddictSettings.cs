namespace Acme.Infrastructure.Settings;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// OpenIddict certificate configuration for production environments.
/// </summary>
public class OpenIddictSettings
{
    /// <summary>
    /// Path to the encryption certificate (.pfx file).
    /// Required in production environments.
    /// </summary>
    public string? EncryptionCertificatePath { get; set; }

    /// <summary>
    /// Path to the signing certificate (.pfx file).
    /// Required in production environments.
    /// </summary>
    public string? SigningCertificatePath { get; set; }

    /// <summary>
    /// Password for the certificate files.
    /// Store this in User Secrets or Azure Key Vault.
    /// </summary>
    public string? CertificatePassword { get; set; }
}
