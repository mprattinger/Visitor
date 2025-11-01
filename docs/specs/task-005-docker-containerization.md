# TASK-005: Docker Containerization

## Overview
**Priority**: Medium  
**Dependencies**: TASK-001, TASK-002, TASK-003  
**Estimated Effort**: 2-3 days  
**Phase**: Deployment & DevOps

## Description
Create Docker containerization setup for the application with proper configuration for production deployment to internal Docker infrastructure. After this task, the application should be fully runnable in Docker containers.

## Acceptance Criteria
- [ ] Create optimized multi-stage Dockerfile for .NET 10 Blazor Server application
- [ ] Configure Docker Compose for local development with SQLite persistence
- [ ] Set up environment variable configuration for different environments
- [ ] Implement proper Docker health checks
- [ ] Configure SQLite database persistence in Docker volumes
- [ ] Optimize Docker image size and build times
- [ ] Set up Docker networking for internal deployment
- [ ] Create development and production Docker configurations
- [ ] Application runs successfully in container with all features working

## Docker Configuration Files

### 1. Dockerfile (Production-Ready)
**File: `Dockerfile`**
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["VisitorTracking.csproj", "."]
RUN dotnet restore "VisitorTracking.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "VisitorTracking.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "VisitorTracking.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && \
    apt-get install -y curl && \
    rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN groupadd -r appuser && \
    useradd -r -g appuser appuser

# Create directories for database and logs
RUN mkdir -p /app/data /app/logs && \
    chown -R appuser:appuser /app

# Copy published application
COPY --from=publish /app/publish .

# Switch to non-root user
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "VisitorTracking.dll"]
```

### 2. .dockerignore
**File: `.dockerignore`**
```
# Build folders
**/bin/
**/obj/
**/out/

# IDE
**/.vs/
**/.vscode/
**/.idea/
*.user
*.suo

# Database files
**/*.db
**/*.db-shm
**/*.db-wal

# Git
.git/
.gitignore
.gitattributes

# Documentation
README.md
docs/

# Node modules (if any)
**/node_modules/

# Logs
**/logs/
**/*.log

# OS files
.DS_Store
Thumbs.db
```

### 3. Docker Compose (Development)
**File: `docker-compose.yml`**
```yaml
version: '3.8'

services:
  visitortracking:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: visitortracking-dev
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__DefaultConnection=Data Source=/app/data/visitors.db
    volumes:
      - sqlite_data:/app/data
      - app_logs:/app/logs
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    networks:
      - visitor-network

volumes:
  sqlite_data:
    driver: local
  app_logs:
    driver: local

networks:
  visitor-network:
    driver: bridge
```

### 4. Docker Compose (Production)
**File: `docker-compose.prod.yml`**
```yaml
version: '3.8'

services:
  visitortracking:
    image: ${REGISTRY:-your-registry}/visitortracking:${TAG:-latest}
    container_name: visitortracking-prod
    ports:
      - "80:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__DefaultConnection=Data Source=/app/data/visitors.db
      - AzureAd__TenantId=${AZURE_TENANT_ID}
      - AzureAd__ClientId=${AZURE_CLIENT_ID}
      - AzureAd__ClientSecret=${AZURE_CLIENT_SECRET}
    volumes:
      - /var/lib/visitortracking/data:/app/data
      - /var/log/visitortracking:/app/logs
    restart: always
    deploy:
      replicas: 2
      resources:
        limits:
          cpus: '1.0'
          memory: 512M
        reservations:
          cpus: '0.5'
          memory: 256M
      update_config:
        parallelism: 1
        delay: 10s
        order: start-first
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    networks:
      - visitor-network

networks:
  visitor-network:
    driver: bridge
```

## Implementation Tasks

### 1. Health Check Endpoint

**File: `Services/HealthChecks/DatabaseHealthCheck.cs`**
```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using VisitorTracking.Data.Context;

namespace VisitorTracking.Services.HealthChecks
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly VisitorTrackingContext _context;

        public DatabaseHealthCheck(VisitorTrackingContext context)
        {
            _context = context;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Try to query the database
                var canConnect = await _context.Database.CanConnectAsync(cancellationToken);

                if (canConnect)
                {
                    return HealthCheckResult.Healthy("Database is accessible");
                }

                return HealthCheckResult.Unhealthy("Cannot connect to database");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    "Database health check failed",
                    ex);
            }
        }
    }
}
```

**Add to Program.cs:**
```csharp
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using VisitorTracking.Services.HealthChecks;

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<VisitorTrackingContext>()
    .AddCheck<DatabaseHealthCheck>("database");

// Configure health check endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            })
        });
        await context.Response.WriteAsync(result);
    }
});
```

### 2. Docker Configuration Service

**File: `Services/DockerConfiguration.cs`**
```csharp
namespace VisitorTracking.Services
{
    public static class DockerConfiguration
    {
        public static void ConfigureForDocker(WebApplicationBuilder builder)
        {
            // Override connection strings for container paths
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            if (!string.IsNullOrEmpty(connectionString))
            {
                builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
            }

            // Configure logging for container environment
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddJsonConsole(options =>
            {
                options.IncludeScopes = true;
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
            });

            // Configure for running behind proxy
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                                          Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });
        }
    }
}
```

**Update Program.cs:**
```csharp
using VisitorTracking.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure for Docker if running in container
if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
{
    DockerConfiguration.ConfigureForDocker(builder);
}
```

### 3. Database Initialization

**File: `Services/DatabaseInitializer.cs`**
```csharp
using Microsoft.EntityFrameworkCore;
using VisitorTracking.Data.Context;

namespace VisitorTracking.Services
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider, ILogger logger)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<VisitorTrackingContext>();

                logger.LogInformation("Checking database...");

                // Ensure database exists
                await context.Database.EnsureCreatedAsync();

                // Run pending migrations
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    logger.LogInformation("Applying {Count} pending migrations", pendingMigrations.Count());
                    await context.Database.MigrateAsync();
                }

                // Seed data if needed
                await DbSeeder.SeedAsync(context);

                logger.LogInformation("Database initialized successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing the database");
                throw;
            }
        }
    }
}
```

**Update Program.cs:**
```csharp
var app = builder.Build();

// Initialize database on startup
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DatabaseInitializer.InitializeAsync(scope.ServiceProvider, logger);
}
```

### 4. Environment Configuration File

**File: `.env.example`**
```bash
# Application Environment
ASPNETCORE_ENVIRONMENT=Production

# Database
ConnectionStrings__DefaultConnection=Data Source=/app/data/visitors.db

# Azure AD / Entra ID
AZURE_TENANT_ID=your-tenant-id
AZURE_CLIENT_ID=your-client-id
AZURE_CLIENT_SECRET=your-client-secret

# Authorization
Authorization__AdminGroupId=your-admin-group-id
Authorization__EmployeeGroupId=your-employee-group-id

# Container Registry
REGISTRY=your-registry.azurecr.io
TAG=latest
```

### 5. Docker Build and Run Scripts

**File: `scripts/docker-build.sh`**
```bash
#!/bin/bash
set -e

echo "Building Docker image..."

# Build the image
docker build -t visitortracking:latest .

echo "✓ Docker image built successfully"

# Optional: Tag for registry
if [ ! -z "$REGISTRY" ]; then
    docker tag visitortracking:latest $REGISTRY/visitortracking:${TAG:-latest}
    echo "✓ Image tagged for registry: $REGISTRY/visitortracking:${TAG:-latest}"
fi
```

**File: `scripts/docker-run.sh`** (PowerShell version)
```powershell
# scripts/docker-run.ps1
Write-Host "Starting VisitorTracking application..." -ForegroundColor Green

# Load environment variables
if (Test-Path .env) {
    Get-Content .env | ForEach-Object {
        if ($_ -match '^\s*([^#][^=]+)=(.*)$') {
            $name = $matches[1].Trim()
            $value = $matches[2].Trim()
            [Environment]::SetEnvironmentVariable($name, $value, "Process")
        }
    }
}

# Start with Docker Compose
docker-compose up -d

Write-Host "✓ Application started" -ForegroundColor Green
Write-Host "Access the application at: http://localhost:5000" -ForegroundColor Yellow

# Show logs
docker-compose logs -f
```

**File: `scripts/docker-stop.ps1`**
```powershell
Write-Host "Stopping VisitorTracking application..." -ForegroundColor Yellow

docker-compose down

Write-Host "✓ Application stopped" -ForegroundColor Green
```

## Testing Requirements

### Container Tests
Create test script: `scripts/test-docker.ps1`
```powershell
Write-Host "Testing Docker container..." -ForegroundColor Green

# Build image
docker build -t visitortracking:test .

# Run container
docker run -d --name visitortracking-test `
    -p 5001:8080 `
    -e ASPNETCORE_ENVIRONMENT=Development `
    visitortracking:test

# Wait for startup
Start-Sleep -Seconds 10

# Test health endpoint
$response = Invoke-WebRequest -Uri "http://localhost:5001/health" -UseBasicParsing

if ($response.StatusCode -eq 200) {
    Write-Host "✓ Health check passed" -ForegroundColor Green
} else {
    Write-Host "✗ Health check failed" -ForegroundColor Red
    exit 1
}

# Cleanup
docker stop visitortracking-test
docker rm visitortracking-test

Write-Host "✓ Container tests passed" -ForegroundColor Green
```

### Checklist
- [ ] Image builds without errors
- [ ] Container starts successfully
- [ ] Health check endpoint responds
- [ ] Database is created and migrations run
- [ ] Application is accessible on configured port
- [ ] Logs are properly output
- [ ] Container can be stopped and restarted
- [ ] Database persists across container restarts
- [ ] Environment variables are properly loaded

## Performance Optimization

### Image Size Reduction
- Multi-stage builds implemented
- Only necessary runtime dependencies included
- Layer caching optimized for faster builds

### Runtime Optimization
```csharp
// Add to Program.cs for production
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
    });

    builder.Services.AddResponseCaching();
}
```

## Documentation

### Quick Start Guide
Create `docs/docker-quick-start.md`:
```markdown
# Docker Quick Start

## Prerequisites
- Docker installed
- Docker Compose installed

## Development
1. Clone the repository
2. Copy `.env.example` to `.env` and configure
3. Run: `docker-compose up -d`
4. Access: http://localhost:5000

## Production
1. Build: `docker build -t visitortracking .`
2. Run: `docker-compose -f docker-compose.prod.yml up -d`

## Useful Commands
- View logs: `docker-compose logs -f`
- Stop: `docker-compose down`
- Rebuild: `docker-compose up -d --build`
```

## Definition of Done
- [ ] Application runs successfully in Docker container
- [ ] Database persistence works correctly across container restarts
- [ ] Environment configuration is properly externalized
- [ ] Docker image builds efficiently and is optimized for size
- [ ] Health checks are functioning correctly
- [ ] Container security best practices are implemented
- [ ] Development and production configurations are tested
- [ ] Documentation is complete with quick start guide
- [ ] All scripts are executable and tested
- [ ] Application is fully functional when accessed through container

## Troubleshooting Guide

### Common Issues
1. **Port already in use**: Change port mapping in docker-compose.yml
2. **Database locked**: Ensure only one container accesses database
3. **Health check failing**: Check application logs and startup time
4. **Permission denied**: Verify volume permissions and user configuration

## Dependencies for Next Tasks
- TASK-006 (CI/CD Pipeline) requires Docker configuration
- Production deployment depends on successful containerization
- All subsequent deployment activities build on this foundation
