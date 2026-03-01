# Versionamiento del Template

Este documento explica cómo manejar las versiones del template `VerticalSliceClean.Template`.

## 📋 Semantic Versioning

El template sigue [Semantic Versioning 2.0.0](https://semver.org/):

```
MAJOR.MINOR.PATCH

0.1.0
│ │ │
│ │ └─── PATCH: Correcciones de bugs, cambios menores
│ └───── MINOR: Nuevas características, compatible hacia atrás
└─────── MAJOR: Cambios que rompen compatibilidad
```

### Ejemplos

| Versión | Tipo de Cambio | Ejemplos |
|---------|----------------|----------|
| **0.1.0** → **0.1.1** | PATCH | Corregir typo, actualizar dependencia menor, fix en documentación |
| **0.1.0** → **0.1.0** | MINOR | Agregar nuevo parámetro opcional, agregar feature opcional, mejorar documentación |
| **0.1.0** → **1.0.0** | MAJOR | Cambiar estructura de carpetas, remover parámetro, cambiar .NET version |

---

## 🔧 Actualizar Versión del Template

### Paso 1: Actualizar `template.json`

Edita `.template.config/template.json`:

```json
{
  "identity": "VerticalSliceClean.Template",
  "name": "VerticalSliceClean API Template",
  "version": "1.1.0",  // <-- Actualizar aquí
  ...
}
```

### Paso 2: Actualizar CHANGELOG (recomendado)

Crea o actualiza `CHANGELOG.md` en la raíz:

```markdown
# Changelog

## [1.1.0] - 2025-11-16

### Added
- Docker Compose para desarrollo local
- Script setup-dev.ps1 automatizado
- Documentación DOCKER_LOCAL.md

### Changed
- Mejorado README.md con instrucciones Docker

### Fixed
- Corregido puerto de Seq en appsettings.json
```

### Paso 3: Reinstalar Template

```powershell
# Desinstalar versión anterior
dotnet new uninstall VerticalSliceClean.Template

# Reinstalar con nueva versión
dotnet new install .

# Verificar versión instalada
dotnet new list vsclean
```

---

## 📦 Publicación de Versiones

### Opción 1: NuGet Package

Si publicas en NuGet, actualiza también el `.nuspec`:

```xml
<package>
  <metadata>
    <id>VerticalSliceClean.Template</id>
    <version>1.1.0</version>  <!-- Debe coincidir con template.json -->
    <releaseNotes>
      - Agregado Docker Compose para desarrollo local
      - Script de setup automatizado
    </releaseNotes>
    ...
  </metadata>
</package>
```

Luego empaqueta y publica:

```powershell
# Crear paquete
nuget pack VerticalSliceClean.Template.nuspec -OutputDirectory ./nupkg

# Publicar a NuGet.org
dotnet nuget push ./nupkg/VerticalSliceClean.Template.1.1.0.nupkg `
  --api-key YOUR_API_KEY `
  --source https://api.nuget.org/v3/index.json
```

### Opción 2: Git Tags (recomendado para GitHub)

```powershell
# Crear tag con la versión
git tag -a v1.1.0 -m "v1.1.0 - Docker Compose support"

# Subir tag a GitHub
git push origin v1.1.0
```

### Opción 3: Instalación Local/Compartida

Para equipos que instalan desde repositorio:

```powershell
# Los usuarios desinstalan la versión anterior
dotnet new uninstall VerticalSliceClean.Template

# Pull de los cambios
git pull origin main

# Reinstalar
dotnet new install .
```

---

## 🔍 Verificar Versión Instalada

```powershell
# Listar templates instalados con versión
dotnet new list

# Ver detalles del template
dotnet new vsclean --help
```

---

## 📊 Estrategia de Versionamiento Recomendada

### Versión Actual: 1.0.0

#### Cambios PATCH (1.0.x)
- ✅ Corregir errores en documentación
- ✅ Fix typos en código generado
- ✅ Actualizar dependencias PATCH (ej: 10.0.0 → 10.0.1)
- ✅ Mejorar comentarios en código

#### Cambios MINOR (1.x.0)
- ✅ Agregar nuevos parámetros opcionales al template
- ✅ Agregar nuevas features opcionales (ej: Docker Compose)
- ✅ Agregar documentación adicional
- ✅ Mejorar scripts de setup
- ✅ Actualizar dependencias MINOR (ej: 10.0.0 → 10.1.0)

#### Cambios MAJOR (x.0.0)
- ❌ Cambiar estructura de carpetas/proyectos
- ❌ Remover parámetros existentes
- ❌ Cambiar nombres de namespaces base
- ❌ Actualizar .NET version (ej: .NET 10 → .NET 11)
- ❌ Cambiar arquitectura fundamental

---

## 🔄 Migración Entre Versiones Mayores

Si necesitas hacer un cambio **MAJOR** (2.0.0), proporciona guía de migración:

### Ejemplo: Migración 1.x → 2.0

Crea `MIGRATION_v2.md`:

```markdown
# Migración a v2.0.0

## Breaking Changes

1. **Estructura de carpetas cambiada**
   - Antes: `src/[Client].[Project].Api/`
   - Ahora: `src/Api/[Client].[Project]/`

2. **Parámetro removido: `DatabaseName`**
   - Se infiere automáticamente de ClientName + ProjectSuffix

## Pasos de Migración

1. Generar nuevo proyecto con v2.0
2. Copiar tu lógica de negocio de `Features/`
3. Actualizar referencias de namespaces
4. Actualizar configuración en `appsettings.json`
```

---

## 📅 Calendario de Versiones

### Esquema Propuesto

| Tipo | Frecuencia | Cuándo |
|------|------------|--------|
| **PATCH** | Según necesidad | Bugs críticos, typos |
| **MINOR** | Mensual | Nuevas features, mejoras |
| **MAJOR** | Anual | Grandes refactorings, .NET upgrade |

### Roadmap Ejemplo

- **v1.0.0** (Actual) - Template base con JWT, Vertical Slice, Docker production
- **v0.1.2** (Q4 2025) - Docker Compose local, mejoras en docs
- **v0.1.1** (Q1 2026) - Soporte para PostgreSQL, mejoras en testing


---

## 🏷️ Convención de Commits (opcional)

Para facilitar el versionamiento automático, usa [Conventional Commits](https://www.conventionalcommits.org/):

```bash
# PATCH
git commit -m "fix: corregir validación de email en LoginCommand"
git commit -m "docs: actualizar README con instrucciones Docker"

# MINOR
git commit -m "feat: agregar Docker Compose para desarrollo local"
git commit -m "feat: agregar script setup-dev.ps1"

# MAJOR
git commit -m "feat!: cambiar estructura de carpetas a src/Api/"
git commit -m "BREAKING CHANGE: remover parámetro DatabaseName"
```

---

## 🤖 Build en GitHub Actions

El repositorio usa un único workflow de build en `.github/workflows/build-api.yml` para restaurar, compilar, ejecutar tests y construir/publicar imagen Docker según el evento.

Para ejecutarlo manualmente:

```powershell
# GitHub → Actions → Build API → Run workflow
```

---

## 📖 Recursos

- [Semantic Versioning](https://semver.org/)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [Keep a Changelog](https://keepachangelog.com/)
- [dotnet new versioning](https://learn.microsoft.com/en-us/dotnet/core/tools/custom-templates#versioning)

---

## ✅ Checklist para Nueva Versión

- [ ] Actualizar `version` en `.template.config/template.json`
- [ ] Actualizar `CHANGELOG.md` con cambios
- [ ] Actualizar `.nuspec` si publicas en NuGet
- [ ] Probar template con `dotnet new vsclean --dry-run`
- [ ] Crear commit con mensaje descriptivo
- [ ] Crear tag git `v1.x.x`
- [ ] Push tag a GitHub
- [ ] Publicar a NuGet (opcional)
- [ ] Notificar a usuarios/equipo
