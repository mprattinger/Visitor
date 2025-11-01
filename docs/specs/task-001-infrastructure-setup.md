# TASK-001: Project Infrastructure Setup

## Overview
**Priority**: Critical  
**Dependencies**: None  
**Estimated Effort**: 1-2 days  
**Phase**: Infrastructure & Setup

## Description
Set up the foundational .NET 10 Blazor Server project structure using Domain-Driven Design (DDD) and Vertical Slice Architecture, with all required dependencies and initial configuration for the Company Visitor Tracking System.

## Acceptance Criteria
- [ ] Create new .NET 10 Blazor Server project with folder structure supporting DDD, Vertical Slice Architecture, and CQRS (using FlintSoft.CQRS)
- [ ] Configure project for SQLite database with Entity Framework Core
- [ ] Set up development environment configuration files (appsettings.json, appsettings.Development.json)
- [ ] Initialize Git repository with appropriate .gitignore for .NET projects
- [ ] Configure project for Docker containerization (Dockerfile preparation)
- [ ] Set up basic logging infrastructure (Serilog or built-in logging)
- [ ] Configure development HTTPS certificates
- [ ] Verify project builds and runs successfully in development environment
- [ ] Create vertical slice feature folders (Features, Domain, Infrastructure, Shared, etc.)
- [ ] Integrate CQRS pattern with FlintSoft.CQRS for request/response handling

## Technical Requirements
- .NET 10 SDK
- Entity Framework Core 8.0+ with SQLite provider
- Blazor Server template with Interactive Server components
- Domain-Driven Design principles applied to solution structure
- Vertical Slice Architecture for feature organization
- CQRS pattern implemented using FlintSoft.CQRS (Mediator clone)
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
# Add FlintSoft.CQRS for CQRS/Mediator pattern
dotnet add package FlintSoft.CQRS --source https://nuget.pkg.github.com/mprattinger/index.json
```

### 3. Folder Structure
Create the following directory structure, reflecting DDD, Vertical Slice Architecture, and CQRS:
```
/Features
  /VisitorRegistration
  /VisitorStatusManagement
  /EmployeePreregistration
  /AdminDashboard
    /Commands
    /Queries
    /Handlers
/Domain
  /Entities
  /ValueObjects
  /Aggregates
  /Events
/Infrastructure
  /Persistence
  /Logging
  /Authentication
/Shared
  /DTOs
  /ViewModels
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
- [ ] Project structure is established and documented, following DDD, Vertical Slice Architecture, and CQRS
- [ ] All team members can clone and run the project locally
- [ ] Basic Blazor pages render correctly (Home page accessible)
- [ ] Database connection is configured (but not yet migrated)
- [ ] Docker containerization is prepared but not yet functional
- [ ] Git repository is initialized with proper .gitignore
- [ ] Project builds without errors or warnings
- [ ] Development HTTPS certificate is configured and working
- [ ] All folder structures are in place, including vertical slice feature folders and CQRS structure
- [ ] README.md with setup instructions is created

## Notes
- This task establishes the foundation for all subsequent development
- Focus on clean architecture, Domain-Driven Design, CQRS, and proper separation of concerns
- Organize features using Vertical Slice Architecture (each feature in its own folder with commands, queries, handlers, models, and UI components)
- Use FlintSoft.CQRS for implementing the Mediator pattern and CQRS request/response handling
- Ensure all team members have consistent development environment setup
- Document any environment-specific setup requirements
- Keep configuration files ready for Entra ID settings (to be added in TASK-003)

## Dependencies for Next Tasks
- TASK-002 (Database Schema) depends on completion of this task
- All subsequent tasks require the foundation established here
