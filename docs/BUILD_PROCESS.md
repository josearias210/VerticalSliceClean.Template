# Proceso de Construcción y Lanzamiento (Build & Release)

Este documento describe el flujo de trabajo de Integración Continua (CI) y Entrega Continua (CD) automatizado mediante **GitHub Actions**.

## Flujo de Trabajo: `build-api.yml`

El archivo de definición se encuentra en `.github/workflows/build-api.yml`. Este flujo único maneja pruebas, construcción y publicación de imágenes Docker.

### 1. Disparadores (Triggers)

El proceso se inicia automáticamente en los siguientes escenarios:

*   **Merge de Pull Request**: Cuando un PR se fusiona en las ramas `master` o `develop`.
    *   *Propósito*: Asegurar que el código integrado es estable y generar imágenes de desarrollo (`edge`, `develop`).
*   **Etiquetas (Tags)**: Cuando se empuja un tag que comienza con `v` (ej. `v1.0.0`).
    *   *Propósito*: Generar una versión de lanzamiento (Release) con versionado semántico.
*   **Manual**: Se puede ejecutar manualmente desde la pestaña "Actions" de GitHub.
    *   *Propósito*: Re-construir una rama específica o probar el flujo sin cambios de código.

### 2. Etapas del Pipeline

#### A. Pruebas (`test`)
Antes de construir nada, el sistema ejecuta las pruebas automatizadas:
```bash
dotnet test src/Acme.slnx --configuration Release
```
Si las pruebas fallan, el proceso se detiene y no se genera ninguna imagen.

#### B. Construcción y Publicación (`build-and-push`)
Si las pruebas pasan:
1.  Se autentica en **GitHub Container Registry (GHCR)**.
2.  Construye la imagen Docker usando `src/Acme.Host/Dockerfile`.
3.  Etiqueta la imagen según la estrategia de versionado.
4.  Sube la imagen al registro privado.

### 3. Estrategia de Versionado

Las imágenes Docker se etiquetan automáticamente según el evento:

| Evento | Ref (Git) | Etiquetas Docker Generadas | Uso |
| :--- | :--- | :--- | :--- |
| **Release** | `v1.2.3` | `1.2.3`, `1.2`, `1`, `sha-xxxx` | Producción |
| **Merge Master** | `master` | `latest`, `sha-xxxx` | Staging / Latest Stable |
| **Merge Develop** | `develop` | `develop`, `sha-xxxx` | Desarrollo / Testing |

### 4. Ejecución Manual

Para ejecutar el build manualmente:
1.  Ve a la pestaña **Actions** en el repositorio de GitHub.
2.  Selecciona el flujo **"Build and Publish API"** en la barra lateral izquierda.
3.  Haz clic en **"Run workflow"**.
4.  Selecciona la rama (ej. `master`) y dale a **Run workflow**.

### 5. Dónde encontrar los Artefactos

Las imágenes construidas aparecen en la página principal del repositorio, en la sección **Packages** (barra lateral derecha).
URL típica: `ghcr.io/usuario/acme-api:etiqueta`

---

## 6. Deployment Automático a VPS

El sistema soporta **dos entornos** con deployment automático usando un único job parametrizado:

### Entorno de Desarrollo
-   **Trigger**: Merge a `develop`
-   **Environment**: `development` (GitHub)
-   **Secrets**: Configurados en environment `development`

### Entorno de Producción
-   **Trigger**: Merge a `master`
-   **Environment**: `production` (GitHub)
-   **Secrets**: Configurados en environment `production`

### Proceso de Deployment

1.  **Conexión SSH**: Se conecta al VPS correspondiente
2.  **Autenticación**: Login a GHCR usando el token temporal del workflow
3.  **Descarga**: `docker compose pull` fuerza la descarga de la imagen más reciente
4.  **Reinicio**: `docker compose up -d` reinicia los servicios con la nueva imagen
5.  **Verificación**: Espera y verifica que el API esté saludable

### Configuración de Environments en GitHub

GitHub Environments permite tener secrets separados por entorno.

#### Paso 1: Crear Environments
1.  Ve a tu repositorio → Settings → Environments
2.  Click "New environment"
3.  Crea dos environments:
    -   `development`
    -   `production`

#### Paso 2: Configurar Secrets por Environment

Para **cada environment**, agrega estos secrets:

| Secret | Descripción | Requerido | Default | Ejemplo |
| :--- | :--- | :--- | :--- | :--- |
| `VPS_HOST` | IP del VPS | ✅ Sí | - | `123.45.67.89` |
| `VPS_USERNAME` | Usuario SSH | ✅ Sí | - | `root` |
| `VPS_SSH_KEY` | Clave privada SSH | ✅ Sí | - | `-----BEGIN RSA...` |
| `VPS_PORT` | Puerto SSH | ❌ No | `22` | `22` |
| `VPS_DOMAIN` | Dominio | ✅ Sí | - | `api.yourdomain.com` |
| `VPS_PATH` | Path en VPS | ❌ No | `/opt/acme` | `/opt/acme` |
| `CONTAINER_NAME` | Nombre del container | ❌ No | `acme-api-prod` | `acme-api-prod` |

> **💡 Tip**: Si dev y prod están en **máquinas diferentes**, puedes usar el **mismo path** (`/opt/acme`) en ambas. Solo cambia `VPS_HOST` y `VPS_DOMAIN`.

#### Paso 3: Cómo Obtener la SSH Key

Si no tienes una clave SSH para el deployment:

**1. Generar nueva clave SSH:**
```bash
ssh-keygen -t rsa -b 4096 -f ~/.ssh/vps_deploy
```

**2. Ver la clave PRIVADA (para copiar al secret `VPS_SSH_KEY`):**
```bash
cat ~/.ssh/vps_deploy
```
Copia **TODO** el contenido, incluyendo las líneas `-----BEGIN RSA PRIVATE KEY-----` y `-----END RSA PRIVATE KEY-----`.

**3. Ver la clave PÚBLICA (para copiar al VPS):**
```bash
cat ~/.ssh/vps_deploy.pub
```

**4. Copiar la clave pública al VPS:**
```bash
# Opción 1: Automática
ssh-copy-id -i ~/.ssh/vps_deploy.pub root@TU_VPS_IP

# Opción 2: Manual
# Conecta al VPS y agrega la clave pública a ~/.ssh/authorized_keys
```

**5. Probar la conexión:**
```bash
ssh -i ~/.ssh/vps_deploy root@TU_VPS_IP
```

#### Ejemplo de Configuración Completa

**Environment: `development`**
```
VPS_HOST = 192.168.1.100
VPS_USERNAME = root
VPS_SSH_KEY = -----BEGIN RSA PRIVATE KEY-----
MIIEpAIBAAKCAQEA...
-----END RSA PRIVATE KEY-----
VPS_DOMAIN = dev.myapi.com
```

**Environment: `production`**
```
VPS_HOST = 203.0.113.50
VPS_USERNAME = root
VPS_SSH_KEY = -----BEGIN RSA PRIVATE KEY-----
MIIEpAIBAAKCAQEA...
-----END RSA PRIVATE KEY-----
VPS_DOMAIN = api.myapi.com
```


### Requisitos del VPS

Cada VPS debe tener en el path configurado (default: `/opt/acme`):
- `docker-compose.production.yml`
- `.env` (con variables del entorno correspondiente)
- `Caddyfile`




