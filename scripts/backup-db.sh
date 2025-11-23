#!/bin/sh
set -e

# Configuration
# Env vars PGHOST, PGUSER, PGPASSWORD, PGDATABASE are used by pg_dump automatically or explicitly
DB_HOST="${PGHOST:-postgres}"
DB_USER="${PGUSER:-postgres}"
DB_NAME="${PGDATABASE:-AcmeDb}"
BACKUP_DIR="/backups"
RETENTION_DAYS="${RETENTION_DAYS:-7}"

# Wait for PostgreSQL to be ready
echo "Waiting for PostgreSQL at $DB_HOST..."
until pg_isready -h "$DB_HOST" -U "$DB_USER" > /dev/null 2>&1; do
  echo -n "."
  sleep 5
done
echo "PostgreSQL is ready."

# Main loop
while true; do
    TIMESTAMP=$(date +%Y-%m-%d_%H%M)
    BACKUP_FILE="$BACKUP_DIR/${DB_NAME}_${TIMESTAMP}.sql.gz"

    echo "Starting backup of $DB_NAME to $BACKUP_FILE..."

    # pg_dump with compression
    if pg_dump -h "$DB_HOST" -U "$DB_USER" "$DB_NAME" | gzip > "$BACKUP_FILE"; then
        echo "Backup completed successfully."
    else
        echo "Backup failed!"
        rm -f "$BACKUP_FILE"
    fi

    # Cleanup old backups
    echo "Cleaning up backups older than $RETENTION_DAYS days..."
    find "$BACKUP_DIR" -name "${DB_NAME}_*.sql.gz" -mtime +$RETENTION_DAYS -exec rm {} \;
    echo "Cleanup complete."

    # Wait for 24 hours
    echo "Sleeping for 24 hours..."
    sleep 86400
done
