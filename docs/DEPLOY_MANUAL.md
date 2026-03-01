# Manual Deployment Guide (Production)

This guide describes the steps needed to deploy the **Acme** application to a production environment using Docker Compose.

## Prerequisites

Make sure you have the following installed on the server:

*   **Docker Engine** (v20.10+)
*   **Docker Compose Plugin** (v2.0+)
*   **Git** (optional, to clone the repo)

## File Structure

For deployment, you need the following files on the server:

*   `docker-compose.yml`
*   `.env` (based on `.env.production.example`)
*   `Caddyfile` (proxy configuration)
*   Folders for volumes (Docker will create them automatically, but it's good to know):
    *   `postgres_data`
    *   `postgres_backups`
    *   `caddy_data`
    *   `api_logs`

## Deployment Steps

### 1. Prepare the Environment

1.  **Clone the repository** (or copy the necessary files):
    ```bash
    git clone https://github.com/josearias210/Acme.git
    cd Acme
    ```

2.  **Configure Environment Variables**:
    Copy the example file and edit it with your real values (passwords, domains, etc.).
    ```bash
    cp .env.production.example .env
    nano .env
    ```
    > [!IMPORTANT]
    > Make sure to set secure passwords for `POSTGRES_PASSWORD`, `JWT_SECRET_KEY`, and `ADMIN_PASSWORD`.

### 2. Start Services

Run the following command to download images and start containers in the background:

```bash
docker compose up -d
```

### 3. Verify Status

1.  **Check active containers**:
    ```bash
    docker compose ps
    ```
    You should see `acme-postgres-prod`, `acme-api-prod`, and `acme-caddy-prod` in `Up` status.

2.  **View API logs**:
    ```bash
    docker compose logs -f api
    ```

3.  **Verify connection**:
    Open your browser and visit `https://api.yourdomain.com/health` (or the domain you configured). It should respond `Healthy`.

## Update

To update to a new version of the application:

1.  **Download the latest image**:
    ```bash
    docker compose pull
    ```

2.  **Restart services** (only changed ones will be recreated):
    ```bash
    docker compose up -d
    ```

3.  **Clean old images** (optional):
    ```bash
    docker image prune -f
    ```

## Troubleshooting

### API Cannot Connect to Database
*   Verify that `POSTGRES_PASSWORD` in `.env` matches the database configuration.
*   Check the logs: `docker compose logs api`.

### SSL Certificate Error
*   Caddy automatically manages certificates. If there are issues, check Caddy logs:
    ```bash
    docker compose logs caddy
    ```
*   Make sure the domain points correctly to the server IP.

### Database Won't Start
*   Check Postgres logs: `docker compose logs postgres`.
*   Check volume folder permissions if problems persist.

## Backup and Recovery

The system includes an **automatic backup** service (`acme-db-backup`) that performs a complete database backup every 24 hours using `pg_dump`.

### Backup Location
`.sql.gz` files are stored in the `postgres_backups` volume. On the host, this is usually in `/var/lib/docker/volumes/...` or in the local folder if you configured a bind mount.

### Manual Restoration

To restore a database from a backup:

1.  **Identify the backup file**:
    ```bash
    # List available backups
    docker compose exec postgres ls -l /backups
    ```

2.  **Run restoration command**:
    ```bash
    # Decompress and restore (Replace the filename)
    docker compose exec -T postgres sh -c "zcat /backups/AcmeDb_YYYY-MM-DD_HHMM.sql.gz | psql -U postgres -d AcmeDb"
    ```
    > ⚠️ **Warning**: This will overwrite existing data if there are conflicts, but `pg_dump` usually generates scripts that recreate tables. It's recommended to drop the DB first if you want a clean restore:
    > `docker compose exec postgres psql -U postgres -c "DROP DATABASE \"AcmeDb\"; CREATE DATABASE \"AcmeDb\";"`

### Force Manual Backup
If you need to make a backup right now:
```bash
docker compose exec db-backup /scripts/backup-db.sh
```
