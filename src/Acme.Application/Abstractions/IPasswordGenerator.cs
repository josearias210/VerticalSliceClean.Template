namespace Acme.Application.Abstractions;

public interface IPasswordGenerator
{
    string Generate(int length = 16);
    string GenerateStrong(int length = 16);
}
