# TASK-001: Project Infrastructure Setup

## Overview
**Priority**: Critical  
**Dependencies**: None  
**Estimated Effort**: 1-2 days  
**Phase**: Infrastructure & Setup

## Description
Set up the foundational .NET 10 Blazor Server project structure with all required dependencies and initial configuration for the Company Visitor Tracking System.

## Acceptance Criteria
- [ ] Create new .NET 10 Blazor Server project with proper folder structure
- [ ] Configure project for SQLite database with Entity Framework Core
- [ ] Set up development environment configuration files (appsettings.json, appsettings.Development.json)
- [ ] Initialize Git repository with appropriate .gitignore for .NET projects
- [ ] Configure project for Docker containerization (Dockerfile preparation)
- [ ] Set up basic logging infrastructure (Serilog or built-in logging)
- [ ] Configure development HTTPS certificates
- [ ] Verify project builds and runs successfully in development environment
- [ ] Create basic project folder structure (Models, Services, Data, Components)

## Technical Requirements
- .NET 10 SDK
- Entity Framework Core 8.0+ with SQLite provider
- Basic Blazor Server template with Interactive Server components
- Development environment ready for Entra ID integration
- Docker support files (Dockerfile, .dockerignore)
- Logging framework configuration

## Implementation Tasks

### 1. Project Creation
```bash
dotnet new blazorserver -n VisitorTracking --framework net10.0
cd VisitorTracking
```

### 2. NuGet Packages to Install
```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Serilog.AspNetCore
dotnet add package Microsoft.AspNetCore.Authentication.OpenIdConnect
```

### 3. Folder Structure
Create the following directory structure:
```
/Data
  /Entities
  /Context
  /Migrations
/Models
  /ViewModels
  /DTOs
/Services
  /Interfaces
/Components
  /Pages
  /Shared
/wwwroot
```

### 4. Configuration Files Setup

#### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=visitors.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

#### appsettings.Development.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=visitors-dev.db"
  }
}
```

### 5. Git Initialization
```bash
git init
# Use standard .NET gitignore
curl -o .gitignore https://raw.githubusercontent.com/github/gitignore/main/VisualStudio.gitignore
git add .
git commit -m "Initial project setup"
```

### 6. Basic Dockerfile Preparation
Create a basic Dockerfile (will be enhanced in TASK-005):
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["VisitorTracking.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VisitorTracking.dll"]
```

### 7. .dockerignore File
```
**/bin/
**/obj/
**/.vs/
**/.vscode/
**/node_modules/
**/*.db
**/*.db-*
.git/
.gitignore
README.md
```

## Definition of Done
- [ ] Project structure is established and documented
- [ ] All team members can clone and run the project locally
- [ ] Basic Blazor pages render correctly (Home page accessible)
- [ ] Database connection is configured (but not yet migrated)
- [ ] Docker containerization is prepared but not yet functional
- [ ] Git repository is initialized with proper .gitignore
- [ ] Project builds without errors or warnings
- [ ] Development HTTPS certificate is configured and working
- [ ] All folder structures are in place
- [ ] README.md with setup instructions is created

## Notes
- This task establishes the foundation for all subsequent development
- Focus on clean architecture and proper separation of concerns
- Ensure all team members have consistent development environment setup
- Document any environment-specific setup requirements
- Keep configuration files ready for Entra ID settings (to be added in TASK-003)

## Dependencies for Next Tasks
- TASK-002 (Database Schema) depends on completion of this task
- All subsequent tasks require the foundation established here
