# TASK-015: Complete Role-Based Access Control (VT-010)

## Overview
**Priority**: Critical  
**Dependencies**: TASK-001 through TASK-014  
**Estimated Effort**: 2-3 days  
**Phase**: Security & Compliance  
**User Story**: VT-010

## Description
Comprehensive validation and enforcement of role-based access control across all application features.

## User Story
**As a** system administrator  
**I want to** ensure proper access control is enforced across all features  
**So that** users can only access functionality appropriate to their role

## Acceptance Criteria
- [ ] All pages and components enforce proper authorization policies
- [ ] API endpoints validate user permissions
- [ ] Unauthorized access attempts are logged and blocked
- [ ] Role transitions are handled securely
- [ ] Navigation menu adapts to user role
- [ ] Comprehensive authorization testing suite
- [ ] Security audit passes all checks

## Implementation

### Complete Authorization Configuration
**File: `Program.cs` (Enhanced)**
```csharp
using Microsoft.AspNetCore.Authorization;
using VisitorTracking.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Authentication & Authorization
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization(options =>
{
    // Guest Policy - Allow anonymous
    options.AddPolicy(AuthorizationPolicies.GuestPolicy, policy =>
        policy.RequireAssertion(context => true));

    // Employee Policy - Require authentication
    options.AddPolicy(AuthorizationPolicies.EmployeePolicy, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole(UserRole.Employee.ToString(), UserRole.Admin.ToString()));

    // Admin Policy - Require admin role
    options.AddPolicy(AuthorizationPolicies.AdminPolicy, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole(UserRole.Admin.ToString()));

    // Default policy
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Custom authorization handlers
builder.Services.AddScoped<IAuthorizationHandler, ResourceOwnerAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, VisitorManagementAuthorizationHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Content-Security-Policy", 
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';");
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

### Resource Owner Authorization Handler
**File: `Authorization/ResourceOwnerAuthorizationHandler.cs`**
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace VisitorTracking.Authorization;

public class ResourceOwnerAuthorizationHandler : 
    AuthorizationHandler<OperationAuthorizationRequirement, Visitor>
{
    private readonly IUserService _userService;

    public ResourceOwnerAuthorizationHandler(IUserService userService)
    {
        _userService = userService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        Visitor resource)
    {
        if (context.User == null)
            return;

        // Admins can do anything
        if (context.User.IsInRole(UserRole.Admin.ToString()))
        {
            context.Succeed(requirement);
            return;
        }

        // Check if user is the creator (for edit/delete operations)
        if (requirement.Name == Operations.Update || requirement.Name == Operations.Delete)
        {
            var user = await _userService.GetOrCreateUserAsync(context.User);
            if (resource.CreatedByUserId == user.Id)
            {
                context.Succeed(requirement);
                return;
            }
        }

        // Check if user can view (read operations)
        if (requirement.Name == Operations.Read)
        {
            if (context.User.IsInRole(UserRole.Employee.ToString()))
            {
                context.Succeed(requirement);
                return;
            }
        }
    }
}

public static class Operations
{
    public static OperationAuthorizationRequirement Create = 
        new() { Name = nameof(Create) };
    public static OperationAuthorizationRequirement Read = 
        new() { Name = nameof(Read) };
    public static OperationAuthorizationRequirement Update = 
        new() { Name = nameof(Update) };
    public static OperationAuthorizationRequirement Delete = 
        new() { Name = nameof(Delete) };
}
```

### Visitor Management Authorization Handler
**File: `Authorization/VisitorManagementAuthorizationHandler.cs`**
```csharp
using Microsoft.AspNetCore.Authorization;

namespace VisitorTracking.Authorization;

public class VisitorManagementAuthorizationHandler : 
    AuthorizationHandler<VisitorManagementRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        VisitorManagementRequirement requirement)
    {
        // Only authenticated users with Employee or Admin role
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var hasRole = context.User.IsInRole(UserRole.Employee.ToString()) ||
                         context.User.IsInRole(UserRole.Admin.ToString());
            
            if (hasRole)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}

public class VisitorManagementRequirement : IAuthorizationRequirement { }
```

### Role-Based Navigation Menu
**File: `Components/Layout/NavMenu.razor`**
```razor
@using Microsoft.AspNetCore.Authorization
@inject AuthenticationStateProvider AuthProvider
@inject IAuthorizationService AuthorizationService

<div class="top-row ps-3 navbar navbar-dark">
    <div class="container-fluid">
        <a class="navbar-brand" href="">Visitor Tracking</a>
    </div>
</div>

<input type="checkbox" title="Navigation menu" class="navbar-toggler" />

<div class="nav-scrollable" onclick="document.querySelector('.navbar-toggler').click()">
    <nav class="flex-column">
        <!-- Public/Guest Links -->
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="" Match="NavLinkMatch.All">
                <span class="bi bi-house-door-fill-nav-menu" aria-hidden="true"></span> Home
            </NavLink>
        </div>

        <div class="nav-item px-3">
            <NavLink class="nav-link" href="register">
                <span class="bi bi-person-plus-fill" aria-hidden="true"></span> Register Visit
            </NavLink>
        </div>

        <AuthorizeView Policy="@AuthorizationPolicies.EmployeePolicy">
            <!-- Employee Links -->
            <Authorized>
                <div class="nav-item px-3">
                    <NavLink class="nav-link" href="employee/dashboard">
                        <span class="bi bi-speedometer2" aria-hidden="true"></span> Dashboard
                    </NavLink>
                </div>

                <div class="nav-item px-3">
                    <NavLink class="nav-link" href="employee/visitors">
                        <span class="bi bi-list-ul" aria-hidden="true"></span> My Visitors
                    </NavLink>
                </div>

                <div class="nav-item px-3">
                    <NavLink class="nav-link" href="employee/visitors/preregister">
                        <span class="bi bi-calendar-plus" aria-hidden="true"></span> Pre-Register
                    </NavLink>
                </div>

                <div class="nav-item px-3">
                    <NavLink class="nav-link" href="employee/visitors/current">
                        <span class="bi bi-people-fill" aria-hidden="true"></span> Current Visitors
                    </NavLink>
                </div>
            </Authorized>
        </AuthorizeView>

        <AuthorizeView Policy="@AuthorizationPolicies.AdminPolicy">
            <!-- Admin Links -->
            <Authorized>
                <hr />
                <div class="nav-item px-3">
                    <NavLink class="nav-link" href="admin/dashboard">
                        <span class="bi bi-graph-up" aria-hidden="true"></span> Admin Dashboard
                    </NavLink>
                </div>

                <div class="nav-item px-3">
                    <NavLink class="nav-link" href="admin/visitors">
                        <span class="bi bi-table" aria-hidden="true"></span> Visitor Management
                    </NavLink>
                </div>

                <div class="nav-item px-3">
                    <NavLink class="nav-link" href="admin/users">
                        <span class="bi bi-person-badge" aria-hidden="true"></span> User Management
                    </NavLink>
                </div>

                <div class="nav-item px-3">
                    <NavLink class="nav-link" href="admin/audit">
                        <span class="bi bi-shield-check" aria-hidden="true"></span> Audit Log
                    </NavLink>
                </div>
            </Authorized>
        </AuthorizeView>
    </nav>
</div>
```

### Secure Service Methods with Authorization
**File: `Services/VisitorService.cs` (Enhanced)**
```csharp
public class VisitorService : IVisitorService
{
    private readonly VisitorTrackingContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public VisitorService(
        VisitorTrackingContext context,
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Visitor> UpdateVisitorAsync(Visitor visitor)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
            throw new UnauthorizedAccessException();

        var authResult = await _authorizationService.AuthorizeAsync(
            user, visitor, Operations.Update);

        if (!authResult.Succeeded)
            throw new UnauthorizedAccessException(
                "You do not have permission to update this visitor.");

        _context.Visitors.Update(visitor);
        await _context.SaveChangesAsync();
        return visitor;
    }

    public async Task DeleteVisitorAsync(string id)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
            throw new UnauthorizedAccessException();

        var visitor = await _context.Visitors.FindAsync(id);
        if (visitor == null)
            throw new KeyNotFoundException();

        var authResult = await _authorizationService.AuthorizeAsync(
            user, visitor, Operations.Delete);

        if (!authResult.Succeeded)
            throw new UnauthorizedAccessException(
                "You do not have permission to delete this visitor.");

        _context.Visitors.Remove(visitor);
        await _context.SaveChangesAsync();
    }
}
```

### Authorization Testing Suite
**File: `Tests/Authorization/AuthorizationTests.cs`**
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Xunit;

namespace VisitorTracking.Tests.Authorization;

public class AuthorizationTests
{
    [Fact]
    public async Task GuestUser_CannotAccessEmployeePages()
    {
        // Arrange
        var authService = CreateAuthorizationService();
        var user = new ClaimsPrincipal(); // Unauthenticated

        // Act
        var result = await authService.AuthorizeAsync(
            user, AuthorizationPolicies.EmployeePolicy);

        // Assert
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task EmployeeUser_CanAccessEmployeePages()
    {
        // Arrange
        var authService = CreateAuthorizationService();
        var user = CreateUserWithRole(UserRole.Employee);

        // Act
        var result = await authService.AuthorizeAsync(
            user, AuthorizationPolicies.EmployeePolicy);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task EmployeeUser_CannotAccessAdminPages()
    {
        // Arrange
        var authService = CreateAuthorizationService();
        var user = CreateUserWithRole(UserRole.Employee);

        // Act
        var result = await authService.AuthorizeAsync(
            user, AuthorizationPolicies.AdminPolicy);

        // Assert
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AdminUser_CanAccessAllPages()
    {
        // Arrange
        var authService = CreateAuthorizationService();
        var user = CreateUserWithRole(UserRole.Admin);

        // Act
        var employeeResult = await authService.AuthorizeAsync(
            user, AuthorizationPolicies.EmployeePolicy);
        var adminResult = await authService.AuthorizeAsync(
            user, AuthorizationPolicies.AdminPolicy);

        // Assert
        Assert.True(employeeResult.Succeeded);
        Assert.True(adminResult.Succeeded);
    }

    [Fact]
    public async Task Employee_CanOnlyEditOwnVisitors()
    {
        // Arrange
        var authService = CreateAuthorizationService();
        var user = CreateUserWithRole(UserRole.Employee);
        var ownVisitor = new Visitor { CreatedByUserId = "user-123" };
        var otherVisitor = new Visitor { CreatedByUserId = "user-456" };

        // Set user ID claim
        var identity = (ClaimsIdentity)user.Identity!;
        identity.AddClaim(new Claim("sub", "user-123"));

        // Act
        var ownResult = await authService.AuthorizeAsync(
            user, ownVisitor, Operations.Update);
        var otherResult = await authService.AuthorizeAsync(
            user, otherVisitor, Operations.Update);

        // Assert
        Assert.True(ownResult.Succeeded);
        Assert.False(otherResult.Succeeded);
    }

    [Fact]
    public async Task Admin_CanEditAllVisitors()
    {
        // Arrange
        var authService = CreateAuthorizationService();
        var user = CreateUserWithRole(UserRole.Admin);
        var visitor = new Visitor { CreatedByUserId = "other-user" };

        // Act
        var result = await authService.AuthorizeAsync(
            user, visitor, Operations.Update);

        // Assert
        Assert.True(result.Succeeded);
    }

    private ClaimsPrincipal CreateUserWithRole(UserRole role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Role, role.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    private IAuthorizationService CreateAuthorizationService()
    {
        // Setup authorization service with policies
        var services = new ServiceCollection();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.EmployeePolicy, policy =>
                policy.RequireRole(UserRole.Employee.ToString(), UserRole.Admin.ToString()));
            options.AddPolicy(AuthorizationPolicies.AdminPolicy, policy =>
                policy.RequireRole(UserRole.Admin.ToString()));
        });
        services.AddLogging();
        
        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IAuthorizationService>();
    }
}
```

## Security Checklist
- [ ] All sensitive pages have `[Authorize]` attributes
- [ ] Policies are correctly configured in Program.cs
- [ ] Service methods validate authorization
- [ ] Navigation menu respects user roles
- [ ] Unauthorized access attempts are logged
- [ ] Security headers are configured
- [ ] CSRF protection enabled
- [ ] Input validation on all forms
- [ ] SQL injection prevention (EF Core parameterization)
- [ ] XSS prevention (Razor auto-encoding)

## Definition of Done
- [ ] All authorization policies correctly enforced
- [ ] Navigation adapts to user role
- [ ] Service methods validate permissions
- [ ] Comprehensive authorization tests pass
- [ ] Security audit completed
- [ ] Unauthorized access logged
- [ ] Documentation updated
