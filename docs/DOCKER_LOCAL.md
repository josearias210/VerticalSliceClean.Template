# Docker Compose - Desarrollo Local

Esta guía explica cómo usar Docker Compose para desarrollo local.

## 📦 Servicios Incluidos

El `docker-compose.yml` incluye todos los servicios necesarios para desarrollo:

| Servicio | Puerto | Credenciales | Descripción |
|----------|--------|--------------|-------------|
| **SQL Server** | 1433 | sa / Local123* | Base de datos principal |
| **Jaeger** | 16686 | - | Distributed tracing UI |
| **Seq** | 5341 | admin / Admin123! | Structured logging UI |

## 🚀 Comandos Rápidos

### Iniciar Servicios
```powershell
# Iniciar todos los servicios en segundo plano
docker compose up -d

# Ver logs de todos los servicios
docker compose logs -f

# Ver logs de un servicio específico
docker compose logs -f sqlserver
docker compose logs -f jaeger
docker compose logs -f seq
```

### Estado de Servicios
```powershell
# Ver estado de todos los servicios
docker compose ps

# Verificar health checks
docker compose ps --format "table {{.Name}}\t{{.Status}}\t{{.Health}}"
```

### Detener Servicios
```powershell
# Detener servicios (mantiene volúmenes/datos)
docker compose down

# Detener y eliminar volúmenes (limpieza completa)
docker compose down -v

# Detener un servicio específico
docker compose stop sqlserver
```

### Reiniciar Servicios
```powershell
# Reiniciar todos los servicios
docker compose restart

# Reiniciar un servicio específico
docker compose restart sqlserver
```

## 🔧 Configuración Personalizada

### Cambiar Puertos o Passwords

Edita el archivo `.env` en la raíz del proyecto:

```env
# SQL Server
MSSQL_PORT=1433
MSSQL_SA_PASSWORD=Local123*

# Jaeger
JAEGER_UI_PORT=16686
JAEGER_OTLP_GRPC_PORT=4317
JAEGER_OTLP_HTTP_PORT=4318

# Seq
SEQ_UI_PORT=5341
SEQ_INGESTION_PORT=5342
SEQ_ADMIN_PASSWORD=Admin123!
```

Después de cambiar `.env`, reinicia los servicios:
```powershell
docker compose down
docker compose up -d
```

## 📊 Acceso a Interfaces Web

### Jaeger UI (Distributed Tracing)
- **URL**: http://localhost:16686
- **Uso**: Ver traces de requests, analizar performance, debugging

### Seq UI (Structured Logging)
- **URL**: http://localhost:5341
- **Login**: admin / Admin123!
- **Uso**: Buscar logs estructurados, filtrar por nivel/propiedades, crear dashboards

## 🗄️ SQL Server

### Conectar con Azure Data Studio / SSMS
```
Server: localhost,1433
Authentication: SQL Login
Username: sa
Password: Local123*
Trust Server Certificate: Yes
```

### Conectar desde la aplicación
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=YourDb;User Id=sa;Password=Local123*;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

### Ejecutar consultas desde terminal
```powershell
# Conectar a SQL Server container
docker compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Local123*" -C

# Ejecutar query directamente
docker compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Local123*" -C -Q "SELECT name FROM sys.databases"
```

## 💾 Gestión de Datos

### Backups
```powershell
# Backup de volúmenes
docker compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Local123*" -C -Q "BACKUP DATABASE [YourDb] TO DISK = '/var/opt/mssql/backup/YourDb.bak'"

# Copiar backup a host
docker compose cp sqlserver:/var/opt/mssql/backup/YourDb.bak ./YourDb.bak
```

### Restaurar Base de Datos
```powershell
# Copiar backup al container
docker compose cp ./YourDb.bak sqlserver:/var/opt/mssql/backup/

# Restaurar
docker compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Local123*" -C -Q "RESTORE DATABASE [YourDb] FROM DISK = '/var/opt/mssql/backup/YourDb.bak' WITH REPLACE"
```

### Limpiar Todo
```powershell
# Eliminar contenedores, volúmenes y datos
docker compose down -v

# Eliminar también imágenes
docker compose down -v --rmi all

# Reiniciar desde cero
docker compose up -d
```

## 🔍 Troubleshooting

### SQL Server no inicia
```powershell
# Ver logs detallados
docker compose logs sqlserver

# Verificar que el puerto 1433 no esté ocupado
netstat -an | findstr 1433

# Verificar recursos (SQL Server necesita ~2GB RAM)
docker stats
```

### Jaeger no muestra traces
1. Verifica que OpenTelemetry esté configurado en `appsettings.json`:
```json
{
  "OpenTelemetry": {
    "OtlpEndpoint": "http://localhost:4317",
    "ServiceName": "YourApp.Api",
    "ServiceVersion": "0.1.0"
  }
}
```

2. Verifica que la aplicación esté enviando traces:
```powershell
# Ver logs del servicio Jaeger
docker compose logs -f jaeger
```

### Seq no recibe logs
1. Verifica configuración de Serilog en `appsettings.json`:
```json
{
  "Logging": {
    "Seq": {
      "ServerUrl": "http://localhost:5341",
      "ApiKey": null
    }
  }
}
```

2. Verifica que Seq esté corriendo:
```powershell
docker compose ps seq
docker compose logs -f seq
```

### Puerto ocupado
```powershell
# Cambiar puerto en .env
# Ejemplo: cambiar SQL Server a puerto 1434
MSSQL_PORT=1434

# Actualizar connection string en User Secrets
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1434;..."

# Reiniciar servicios
docker compose down
docker compose up -d
```

## 📊 Monitoreo de Recursos

```powershell
# Ver consumo de CPU y RAM de cada servicio
docker stats

# Ver consumo de disco de volúmenes
docker system df -v

# Limpiar recursos no usados
docker system prune -a --volumes
```

## 🎯 Recomendaciones

1. **Siempre usa `.env`** para configuración local (nunca subas al repo)
2. **Ejecuta `docker compose down -v`** antes de cambiar schemas de DB
3. **Monitorea recursos** con `docker stats` si tu máquina es lenta
4. **Usa Seq y Jaeger** activamente durante desarrollo (son muy útiles)
5. **Haz backups** antes de operaciones destructivas

## 🔗 Links Útiles

- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [SQL Server on Docker](https://hub.docker.com/_/microsoft-mssql-server)
- [Jaeger Documentation](https://www.jaegertracing.io/docs/)
- [Seq Documentation](https://docs.datalust.co/docs)
