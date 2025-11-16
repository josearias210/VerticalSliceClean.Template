# Changelog

Todos los cambios notables en este template serán documentados en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es/1.0.0/),
y este proyecto sigue [Semantic Versioning](https://semver.org/lang/es/).

## [1.0.0] - 2025-11-16

### Added
- ✨ Template inicial `VerticalSliceClean.Template` (anteriormente `CleanVerticalSlice`)
- 🏗️ Vertical Slice Architecture con Clean Architecture
- 🔐 JWT Authentication con httpOnly cookies (refresh + access tokens)
- 🛡️ Account Lockout (5 intentos → 30 min)
- 🔍 Token Reuse Detection
- 📊 OpenTelemetry distributed tracing
- 📝 Structured logging con Serilog
- 🐳 Docker Compose para desarrollo local (SQL Server + Jaeger + Seq)
- 🚀 Docker Compose para producción (API + SQL Server Express + Caddy)
- 🔄 GitHub Actions CI/CD con build/deploy separados
- 📦 Deployment a VPS vía SSH con backups automáticos
- 🔒 Caddy reverse proxy con auto-HTTPS (Let's Encrypt)
- ✅ Health checks para DB y aplicación
- 🧪 Tests unitarios e integración con xUnit
- 📖 Documentación completa (Getting Started, Deployment, Docker, Migrations, ADRs)
- 🎨 Scalar + Swagger para documentación API
- 🔢 API Versioning (URL segment: `/api/v1/...`)
- ⚡ ErrorOr pattern para manejo funcional de errores
- 🎯 Typed Results para type safety
- 📋 FluentValidation con pipeline behavior
- 🔧 Strongly-typed configuration con IOptions<T> pattern
- 🗄️ EF Core 10 con SQL Server
- 🔄 Migrations con distributed locking (sp_getapplock)
- 🧹 Background job para limpieza de tokens (diario 3 AM)
- 🛠️ Script PowerShell `setup-dev.ps1` para setup automatizado

### Template Parameters
- `ClientName` - Nombre de la compañía/cliente
- `ProjectSuffix` - Sufijo del proyecto (Acme, Api, Platform, etc.)
- `DatabaseName` - Nombre de la base de datos
- `AdminEmail` - Email del administrador por defecto
- `AdminPassword` - Password del administrador por defecto
- `JwtIssuer` - URL del emisor de tokens JWT
- `JwtAudience` - URL de la audiencia JWT
- `CorsOrigin` - Origen CORS permitido
- `skipRestore` - Omitir restore automático de paquetes

### Stack Tecnológico
- .NET 10
- ASP.NET Core Minimal APIs
- Entity Framework Core 10
- MediatR 12
- FluentValidation 11
- ErrorOr 2
- Serilog 4
- OpenTelemetry 1.9
- xUnit 2.9
- SQL Server 2022
- Docker Compose
- Caddy 2
- GitHub Actions

### Documentation
- 📚 README.md con guía completa
- 🚀 GETTING_STARTED.md para primeros pasos
- 🐳 DOCKER_LOCAL.md para desarrollo local
- 📦 DEPLOYMENT.md para producción VPS
- ⚡ DEPLOYMENT_QUICKSTART.md para setup rápido
- 🗄️ MIGRATIONS.md para gestión de base de datos
- 🔒 SECURITY.md sobre seguridad y autenticación
- 📋 5 ADRs (Architecture Decision Records)
- 📝 TEMPLATE_USAGE.md para uso del template
- 📤 PUBLISHING.md para publicación
- 🔢 VERSIONING.md para manejo de versiones

---

## [Unreleased]

### Planned
- 🗄️ Soporte para PostgreSQL como alternativa a SQL Server
- 🧪 Mejoras en cobertura de tests
- 📊 Ejemplos adicionales de features con Vertical Slice
- 🌍 Internacionalización (i18n)
- 📧 Templates de emails
- 🔔 Sistema de notificaciones
- 📱 Endpoints para cambio de password y recuperación

---

## Formato de Cambios

### Added (Agregado)
Para nuevas características.

### Changed (Cambiado)
Para cambios en funcionalidad existente.

### Deprecated (Obsoleto)
Para características que pronto serán removidas.

### Removed (Removido)
Para características removidas.

### Fixed (Corregido)
Para correcciones de bugs.

### Security (Seguridad)
Para vulnerabilidades de seguridad.

---

## Tipos de Versiones

- **PATCH** (1.0.x): Correcciones de bugs, cambios menores sin breaking changes
- **MINOR** (1.x.0): Nuevas características compatibles hacia atrás
- **MAJOR** (x.0.0): Cambios que rompen compatibilidad con versiones anteriores

---

[Unreleased]: https://github.com/josearias210/VerticalSliceClean.Template/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/josearias210/VerticalSliceClean.Template/releases/tag/v1.0.0
