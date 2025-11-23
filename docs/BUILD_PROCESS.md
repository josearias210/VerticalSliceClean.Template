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
dotnet test src/Acme.sln --configuration Release
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
