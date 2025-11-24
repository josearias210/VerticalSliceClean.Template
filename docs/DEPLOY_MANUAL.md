# Guía de Despliegue Manual (Producción)

Esta guía describe los pasos necesarios para desplegar la aplicación **Acme** en un entorno de producción utilizando Docker Compose.

## Prerrequisitos

Asegúrate de tener instalado lo siguiente en el servidor:

*   **Docker Engine** (v20.10+)
*   **Docker Compose Plugin** (v2.0+)
*   **Git** (opcional, para clonar el repo)

## Estructura de Archivos

Para el despliegue, necesitas los siguientes archivos en el servidor:

*   `docker-compose.production.yml`
*   `.env` (basado en `.env.production.example`)
*   `Caddyfile` (configuración del proxy)
*   Carpetas para volúmenes (Docker las creará automáticamente, pero es bueno saberlo):
    *   `postgres_data`
    *   `postgres_backups`
    *   `caddy_data`
    *   `api_logs`

## Pasos de Despliegue

### 1. Preparar el Entorno

1.  **Clonar el repositorio** (o copiar los archivos necesarios):
    ```bash
    git clone https://github.com/josearias210/Acme.git
    cd Acme
    ```

2.  **Configurar Variables de Entorno**:
    Copia el archivo de ejemplo y edítalo con tus valores reales (contraseñas, dominios, etc.).
    ```bash
    cp .env.production.example .env
    nano .env
    ```
    > [!IMPORTANT]
    > Asegúrate de establecer contraseñas seguras para `POSTGRES_PASSWORD`, `JWT_SECRET_KEY` y `ADMIN_PASSWORD`.

### 2. Iniciar Servicios

Ejecuta el siguiente comando para descargar las imágenes e iniciar los contenedores en segundo plano:

```bash
docker compose up -d
```

### 3. Verificar el Estado

1.  **Comprobar contenedores activos**:
    ```bash
    docker compose ps
    ```
    Deberías ver `acme-postgres-prod`, `acme-api-prod` y `acme-caddy-prod` en estado `Up`.

2.  **Ver logs de la API**:
    ```bash
    docker compose logs -f api
    ```

3.  **Verificar conexión**:
    Abre tu navegador y visita `https://api.yourdomain.com/health` (o el dominio que hayas configurado). Debería responder `Healthy`.

## Actualización

Para actualizar a una nueva versión de la aplicación:

1.  **Descargar la última imagen**:
    ```bash
    docker compose pull
    ```

2.  **Reiniciar los servicios** (solo se recrearán los que hayan cambiado):
    ```bash
    docker compose up -d
    ```

3.  **Limpiar imágenes antiguas** (opcional):
    ```bash
    docker image prune -f
    ```

## Solución de Problemas

### La API no conecta a la Base de Datos
*   Verifica que `POSTGRES_PASSWORD` en el `.env` coincida con la configuración de la base de datos.
*   Revisa los logs: `docker compose logs api`.

### Error de Certificado SSL
*   Caddy gestiona automáticamente los certificados. Si hay problemas, revisa los logs de Caddy:
    ```bash
    docker compose logs caddy
    ```
*   Asegúrate de que el dominio apunte correctamente a la IP del servidor.

### La Base de Datos no inicia
*   Revisa los logs de Postgres: `docker compose logs postgres`.
*   Revisa los permisos de la carpeta de volumen si persisten los problemas.

## Copias de Seguridad y Recuperación

El sistema incluye un servicio de **backup automático** (`acme-db-backup`) que realiza una copia completa de la base de datos cada 24 horas usando `pg_dump`.

### Ubicación de los Backups
Los archivos `.sql.gz` se almacenan en el volumen `postgres_backups`. En el host, esto suele estar en `/var/lib/docker/volumes/...` o en la carpeta local si configuraste un bind mount.

### Restauración Manual

Para restaurar una base de datos desde un backup:

1.  **Identificar el archivo de backup**:
    ```bash
    # Listar backups disponibles
    docker compose exec postgres ls -l /backups
    ```

2.  **Ejecutar comando de restauración**:
    ```bash
    # Descomprimir y restaurar (Reemplaza el nombre del archivo)
    docker compose exec -T postgres sh -c "zcat /backups/AcmeDb_YYYY-MM-DD_HHMM.sql.gz | psql -U postgres -d AcmeDb"
    ```
    > ⚠️ **Advertencia**: Esto sobrescribirá los datos existentes si hay conflictos, pero `pg_dump` suele generar scripts que recrean tablas. Es recomendable borrar la DB antes si quieres una restauración limpia:
    > `docker compose exec postgres psql -U postgres -c "DROP DATABASE \"AcmeDb\"; CREATE DATABASE \"AcmeDb\";"`

### Forzar un Backup Manual
Si necesitas hacer un backup en este momento:
```bash
docker compose exec db-backup /scripts/backup-db.sh
```
