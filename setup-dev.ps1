# =============================================================================
# Setup de Desarrollo Local - CleanVerticalSlice Template
# =============================================================================
# Este script automatiza el setup inicial del proyecto
#
# Uso:
#   .\setup-dev.ps1
#
# =============================================================================

$ErrorActionPreference = "Stop"

Write-Host "🚀 VerticalSliceClean.Template - Setup de Desarrollo Local" -ForegroundColor Cyan
Write-Host "=" * 70 -ForegroundColor Cyan
Write-Host ""

# ---------------------------------------------------------------------------
# 1. Verificar Docker
# ---------------------------------------------------------------------------
Write-Host "📦 Verificando Docker..." -ForegroundColor Yellow

try {
    $dockerVersion = docker --version
    Write-Host "✅ Docker encontrado: $dockerVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Docker no está instalado o no está en el PATH" -ForegroundColor Red
    Write-Host "   Descarga Docker Desktop: https://www.docker.com/products/docker-desktop" -ForegroundColor Yellow
    exit 1
}

# Verificar que Docker esté corriendo
try {
    docker ps | Out-Null
    Write-Host "✅ Docker está corriendo" -ForegroundColor Green
} catch {
    Write-Host "❌ Docker no está corriendo. Por favor inicia Docker Desktop" -ForegroundColor Red
    exit 1
}

Write-Host ""

# ---------------------------------------------------------------------------
# 2. Verificar .NET SDK
# ---------------------------------------------------------------------------
Write-Host "🔧 Verificando .NET SDK..." -ForegroundColor Yellow

try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET SDK encontrado: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ .NET SDK no está instalado" -ForegroundColor Red
    Write-Host "   Descarga .NET 10: https://dotnet.microsoft.com/download/dotnet/10.0" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# ---------------------------------------------------------------------------
# 3. Crear archivo .env si no existe
# ---------------------------------------------------------------------------
Write-Host "📝 Configurando variables de entorno..." -ForegroundColor Yellow

if (-Not (Test-Path ".env")) {
    Write-Host "   Creando .env desde .env.example..." -ForegroundColor Cyan
    Copy-Item ".env.example" ".env"
    Write-Host "✅ Archivo .env creado" -ForegroundColor Green
} else {
    Write-Host "✅ Archivo .env ya existe" -ForegroundColor Green
}

Write-Host ""

# ---------------------------------------------------------------------------
# 4. Iniciar Docker Compose
# ---------------------------------------------------------------------------
Write-Host "🐳 Iniciando servicios de Docker..." -ForegroundColor Yellow
Write-Host "   - SQL Server (localhost:1433)" -ForegroundColor Cyan
Write-Host "   - Jaeger (http://localhost:16686)" -ForegroundColor Cyan
Write-Host "   - Seq (http://localhost:5341)" -ForegroundColor Cyan
Write-Host ""

docker compose up -d

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Servicios iniciados correctamente" -ForegroundColor Green
} else {
    Write-Host "❌ Error al iniciar servicios" -ForegroundColor Red
    exit 1
}

Write-Host ""

# ---------------------------------------------------------------------------
# 5. Esperar a que los servicios estén listos
# ---------------------------------------------------------------------------
Write-Host "⏳ Esperando a que los servicios estén listos..." -ForegroundColor Yellow

$maxRetries = 30
$retryCount = 0

while ($retryCount -lt $maxRetries) {
    $services = docker compose ps --format json | ConvertFrom-Json
    $healthyCount = 0
    
    foreach ($service in $services) {
        if ($service.Health -eq "healthy" -or $service.State -eq "running") {
            $healthyCount++
        }
    }
    
    if ($healthyCount -eq 3) {
        Write-Host "✅ Todos los servicios están listos" -ForegroundColor Green
        break
    }
    
    $retryCount++
    Write-Host "   Intentando ($retryCount/$maxRetries)..." -ForegroundColor Gray
    Start-Sleep -Seconds 2
}

if ($retryCount -eq $maxRetries) {
    Write-Host "⚠️  Algunos servicios pueden no estar listos. Verifica con: docker compose ps" -ForegroundColor Yellow
}

Write-Host ""

# ---------------------------------------------------------------------------
# 6. Mostrar estado de servicios
# ---------------------------------------------------------------------------
Write-Host "📊 Estado de servicios:" -ForegroundColor Yellow
docker compose ps
Write-Host ""

# ---------------------------------------------------------------------------
# 7. Instrucciones de User Secrets
# ---------------------------------------------------------------------------
Write-Host "🔐 Configurar User Secrets (siguiente paso):" -ForegroundColor Yellow
Write-Host ""
Write-Host "cd src\Acme.Infrastructure" -ForegroundColor Cyan
Write-Host ""
Write-Host "# JWT Settings" -ForegroundColor Green
Write-Host 'dotnet user-secrets set "JwtSettings:Key" "YourSuperSecretKeyThatIsAtLeast32CharactersLong1234567890"' -ForegroundColor Cyan
Write-Host 'dotnet user-secrets set "JwtSettings:Issuer" "https://yourcompany.com"' -ForegroundColor Cyan
Write-Host 'dotnet user-secrets set "JwtSettings:Audience" "https://yourcompany.com"' -ForegroundColor Cyan
Write-Host ""
Write-Host "# Admin User" -ForegroundColor Green
Write-Host 'dotnet user-secrets set "Admin:Email" "admin@yourcompany.com"' -ForegroundColor Cyan
Write-Host 'dotnet user-secrets set "Admin:Password" "Admin@123456"' -ForegroundColor Cyan
Write-Host ""
Write-Host "# Connection String" -ForegroundColor Green
Write-Host 'dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=YourDb;User Id=sa;Password=Local123*;TrustServerCertificate=True;MultipleActiveResultSets=true"' -ForegroundColor Cyan
Write-Host ""

# ---------------------------------------------------------------------------
# 8. Resumen final
# ---------------------------------------------------------------------------
Write-Host "=" * 70 -ForegroundColor Cyan
Write-Host "✅ Setup completado!" -ForegroundColor Green
Write-Host "=" * 70 -ForegroundColor Cyan
Write-Host ""
Write-Host "🌐 Acceso a servicios:" -ForegroundColor Yellow
Write-Host "   • SQL Server:  localhost:1433 (sa / Local123*)" -ForegroundColor White
Write-Host "   • Jaeger UI:   http://localhost:16686" -ForegroundColor White
Write-Host "   • Seq UI:      http://localhost:5341 (admin / Admin123!)" -ForegroundColor White
Write-Host ""
Write-Host "📖 Siguiente:" -ForegroundColor Yellow
Write-Host "   1. Configura User Secrets (comandos arriba)" -ForegroundColor White
Write-Host "   2. Ejecuta la aplicación: cd src\Acme.AppHost && dotnet run" -ForegroundColor White
Write-Host "   3. Abre Swagger: http://localhost:7297/swagger" -ForegroundColor White
Write-Host ""
Write-Host "📚 Documentación:" -ForegroundColor Yellow
Write-Host "   • Docker Compose: docs\DOCKER_LOCAL.md" -ForegroundColor White
Write-Host "   • Getting Started: docs\GETTING_STARTED.md" -ForegroundColor White
Write-Host ""
Write-Host "💡 Comandos útiles:" -ForegroundColor Yellow
Write-Host "   docker compose ps        # Ver estado de servicios" -ForegroundColor White
Write-Host "   docker compose logs -f   # Ver logs en tiempo real" -ForegroundColor White
Write-Host "   docker compose down      # Detener servicios" -ForegroundColor White
Write-Host ""
