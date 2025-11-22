namespace Acme.Application.Common;

public static class ErrorCodes
{
    public static class Account
    {
        public const string FirstNameEmpty = "Account.FirstName.Required";
        public const string EmailEmpty = "Account.Email.Required";
        public const string PasswordEmpty = "Account.Password.Required";
        public const string RoleEmpty = "Account.Role.Required";
        public const string RoleInvalid = "Account.Role.Invalid";
        public const string CreateFailed = "Account.CreateFailed";
        public const string RoleAssignFailed = "Account.RoleAssignFailed";
        public const string EmailExists = "Account.EmailExists";
        public const string InsufficientPermissions = "Account.InsufficientPermissions";
    }
}
