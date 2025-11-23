# ============================================
# Stage 1: Build
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first (for layer caching)
COPY ["Acme.slnx", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

# Copy all project files
COPY ["Acme.Domain/Acme.Domain.csproj", "Acme.Domain/"]
COPY ["Acme.Application/Acme.Application.csproj", "Acme.Application/"]
COPY ["Acme.Infrastructure/Acme.Infrastructure.csproj", "Acme.Infrastructure/"]
COPY ["Acme.Api/Acme.Api.csproj", "Acme.Api/"]
COPY ["Acme.Host/Acme.Host.csproj", "Acme.Host/"]

# Restore dependencies (cached layer if project files don't change)
RUN dotnet restore "Acme.Host/Acme.Host.csproj"

# Copy everything else
COPY . .

# Build and publish AppHost (which includes Api)
WORKDIR "/src/Acme.Host"
RUN dotnet publish "Acme.Host.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ============================================
# Stage 2: Runtime
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published files from build stage
COPY --from=build /app/publish .

# Create logs directory
RUN mkdir -p /app/logs && chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080;http://+:8081 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Run AppHost (which configures and starts the API)
ENTRYPOINT ["dotnet", "Acme.Host.dll"]
