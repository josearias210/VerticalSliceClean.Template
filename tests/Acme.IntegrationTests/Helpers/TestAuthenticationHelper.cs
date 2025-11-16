using System.Net;
using FluentAssertions;
using Acme.Application.Features.Account.Login;

namespace Acme.IntegrationTests.Helpers;

/// <summary>
/// Helper class for authentication operations in integration tests.
/// Provides reusable methods for login, token extraction, and cookie validation.
/// </summary>
public static class TestAuthenticationHelper
{
    /// <summary>
    /// Performs login and extracts the access token from the response cookies.
    /// </summary>
    /// <param name="client">The HTTP client to use for the request</param>
    /// <param name="credentials">The login credentials</param>
    /// <returns>The extracted access token</returns>
    /// <exception cref="InvalidOperationException">Thrown when login fails or token cannot be extracted</exception>
    public static async Task<string> LoginAndGetTokenAsync(HttpClient client, LoginCommand credentials)
    {
        var response = await client.PostAsJsonAsync("/api/v1/accounts/login", credentials);
        
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Login failed with status {response.StatusCode}. Response: {content}");
        }
        
        return ExtractAccessTokenFromCookies(response);
    }

    /// <summary>
    /// Extracts the access token from the Set-Cookie headers in the response.
    /// </summary>
    /// <param name="response">The HTTP response containing Set-Cookie headers</param>
    /// <returns>The extracted access token value</returns>
    /// <exception cref="InvalidOperationException">Thrown when the access token cookie is not found</exception>
    public static string ExtractAccessTokenFromCookies(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookieValues))
        {
            throw new InvalidOperationException("No Set-Cookie header found in response");
        }

        var cookies = cookieValues.ToList();
        var accessTokenCookie = cookies.FirstOrDefault(c => c.Contains("accessToken=", StringComparison.OrdinalIgnoreCase));
        
        if (accessTokenCookie == null)
        {
            var availableCookies = string.Join(", ", cookies.Select(c => c.Split('=')[0]));
            throw new InvalidOperationException($"Access token cookie not found. Available cookies: {availableCookies}");
        }

        var tokenPart = accessTokenCookie.Split(';')[0];
        var token = tokenPart.Substring(tokenPart.IndexOf('=') + 1);
        return token;
    }

    /// <summary>
    /// Validates that the response contains the expected authentication cookies with proper attributes.
    /// </summary>
    /// <param name="response">The HTTP response to validate</param>
    /// <param name="shouldHaveAccessToken">Whether the access token cookie should be present</param>
    /// <param name="shouldHaveRefreshToken">Whether the refresh token cookie should be present</param>
    public static void ValidateAuthCookies(HttpResponseMessage response, bool shouldHaveAccessToken = true, bool shouldHaveRefreshToken = true)
    {
        response.Headers.Should().ContainKey("Set-Cookie");
        
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookieValues))
        {
            throw new InvalidOperationException("No Set-Cookie header found in response");
        }

        var cookies = cookieValues.ToList();

        if (shouldHaveAccessToken)
        {
            var accessTokenCookie = cookies.FirstOrDefault(c => c.Contains("accessToken=", StringComparison.OrdinalIgnoreCase));
            accessTokenCookie.Should().NotBeNull("access token cookie should be present");
            accessTokenCookie!.Should().Contain("httponly", "access token should be HttpOnly");
            accessTokenCookie.Should().Contain("path=/", "access token should have path set");
        }

        if (shouldHaveRefreshToken)
        {
            var refreshTokenCookie = cookies.FirstOrDefault(c => c.Contains("refreshToken=", StringComparison.OrdinalIgnoreCase));
            refreshTokenCookie.Should().NotBeNull("refresh token cookie should be present");
            refreshTokenCookie!.Should().Contain("httponly", "refresh token should be HttpOnly");
            refreshTokenCookie.Should().Contain("path=/", "refresh token should have path set");
        }
    }

    /// <summary>
    /// Creates an authenticated HTTP request with the Bearer token.
    /// </summary>
    /// <param name="method">The HTTP method</param>
    /// <param name="requestUri">The request URI</param>
    /// <param name="accessToken">The access token to include</param>
    /// <returns>A configured HTTP request message</returns>
    public static HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string requestUri, string accessToken)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        return request;
    }

    /// <summary>
    /// Performs logout operation.
    /// </summary>
    /// <param name="client">The HTTP client to use for the request</param>
    /// <param name="accessToken">The access token for authentication</param>
    /// <returns>The HTTP response from the logout endpoint</returns>
    public static async Task<HttpResponseMessage> LogoutAsync(HttpClient client, string accessToken)
    {
        var request = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/accounts/logout", accessToken);
        return await client.SendAsync(request);
    }

    /// <summary>
    /// Validates that cookies have been cleared (typically after logout).
    /// </summary>
    /// <param name="response">The HTTP response to validate</param>
    public static void ValidateCookiesCleared(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookieValues))
        {
            return; // No cookies set, which is acceptable
        }

        var cookies = cookieValues.ToList();
        
        foreach (var cookie in cookies)
        {
            if (cookie.Contains("accessToken=") || cookie.Contains("refreshToken="))
            {
                // Cookie should be expired or have empty value
                cookie.Should().Match(c => 
                    c.Contains("expires=", StringComparison.OrdinalIgnoreCase) || 
                    c.Contains("max-age=0", StringComparison.OrdinalIgnoreCase) ||
                    c.Contains("=;"), 
                    "auth cookies should be cleared on logout");
            }
        }
    }
}
