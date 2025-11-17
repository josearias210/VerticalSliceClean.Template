// -----------------------------------------------------------------------
// <copyright file="CommonValidationRules.cs" company="Acme">
// Copyright (c) Acme. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;

namespace Acme.Application.Shared.Validators;

/// <summary>
/// Reusable validation rules to maintain consistency across validators.
/// </summary>
public static class CommonValidationRules
{
    /// <summary>
    /// Validates that an email is in valid format.
    /// </summary>
    public static IRuleBuilderOptions<T, string> EmailMustBeValid<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .WithMessage("Email is required")
            .WithErrorCode("Email.Required")
            .EmailAddress()
            .WithMessage("Invalid email format")
            .WithErrorCode("Email.Invalid")
            .MaximumLength(256)
            .WithMessage("Email must not exceed 256 characters")
            .WithErrorCode("Email.TooLong");
    }

    /// <summary>
    /// Validates that a password meets strong security requirements.
    /// Requires:
    /// - At least 8 characters
    /// - At least one uppercase letter
    /// - At least one lowercase letter
    /// - At least one digit
    /// - At least one special character
    /// </summary>
    public static IRuleBuilderOptions<T, string> PasswordMustBeStrong<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .WithMessage("Password is required")
            .WithErrorCode("Password.Required")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters")
            .WithErrorCode("Password.TooShort")
            .MaximumLength(128)
            .WithMessage("Password must not exceed 128 characters")
            .WithErrorCode("Password.TooLong")
            .Matches(@"[A-Z]")
            .WithMessage("Password must contain at least one uppercase letter")
            .WithErrorCode("Password.MissingUppercase")
            .Matches(@"[a-z]")
            .WithMessage("Password must contain at least one lowercase letter")
            .WithErrorCode("Password.MissingLowercase")
            .Matches(@"\d")
            .WithMessage("Password must contain at least one digit")
            .WithErrorCode("Password.MissingDigit")
            .Matches(@"[^\w]")
            .WithMessage("Password must contain at least one special character")
            .WithErrorCode("Password.MissingSpecialChar");
    }

    /// <summary>
    /// Validates that a password is not empty (for login scenarios where complexity is not checked).
    /// </summary>
    public static IRuleBuilderOptions<T, string> PasswordMustNotBeEmpty<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .WithMessage("Password is required")
            .WithErrorCode("Password.Required");
    }

    /// <summary>
    /// Validates that a string is not empty and has a maximum length.
    /// </summary>
    public static IRuleBuilderOptions<T, string> RequiredWithMaxLength<T>(
        this IRuleBuilder<T, string> ruleBuilder, 
        int maxLength,
        string propertyName)
    {
        return ruleBuilder
            .NotEmpty()
            .WithMessage($"{propertyName} is required")
            .WithErrorCode($"{propertyName}.Required")
            .MaximumLength(maxLength)
            .WithMessage($"{propertyName} must not exceed {maxLength} characters")
            .WithErrorCode($"{propertyName}.TooLong");
    }

    /// <summary>
    /// Validates that a Guid is not empty.
    /// </summary>
    public static IRuleBuilderOptions<T, Guid> GuidMustNotBeEmpty<T>(
        this IRuleBuilder<T, Guid> ruleBuilder,
        string propertyName)
    {
        return ruleBuilder
            .NotEmpty()
            .WithMessage($"{propertyName} is required")
            .WithErrorCode($"{propertyName}.Required");
    }

    /// <summary>
    /// Validates that a decimal value is greater than or equal to zero.
    /// </summary>
    public static IRuleBuilderOptions<T, decimal> MustBeNonNegative<T>(
        this IRuleBuilder<T, decimal> ruleBuilder,
        string propertyName)
    {
        return ruleBuilder
            .GreaterThanOrEqualTo(0)
            .WithMessage($"{propertyName} must be greater than or equal to 0")
            .WithErrorCode($"{propertyName}.Negative");
    }
}
