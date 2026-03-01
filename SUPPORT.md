# Support

## How to get help

If you need help using this template:

- Open a GitHub Issue for reproducible bugs or improvement requests.
- Include environment details (`OS`, `.NET SDK`, Docker version) and clear reproduction steps.

## Before opening an issue

- Check [README.md](README.md) and [docs/BUILD_PROCESS.md](docs/BUILD_PROCESS.md).
- Verify local setup with:
  - `dotnet restore src/Acme.slnx`
  - `dotnet build src/Acme.slnx --configuration Release --no-restore`
  - `dotnet test tests/Acme.Application.Unit.Tests/Acme.Application.Unit.Tests.csproj --configuration Release --no-build --no-restore`

## Security issues

Do not report security vulnerabilities in public issues.
Please follow [SECURITY.md](SECURITY.md).