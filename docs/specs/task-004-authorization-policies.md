# TASK-004: Authorization Policies & Role Management

## Overview
**Priority**: Medium  
**Dependencies**: TASK-003  
**Estimated Effort**: 1-2 days  
**Phase**: Authentication & Authorization

## Description
Implement comprehensive authorization policies and role management system for different user types to ensure proper access control throughout the application.

## Acceptance Criteria
- [ ] Create authorization policies for Guest, Employee, and Admin roles
- [ ] Implement role-based component rendering in Blazor pages
- [ ] Set up authorization attributes for pages and components
- [ ] Create middleware for role verification and assignment
- [ ] Implement proper access denied handling
- [ ] Create role management utilities
- [ ] Test all authorization scenarios thoroughly

## Authorization Policies

### Policy Definitions
**File: `Constants/AuthorizationPolicies.cs`**
```csharp
namespace VisitorTracking.Constants
{
    public static class AuthorizationPolicies
    {
        public const string GuestPolicy = "GuestPolicy";
        public const string EmployeePolicy = "EmployeePolicy";
        public const string AdminPolicy = "AdminPolicy";
    }
}
```

### Policy Configuration in Program.cs
```csharp
using VisitorTracking.Constants;

builder.Services.AddAuthorization(options =>
{
    // Guest policy - allows unauthenticated access
    options.AddPolicy(AuthorizationPolicies.GuestPolicy, policy =>
        policy.RequireAssertion(context =>
            !context.User.Identity!.IsAuthenticated ||
            context.User.IsInRole("Employee") ||
            context.User.IsInRole("Admin")));

    // Employee policy - requires authenticated employee or admin
    options.AddPolicy(AuthorizationPolicies.EmployeePolicy, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Employee", "Admin"));

    // Admin policy - requires authenticated admin only
    options.AddPolicy(AuthorizationPolicies.AdminPolicy, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Admin"));

    // Set fallback policy to require authentication by default
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
```

## Implementation Tasks

### 1. Role Management Service

**File: `Services/Interfaces/IRoleManagementService.cs`**
```csharp
using System.Security.Claims;

namespace VisitorTracking.Services.Interfaces
{
    public interface IRoleManagementService
    {
        Task<bool> UserHasRole(ClaimsPrincipal user, string role);
        Task<IEnumerable<string>> GetUserRoles(ClaimsPrincipal user);
        Task<bool> IsAdmin(ClaimsPrincipal user);
        Task<bool> IsEmployee(ClaimsPrincipal user);
        Task<bool> CanAccessResource(ClaimsPrincipal user, string resourceType);
    }
}
```

**File: `Services/RoleManagementService.cs`**
```csharp
using System.Security.Claims;
using VisitorTracking.Data.Entities;
using VisitorTracking.Services.Interfaces;

namespace VisitorTracking.Services
{
    public class RoleManagementService : IRoleManagementService
    {
        private readonly IUserService _userService;
        private readonly ILogger<RoleManagementService> _logger;

        public RoleManagementService(
            IUserService userService,
            ILogger<RoleManagementService> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        public async Task<bool> UserHasRole(ClaimsPrincipal user, string role)
        {
            if (!user.Identity?.IsAuthenticated ?? false)
                return false;

            return user.IsInRole(role);
        }

        public async Task<IEnumerable<string>> GetUserRoles(ClaimsPrincipal user)
        {
            if (!user.Identity?.IsAuthenticated ?? false)
                return Enumerable.Empty<string>();

            return user.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();
        }

        public async Task<bool> IsAdmin(ClaimsPrincipal user)
        {
            return await UserHasRole(user, UserRole.Admin.ToString());
        }

        public async Task<bool> IsEmployee(ClaimsPrincipal user)
        {
            return await UserHasRole(user, UserRole.Employee.ToString()) ||
                   await UserHasRole(user, UserRole.Admin.ToString());
        }

        public async Task<bool> CanAccessResource(ClaimsPrincipal user, string resourceType)
        {
            return resourceType switch
            {
                "Visitors" => await IsEmployee(user),
                "AdminPanel" => await IsAdmin(user),
                "VisitorRegistration" => true, // Anyone can register
                _ => false
            };
        }
    }
}
```

Add to `Program.cs`:
```csharp
builder.Services.AddScoped<IRoleManagementService, RoleManagementService>();
```

### 2. Page-Level Authorization

**Example: Employee Dashboard**
**File: `Components/Pages/Employee/Dashboard.razor`**
```razor
@page "/employee/dashboard"
@using VisitorTracking.Constants
@attribute [Authorize(Policy = AuthorizationPolicies.EmployeePolicy)]

<PageTitle>Employee Dashboard</PageTitle>

<h3>Employee Dashboard</h3>

<AuthorizeView Roles="Admin">
    <Authorized>
        <div class="admin-section">
            <h4>Admin Controls</h4>
            <p>You have administrative privileges.</p>
        </div>
    </Authorized>
</AuthorizeView>

<!-- Employee content -->
```

**Example: Admin Panel**
**File: `Components/Pages/Admin/Panel.razor`**
```razor
@page "/admin/panel"
@using VisitorTracking.Constants
@attribute [Authorize(Policy = AuthorizationPolicies.AdminPolicy)]

<PageTitle>Admin Panel</PageTitle>

<h3>Administrative Panel</h3>

<!-- Admin content -->
```

**Example: Visitor Registration (Public)**
**File: `Components/Pages/Visitor/Register.razor`**
```razor
@page "/visitor/register"
@using VisitorTracking.Constants
@attribute [AllowAnonymous]

<PageTitle>Visitor Registration</PageTitle>

<h3>Visitor Registration</h3>

<!-- Public registration form -->
```

### 3. Component-Level Authorization

**File: `Components/Shared/RoleBasedContent.razor`**
```razor
@using Microsoft.AspNetCore.Components.Authorization
@inject IRoleManagementService RoleService

@if (IsAuthorized)
{
    @ChildContent
}
else if (ShowAccessDenied)
{
    <div class="alert alert-warning">
        <p>You do not have permission to access this content.</p>
    </div>
}

@code {
    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    [Parameter]
    public string? RequiredRole { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public bool ShowAccessDenied { get; set; } = false;

    private bool IsAuthorized { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (AuthenticationState == null)
        {
            IsAuthorized = false;
            return;
        }

        var authState = await AuthenticationState;
        var user = authState.User;

        if (string.IsNullOrEmpty(RequiredRole))
        {
            IsAuthorized = true;
            return;
        }

        IsAuthorized = await RoleService.UserHasRole(user, RequiredRole);
    }
}
```

**Usage Example:**
```razor
<RoleBasedContent RequiredRole="Admin" ShowAccessDenied="true">
    <button class="btn btn-danger">Delete All Records</button>
</RoleBasedContent>
```

### 4. Access Denied Handling

**File: `Components/Pages/AccessDenied.razor`**
```razor
@page "/access-denied"

<PageTitle>Access Denied</PageTitle>

<div class="access-denied-container">
    <h1>Access Denied</h1>
    <p>You do not have permission to access this resource.</p>
    <p>If you believe this is an error, please contact your system administrator.</p>

    <AuthorizeView>
        <Authorized>
            <a href="/" class="btn btn-primary">Return to Dashboard</a>
        </Authorized>
        <NotAuthorized>
            <a href="/visitor/register" class="btn btn-primary">Go to Visitor Registration</a>
        </NotAuthorized>
    </AuthorizeView>
</div>

@code {
    // Optional: Log access denied attempts
}
```

**File: `Middleware/AccessDeniedMiddleware.cs`**
```csharp
namespace VisitorTracking.Middleware
{
    public class AccessDeniedMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AccessDeniedMiddleware> _logger;

        public AccessDeniedMiddleware(
            RequestDelegate next,
            ILogger<AccessDeniedMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            if (context.Response.StatusCode == 403)
            {
                _logger.LogWarning(
                    "Access denied for user {User} to path {Path}",
                    context.User.Identity?.Name ?? "Anonymous",
                    context.Request.Path);

                if (!context.Response.HasStarted)
                {
                    context.Response.Redirect("/access-denied");
                }
            }
        }
    }
}
```

Add middleware to `Program.cs`:
```csharp
app.UseMiddleware<AccessDeniedMiddleware>();
```

### 5. Security Headers Middleware

**File: `Middleware/SecurityHeadersMiddleware.cs`**
```csharp
namespace VisitorTracking.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Add security headers
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

            // Content Security Policy
            context.Response.Headers.Add("Content-Security-Policy",
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data:; " +
                "font-src 'self'; " +
                "connect-src 'self'; " +
                "frame-ancestors 'none';");

            await _next(context);
        }
    }
}
```

Add to `Program.cs`:
```csharp
app.UseMiddleware<SecurityHeadersMiddleware>();
```

### 6. Anti-Forgery Configuration

Add to `Program.cs`:
```csharp
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "X-CSRF-TOKEN-COOKIE";
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
```

## Testing Requirements

### Authorization Policy Tests
**File: `Tests/Authorization/PolicyTests.cs`**
```csharp
using Microsoft.AspNetCore.Authorization;
using VisitorTracking.Constants;
using Xunit;

namespace VisitorTracking.Tests.Authorization
{
    public class PolicyTests
    {
        [Fact]
        public async Task AdminPolicy_RequiresAdminRole()
        {
            // Arrange
            var user = CreateUserWithRole("Employee");
            var authService = CreateAuthorizationService();

            // Act
            var result = await authService.AuthorizeAsync(
                user,
                null,
                AuthorizationPolicies.AdminPolicy);

            // Assert
            Assert.False(result.Succeeded);
        }

        [Fact]
        public async Task EmployeePolicy_AllowsAdminAndEmployee()
        {
            // Arrange & Act & Assert
            var admin = CreateUserWithRole("Admin");
            var employee = CreateUserWithRole("Employee");
            var authService = CreateAuthorizationService();

            var adminResult = await authService.AuthorizeAsync(
                admin, null, AuthorizationPolicies.EmployeePolicy);
            var employeeResult = await authService.AuthorizeAsync(
                employee, null, AuthorizationPolicies.EmployeePolicy);

            Assert.True(adminResult.Succeeded);
            Assert.True(employeeResult.Succeeded);
        }

        // Helper methods...
    }
}
```

### Component Authorization Tests
```csharp
[Fact]
public async Task RoleBasedContent_ShowsContentForAuthorizedUser()
{
    // Test role-based component rendering
}

[Fact]
public async Task RoleBasedContent_HidesContentForUnauthorizedUser()
{
    // Test access denial
}
```

## Definition of Done
- [ ] All authorization policies are properly configured and tested
- [ ] Role-based UI rendering works correctly across all components
- [ ] Unauthorized access attempts are properly handled with appropriate messages
- [ ] Role assignment from Entra ID groups functions correctly
- [ ] Security headers and protections are implemented
- [ ] Access denied pages provide clear user guidance
- [ ] Performance impact of authorization checks is minimal
- [ ] All authorization scenarios are covered by automated tests
- [ ] Anti-forgery protection is configured
- [ ] HTTPS redirection is enforced in production

## Security Checklist
- [ ] HTTPS is enforced
- [ ] Security headers are configured
- [ ] Anti-forgery tokens are validated
- [ ] Role-based access is properly enforced
- [ ] Access denied attempts are logged
- [ ] Session timeout is configured
- [ ] Secure cookies are used

## Dependencies for Next Tasks
- TASK-005 (Docker) can proceed independently
- TASK-007 through TASK-015 (Feature implementation) require these authorization policies
- All user-facing features depend on proper authorization implementation
