# Contributing

Thanks for contributing to VerticalSliceClean.Template.

## Development setup

1. Clone the repository.
2. Copy environment variables:
   - `cp .env.example .env`
3. Start local dependencies:
   - `docker compose -f docker-compose.local.yml up postgres seq jaeger -d`
4. Restore and test:
   - `dotnet restore src/Acme.slnx`
   - `dotnet test tests/Acme.Application.Unit.Tests/Acme.Application.Unit.Tests.csproj`

## Branching and pull requests

- Create a branch from `main` with a descriptive name.
- Keep PRs focused and small.
- Include:
  - What changed
  - Why it changed
  - How it was tested

## Coding expectations

- Follow the existing solution structure and naming conventions.
- Do not commit secrets or real credentials.
- Prefer minimal, targeted changes over broad refactors.

## Commit messages

Use clear, imperative commit messages, for example:

- `feat: add account lockout policy`
- `fix: handle invalid refresh token`
- `docs: update deployment prerequisites`

## Security issues

Do not open public issues for vulnerabilities.
Please follow `SECURITY.md` for responsible disclosure.