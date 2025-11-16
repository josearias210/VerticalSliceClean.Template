# VerticalSliceClean.Template - Publishing Guide

## Option 1: Local Installation (Development/Team Use)

### Install Locally
```powershell
# From template root directory
dotnet new install .
```

### Uninstall
```powershell
dotnet new uninstall VerticalSliceClean.Template
# or with full path
dotnet new uninstall C:\path\to\VerticalSliceClean.Template
```

### Update
```powershell
# Uninstall old version
dotnet new uninstall VerticalSliceClean.Template

# Reinstall
dotnet new install .
```

---

## Option 2: NuGet Package (Public/Private Feed)

### Prerequisites
1. NuGet account (for public) or private feed URL
2. API key for authentication

### Step 1: Create .nuspec File

Create `VerticalSliceClean.Template.nuspec` in root:

```xml
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd">
  <metadata>
    <id>VerticalSliceClean.Template</id>
    <version>1.0.0</version>
    <description>.NET 10 API Template with Clean Architecture, Vertical Slice, JWT, ErrorOr pattern</description>
    <authors>Jose Antonio Arias</authors>
    <packageTypes>
      <packageType name="Template" />
    </packageTypes>
    <tags>dotnet-new;templates;csharp;aspnet;webapi;jwt;cleanarchitecture</tags>
    <license type="expression">MIT</license>
    <projectUrl>https://github.com/josearias210/VerticalSliceClean.Template</projectUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <language>en-US</language>
  </metadata>
  <files>
    <file src="**\*" exclude="**\bin\**;**\obj\**;**\.vs\**;**\.git\**" target="content" />
  </files>
</package>
```

### Step 2: Pack the Template

```powershell
# Create nupkg
nuget pack VerticalSliceClean.Template.nuspec -OutputDirectory ./nupkg
```

### Step 3: Publish

#### To NuGet.org (Public)
```powershell
dotnet nuget push ./nupkg/VerticalSliceClean.Template.1.0.0.nupkg `
  --api-key YOUR_NUGET_API_KEY `
  --source https://api.nuget.org/v3/index.json
```

#### To Azure Artifacts (Private Feed)
```powershell
# Add source (one-time)
dotnet nuget add source `
  https://pkgs.dev.azure.com/yourorg/_packaging/yourfeed/nuget/v3/index.json `
  --name AzureArtifacts `
  --username anything `
  --password YOUR_PAT

# Push
dotnet nuget push ./nupkg/BaseNet10.Template.1.0.0.nupkg `
  --source AzureArtifacts
```

#### To GitHub Packages
```powershell
# Configure source
dotnet nuget add source `
  --username YOUR_GITHUB_USERNAME `
  --password YOUR_GITHUB_PAT `
  --store-password-in-clear-text `
  --name github `
  "https://nuget.pkg.github.com/YOUR_GITHUB_USERNAME/index.json"

# Push
dotnet nuget push ./nupkg/BaseNet10.Template.1.0.0.nupkg `
  --source github
```

### Step 4: Team Installation

```powershell
# From NuGet.org
dotnet new install VerticalSliceClean.Template

# From private feed
dotnet new install VerticalSliceClean.Template --nuget-source https://your-feed-url
```

---

## Option 3: GitHub Repository (Simple Sharing)

### Share via Git Clone

Team members can install directly from repository:

```powershell
# Clone
git clone https://github.com/josearias210/VerticalSliceClean.Template.git
cd VerticalSliceClean.Template

# Install
dotnet new install .
```

### Share via ZIP Download

1. Export repository as ZIP
2. Team downloads and extracts
3. Install:
```powershell
cd path\to\VerticalSliceClean.Template
dotnet new install .
```

---

## Versioning

Update version in these locations:

1. `.template.config/template.json`:
```json
{
  "identity": "VerticalSliceClean.Template",
  "version": "1.0.0",  // <-- HERE
  ...
}
```

2. `VerticalSliceClean.Template.nuspec` (if using NuGet):
```xml
<version>1.0.0</version>  <!-- HERE -->
```

3. Release notes in README

### Semantic Versioning

- **Major (1.x.x)**: Breaking changes (namespace changes, required parameter changes)
- **Minor (x.1.x)**: New features (new optional parameters, new projects)
- **Patch (x.x.1)**: Bug fixes (documentation, small fixes)

---

## Testing Before Publishing

### Create Test Project

```powershell
# Install locally
dotnet new install .

# Create test project
cd C:\temp
mkdir TestProject
cd TestProject
dotnet new cleanslice -n TestCompany.TestApi --ClientName TestCompany --ProjectSuffix TestApi

# Verify structure
dir src

# Build
cd src
dotnet build

# Clean up
cd C:\temp
rm -r TestProject
```

### Dry Run

```powershell
# See what will be created without actually creating files
dotnet new cleanslice -n Test.Api --dry-run
```

---

## CI/CD Publishing (GitHub Actions)

Create `.github/workflows/publish-template.yml`:

```yaml
name: Publish Template

on:
  release:
    types: [published]

jobs:
  publish:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      
      - name: Pack Template
        run: nuget pack VerticalSliceClean.Template.nuspec -OutputDirectory ./nupkg
      
      - name: Publish to NuGet
        run: |
          dotnet nuget push ./nupkg/*.nupkg \
            --api-key ${{ secrets.NUGET_API_KEY }} \
            --source https://api.nuget.org/v3/index.json
```

---

## Distribution Comparison

| Method            | Best For                | Pros                        | Cons                          |
|-------------------|-------------------------|-----------------------------|-------------------------------|
| Local Install     | Development/Testing     | Quick, no dependencies      | Manual updates                |
| NuGet.org         | Public sharing          | Easy discovery, automatic   | Review process, public        |
| Private NuGet     | Enterprise teams        | Secure, controlled          | Infrastructure cost           |
| GitHub Packages   | GitHub-based workflows  | Integrated with repo        | Requires GitHub auth          |
| Git Clone         | Simple team sharing     | No setup, version control   | Manual installation           |

---

## Troubleshooting

### Template Not Found After Install
```powershell
# Clear template cache
dotnet new --debug:reinit

# Verify installation
dotnet new list | Select-String "vsclean"
```

### Version Conflict
```powershell
# Uninstall all versions
dotnet new uninstall VerticalSliceClean.Template

# Reinstall specific version
dotnet new install VerticalSliceClean.Template::1.0.0
```

### NuGet Push Fails
```powershell
# Validate package
nuget verify -Signatures ./nupkg/VerticalSliceClean.Template.1.0.0.nupkg

# Test with different API key
dotnet nuget push --help
```

---

## Best Practices

1. **Test before publishing**: Always create a test project and build it
2. **Version consistently**: Update all version numbers together
3. **Document changes**: Keep CHANGELOG.md updated
4. **Use semantic versioning**: Follow semver for predictable updates
5. **Security**: Don't commit API keys; use environment variables or secrets
6. **Backup**: Tag releases in Git before publishing

---

## Support & Updates

- **Issues**: Report at GitHub repository
- **Updates**: Check for new versions quarterly
- **Breaking Changes**: Announced in release notes
- **Migration Guide**: Provided for major versions
