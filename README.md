# VerticalSliceClean API Template

> **🎯 .NET 10 API Template** with Clean Architecture and Vertical Slice patterns for rapid development with enterprise-grade features.

This is a **production-ready project template** (`dotnet new`) for building RESTful APIs with **JWT authentication**, **httpOnly cookies**, **Vertical Slice Architecture**, and **ErrorOr pattern** for functional error handling.

Built with **.NET 10**, **EF Core**, **MediatR**, **FluentValidation**, **Serilog**, and **OpenTelemetry**.

---

## 🚀 Quick Start

### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (Required for database & tools)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (For local development)

### 1. Start Development Environment
The easiest way to run the application is using Docker Compose, which sets up the API, Database (PostgreSQL), Logging (Seq), and Tracing (Jaeger).

1.  **Clone the repository**:
    ```bash
    git clone https://github.com/josearias210/Acme.git
    cd Acme
    ```

2.  **Configure Environment**:
    ```bash
    cp .env.example .env
    # Edit .env if needed (default values work for local dev)
    ```

3.  **Start Services**:
    ```bash
    docker compose up -d
    ```

4.  **Access Services**:
    -   **API**: http://localhost:8080
    -   **Swagger UI**: http://localhost:8080/swagger
    -   **Seq (Logs)**: http://localhost:5341 (admin / Admin123!)
    -   **Jaeger (Tracing)**: http://localhost:16686

### 2. Local Development (Source Code)
If you want to run the API from source (Visual Studio / VS Code) but keep infrastructure in Docker:

1.  **Start only infrastructure**:
    ```bash
    docker compose up postgres seq jaeger -d
    ```

2.  **Configure User Secrets** (Recommended for sensitive data):
    ```bash
    cd src/Acme.Host
    cat ../../secrets.json.example # View required secrets
    dotnet user-secrets init
    dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=AcmeDb;User Id=postgres;Password=postgres;"
    # Set other secrets as needed...
    ```

3.  **Run the App**:
    ```bash
    dotnet run
    ```

---

## 🏗️ Architecture

### **Vertical Slice Architecture**
- Each feature is self-contained (Command/Query + Handler + Validator + Endpoint)
- No shared business logic layers
- Easy to add/remove features without side effects

### **Clean Architecture Layers**
```
┌─────────────────────────────────────────┐
│           [Client].AppHost              │  ← Entry point, middleware pipeline
├─────────────────────────────────────────┤
│            [Client].Api                 │  ← Endpoints, Swagger, versioning
├─────────────────────────────────────────┤
│         [Client].Application            │  ← CQRS handlers, validators
├─────────────────────────────────────────┤
│        [Client].Infrastructure          │  ← DbContext, auth, services
├─────────────────────────────────────────┤
│           [Client].Domain               │  ← Entities, enums
└─────────────────────────────────────────┘
```

---

## ✨ Features

### **Security** 🔒
- ✅ JWT authentication with **httpOnly cookies**
- ✅ **Account Lockout** & **Token Reuse Detection**
- ✅ **Security Headers** & **Rate Limiting**
- ✅ **CORS** configured for development

### **Observability** 📊
- ✅ **Structured Logging** with Serilog (Console + Seq)
- ✅ **OpenTelemetry** distributed tracing (Jaeger)
- ✅ **Health Checks** for DB and app

### **Database** 💾
- ✅ **EF Core 10**
- ✅ **PostgreSQL** (Development) / **SQL Server** (Production ready)
- ✅ **Migrations** with distributed locking
- ✅ **Auto-creation** of database on first run

---

## 🚀 Deployment

### Manual Production Deployment
We provide a comprehensive guide for deploying to production using Docker Compose with SQL Server and Caddy (Reverse Proxy).

📖 **[Read the Deployment Guide](docs/DEPLOY_MANUAL.md)**

### Configuration Files
- **`docker-compose.yml`**: Development environment (Postgres, API, Tools).
- **`docker-compose.production.yml`**: Production environment (SQL Server, Caddy, API).
- **`.env.example`**: Template for development environment variables.
- **`.env.production.example`**: Template for production environment variables.
- **`secrets.json.example`**: Template for User Secrets (Local Development).

---

## 🛠️ Project Structure
```
Acme/
├── src/
│   ├── Acme.AppHost/       # Entry point
│   ├── Acme.Api/           # Endpoints
│   ├── Acme.Application/   # Business Logic
│   ├── Acme.Infrastructure/# Data & Services
│   └── Acme.Domain/        # Entities
├── docs/                   # Documentation
├── docker-compose.yml      # Dev Infrastructure
├── docker-compose.production.yml # Prod Infrastructure
└── README.md
```

---

## 📄 License
This project is licensed under the MIT License.

## 📞 Contact
**Jose Antonio Arias**
- Website: [programemos.net](https://programemos.net)
- GitHub: [@josearias210](https://github.com/josearias210)
