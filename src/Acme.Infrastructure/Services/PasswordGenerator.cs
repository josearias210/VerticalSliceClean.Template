namespace Acme.Infrastructure.Services
{
    using Acme.Application.Abstractions;
    using System.Security.Cryptography;

    public class PasswordGenerator : IPasswordGenerator
    {
        private const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string Lower = "abcdefghijklmnopqrstuvwxyz";
        private const string Digits = "0123456789";
        private const string Symbols = "!@#$%^&*()-_=+[]{};:,.<>?";
        private const string All = Upper + Lower + Digits + Symbols;

        public string Generate(int length = 16)
        {
            var data = new byte[length];
            RandomNumberGenerator.Fill(data);

            var result = new char[length];

            for (int i = 0; i < length; i++)
            {
                result[i] = All[data[i] % All.Length];
            }

            return new string(result);
        }

        public string GenerateStrong(int length = 16)
        {
            if (length < 8)
                throw new ArgumentException("Password length must be at least 8.");

            var password = new List<char>
        {
            Upper[RandomNumberGenerator.GetInt32(Upper.Length)],
            Lower[RandomNumberGenerator.GetInt32(Lower.Length)],
            Digits[RandomNumberGenerator.GetInt32(Digits.Length)],
            Symbols[RandomNumberGenerator.GetInt32(Symbols.Length)]
        };

            for (int i = password.Count; i < length; i++)
                password.Add(All[RandomNumberGenerator.GetInt32(All.Length)]);

            // Mezclar (Fisher-Yates)
            for (int i = password.Count - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (password[i], password[j]) = (password[j], password[i]);
            }

            return new string(password.ToArray());
        }
    }

}
