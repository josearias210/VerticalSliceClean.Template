# Build Process

This document describes the automated Continuous Integration (CI) workflow using **GitHub Actions**.

## Workflow: `build-api.yml`

The definition file is located at `.github/workflows/build-api.yml`. This single workflow handles restore, build, tests, and Docker image build/push.

### 1. Triggers

The process starts automatically in the following scenarios:

*   **Pull Request events**: `opened`, `synchronize`, `reopened`, `closed` for `main` and `develop`.
*   **Manual**: Can be executed manually from GitHub's "Actions" tab.

### 2. Pipeline Stages

#### A. Restore + Build
```bash
dotnet restore src/Acme.slnx
dotnet build src/Acme.slnx --configuration Release --no-restore
```

#### B. Tests
```bash
dotnet test tests/Acme.Application.Unit.Tests/Acme.Application.Unit.Tests.csproj --configuration Release --no-build --no-restore
```
If tests fail, the process stops and the image is not published.

#### C. Docker Build
If the workflow is not a non-merged PR, it authenticates to **GHCR**, builds the image from `src/Acme.Host/Dockerfile`, and pushes it using metadata tags.

### 3. Image Tags

Docker metadata is generated with:

| Source | Tags |
| :--- | :--- |
| Branch / PR refs | Branch/PR tags from `docker/metadata-action` |
| Commit | `sha-xxxx` |
| Main branch | `latest` |

### 4. Manual Execution

To run the build manually:
1.  Go to the **Actions** tab in the GitHub repository.
2.  Select the **"Build API"** workflow in the left sidebar.
3.  Click **"Run workflow"**.
4.  Select the branch and click **Run workflow**.

### 5. Workflow Optimizations

- **Docker Cache**: Uses `gha` cache to speed up builds.
- **Timeouts**: Build job timeout is 20 minutes.
- **Concurrency**: Prevents overlapping runs per target ref.
- **Traceability**: Injects `VERSION` (SHA) and `BUILD_DATE` at build time.
