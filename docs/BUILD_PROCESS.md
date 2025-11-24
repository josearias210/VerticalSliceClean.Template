# Build and Release Process

This document describes the automated Continuous Integration (CI) and Continuous Delivery (CD) workflow using **GitHub Actions**.

## Workflow: `build-api.yml`

The definition file is located at `.github/workflows/build-api.yml`. This single workflow handles testing, building, and publishing Docker images.

### 1. Triggers

The process starts automatically in the following scenarios:

*   **Pull Request Merge**: When a PR is merged into `main` or `develop` branches.
    *   *Purpose*: Ensure integrated code is stable and generate development images (`edge`, `develop`).
*   **Tags**: When a tag starting with `v` is pushed (e.g. `v1.0.0`).
    *   *Purpose*: Generate a release version with semantic versioning.
*   **Manual**: Can be executed manually from GitHub's "Actions" tab.
    *   *Purpose*: Rebuild a specific branch or test the workflow without code changes.

### 2. Pipeline Stages

#### A. Tests (`test`)
Before building anything, the system runs automated tests:
```bash
dotnet test src/Acme.slnx --configuration Release
```
If tests fail, the process stops and no image is generated.

#### B. Build and Publish (`build-and-push`)
If tests pass:
1.  Authenticates to **GitHub Container Registry (GHCR)**.
2.  Builds the Docker image using `src/Acme.Host/Dockerfile`.
3.  Tags the image according to the versioning strategy.
4.  Pushes the image to the private registry.

### 3. Versioning Strategy

Docker images are automatically tagged based on the event:

| Event | Ref (Git) | Generated Docker Tags | Usage |
| :--- | :--- | :--- | :--- |
| **Release** | `v1.2.3` | `1.2.3`, `1.2`, `1`, `sha-xxxx` | Production |
| **Merge Main** | `main` | `latest`, `sha-xxxx` | Staging / Latest Stable |
| **Merge Develop** | `develop` | `develop`, `sha-xxxx` | Development / Testing |

### 4. Manual Execution

To run the build manually:
1.  Go to the **Actions** tab in the GitHub repository.
2.  Select the **"Build and Publish API"** workflow in the left sidebar.
3.  Click **"Run workflow"**.
4.  Select the branch (e.g. `main`) and click **Run workflow**.

### 5. Where to Find Artifacts

Built images appear on the repository's main page, in the **Packages** section (right sidebar).
Typical URL: `ghcr.io/username/acme-api:tag`

---

## 6. Automatic Deployment

The system automatically deploys to configured environments:

### Automatic Deployment
-   **Development**: When merging to `develop`.
-   **Production**: When merging to `main`.

### Manual Deployment
You can execute deployment manually from GitHub Actions:
1.  Go to the **Actions** tab.
2.  Select the **Build and Publish API** workflow.
3.  Click **Run workflow**.
4.  Select the branch (`develop` or `main`).
5.  Click the green **Run workflow** button.

### Deployment Process

1.  **SSH Connection**: Connects to the corresponding VPS
2.  **Authentication**: Login to GHCR using the workflow's temporary token
3.  **Download**: `docker compose pull` forces download of the latest image
4.  **Restart**: `docker compose up -d` restarts services with the new image
5.  **Verification**: Waits and verifies that the API is healthy

### Secrets Configuration

To configure the secrets needed for deployment, see the [Manual Deployment Guide](DEPLOY_MANUAL.md#secrets-configuration).

---

## 7. Workflow Optimizations

The pipeline includes several optimizations to improve performance and security:

### ⚡ Docker Cache
- **What it does**: Saves Docker layers in cache.
- **Benefit**: Significantly reduces build time.
- **Type**: GitHub Actions cache (gha).

### ⏱️ Security Timeouts
- **Build Job**: 20 minutes
- **Deploy Job**: 15 minutes
- **Benefit**: Prevents hung processes from consuming GitHub Actions minutes.

### 🧹 Automatic Cleanup
- **What it does**: Runs `docker image prune` after each deploy.
- **Rule**: Removes unused images created more than 7 days ago (`until=168h`).
- **Benefit**: Keeps VPS disk clean.

### 🔒 Concurrency Control
- **What it does**: Prevents simultaneous deployments to the same environment.
- **Behavior**: If you launch two deployments in a row, the second waits for the first to finish.

### 🏷️ Traceability
- **Version**: Injects Git SHA as `VERSION` environment variable.
- **Date**: Injects build date as `BUILD_DATE`.
