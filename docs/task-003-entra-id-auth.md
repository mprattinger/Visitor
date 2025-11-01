# TASK-003: Entra ID Authentication Integration

## Overview
**Priority**: High  
**Dependencies**: TASK-001, TASK-002  
**Estimated Effort**: 3-4 days  
**Phase**: Authentication & Authorization

## Description
Implement Microsoft Entra ID authentication for employee and admin access with proper role-based authorization in the Blazor Server application.

## Acceptance Criteria
- [ ] Configure Entra ID authentication in Blazor Server application
- [ ] Implement role-based authorization with Employee and Admin roles
- [ ] Set up proper authentication middleware and policies
- [ ] Create authentication state management for Blazor components
- [ ] Implement logout functionality
- [ ] Handle authentication failures gracefully
- [ ] Configure user provisioning from Entra ID to local database
- [ ] Set up development environment authentication (with fallback for local testing)
- [ ] Implement proper session management and security

## Technical Requirements

### NuGet Packages
```bash
dotnet add package Microsoft.Identity.Web
dotnet add package Microsoft.Identity.Web.UI
dotnet add package Microsoft.Graph
dotnet add package Microsoft.AspNetCore.Authentication.OpenIdConnect
```

### Configuration Setup

#### appsettings.json
Add Entra ID configuration:
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "[Your-Tenant-ID]",
    "ClientId": "[Your-Client-ID]",
    "ClientSecret": "[Your-Client-Secret]",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  },
  "Authorization": {
    "AdminGroupId": "[AD-Group-ID-for-Admins]",
    "EmployeeGroupId": "[AD-Group-ID-for-Employees]"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=visitors.db"
  }
}
```

#### User Secrets (Development)
```bash
dotnet user-secrets init
dotnet user-secrets set "AzureAd:TenantId" "your-tenant-id"
dotnet user-secrets set "AzureAd:ClientId" "your-client-id"
dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"
```

## Implementation Tasks

### 1. Entra ID App Registration
**Manual Steps in Azure Portal:**
- [ ] Navigate to Azure Portal → Entra ID → App registrations
- [ ] Click "New registration"
- [ ] Set name: "VisitorTracking"
- [ ] Set redirect URI: `https://localhost:7000/signin-oidc` (development)
- [ ] Add redirect URI: `https://your-domain.com/signin-oidc` (production)
- [ ] Under "Certificates & secrets", create a new client secret
- [ ] Under "API permissions", add:
  - Microsoft Graph → User.Read (Delegated)
  - Microsoft Graph → GroupMember.Read.All (Application)
- [ ] Grant admin consent for the tenant

### 2. Authentication Service Implementation

**File: `Services/Interfaces/IUserService.cs`**
```csharp
using System.Security.Claims;
using VisitorTracking.Data.Entities;

namespace VisitorTracking.Services.Interfaces
{
    public interface IUserService
    {
        Task<User> GetOrCreateUserAsync(ClaimsPrincipal principal);
        Task<UserRole> GetUserRoleAsync(string userId);
        Task SyncUserFromEntraIdAsync(ClaimsPrincipal principal);
        Task UpdateLastLoginAsync(string userId);
    }
}
```

**File: `Services/UserService.cs`**
```csharp
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using VisitorTracking.Data.Context;
using VisitorTracking.Data.Entities;
using VisitorTracking.Services.Interfaces;

namespace VisitorTracking.Services
{
    public class UserService : IUserService
    {
        private readonly VisitorTrackingContext _context;
        private readonly GraphServiceClient _graphClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserService> _logger;

        public UserService(
            VisitorTrackingContext context,
            GraphServiceClient graphClient,
            IConfiguration configuration,
            ILogger<UserService> logger)
        {
            _context = context;
            _graphClient = graphClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<User> GetOrCreateUserAsync(ClaimsPrincipal principal)
        {
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? principal.FindFirst("sub")?.Value
                        ?? throw new InvalidOperationException("User ID not found in claims");

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                user = await CreateUserFromPrincipalAsync(principal);
            }
            else
            {
                await UpdateLastLoginAsync(userId);
            }

            return user;
        }

        public async Task<UserRole> GetUserRoleAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.Role ?? UserRole.Employee;
        }

        public async Task SyncUserFromEntraIdAsync(ClaimsPrincipal principal)
        {
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return;

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return;

            // Update user information from current claims
            user.Email = principal.FindFirst(ClaimTypes.Email)?.Value ?? user.Email;
            user.DisplayName = principal.FindFirst(ClaimTypes.Name)?.Value ?? user.DisplayName;

            // Determine role based on group membership
            var role = await DetermineUserRoleAsync(principal);
            user.Role = role;

            await _context.SaveChangesAsync();
        }

        public async Task UpdateLastLoginAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        private async Task<User> CreateUserFromPrincipalAsync(ClaimsPrincipal principal)
        {
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? throw new InvalidOperationException("User ID not found");

            var email = principal.FindFirst(ClaimTypes.Email)?.Value
                       ?? principal.FindFirst("preferred_username")?.Value
                       ?? "unknown@unknown.com";

            var displayName = principal.FindFirst(ClaimTypes.Name)?.Value
                            ?? principal.FindFirst("name")?.Value
                            ?? email;

            var role = await DetermineUserRoleAsync(principal);

            var user = new User
            {
                Id = userId,
                Email = email,
                DisplayName = displayName,
                Role = role,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new user: {Email} with role: {Role}", email, role);

            return user;
        }

        private async Task<UserRole> DetermineUserRoleAsync(ClaimsPrincipal principal)
        {
            try
            {
                var adminGroupId = _configuration["Authorization:AdminGroupId"];
                var employeeGroupId = _configuration["Authorization:EmployeeGroupId"];

                // Get user's group memberships from claims or Graph API
                var groups = principal.FindAll("groups").Select(c => c.Value).ToList();

                if (!groups.Any() && _graphClient != null)
                {
                    // Fallback to Graph API if groups not in claims
                    var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        var memberOf = await _graphClient.Users[userId].MemberOf.Request().GetAsync();
                        groups = memberOf.Select(g => g.Id).ToList();
                    }
                }

                if (!string.IsNullOrEmpty(adminGroupId) && groups.Contains(adminGroupId))
                {
                    return UserRole.Admin;
                }

                if (!string.IsNullOrEmpty(employeeGroupId) && groups.Contains(employeeGroupId))
                {
                    return UserRole.Employee;
                }

                // Default to Employee if no specific group found
                return UserRole.Employee;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining user role, defaulting to Employee");
                return UserRole.Employee;
            }
        }
    }
}
```

### 3. Program.cs Configuration

Add authentication configuration to `Program.cs`:
```csharp
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.Graph;
using VisitorTracking.Services;
using VisitorTracking.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add authentication
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddMicrosoftGraph(builder.Configuration.GetSection("MicrosoftGraph"))
    .AddInMemoryTokenCaches();

// Add authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("EmployeePolicy", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Employee", "Admin"));

    options.AddPolicy("AdminPolicy", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Admin"));
});

// Add services
builder.Services.AddScoped<IUserService, UserService>();

// Add Razor Pages with authentication
builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();

// Add server-side Blazor
builder.Services.AddServerSideBlazor()
    .AddMicrosoftIdentityConsentHandler();

var app = builder.Build();

// Configure middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
```

### 4. Authentication Components

**File: `Components/Shared/LoginDisplay.razor`**
```razor
@using Microsoft.AspNetCore.Components.Authorization
@inject NavigationManager Navigation

<AuthorizeView>
    <Authorized>
        <span>Hello, @context.User.Identity?.Name!</span>
        <a href="MicrosoftIdentity/Account/SignOut">Log out</a>
    </Authorized>
    <NotAuthorized>
        <a href="MicrosoftIdentity/Account/SignIn">Log in</a>
    </NotAuthorized>
</AuthorizeView>
```

**File: `Components/Shared/MainLayout.razor`** (Update)
```razor
@inherits LayoutComponentBase

<div class="page">
    <div class="sidebar">
        <NavMenu />
    </div>

    <main>
        <div class="top-row px-4">
            <LoginDisplay />
        </div>

        <article class="content px-4">
            <CascadingAuthenticationState>
                @Body
            </CascadingAuthenticationState>
        </article>
    </main>
</div>
```

### 5. Role-Based Claims Transformation

**File: `Services/ClaimsTransformation.cs`**
```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using VisitorTracking.Services.Interfaces;

namespace VisitorTracking.Services
{
    public class RoleClaimsTransformation : IClaimsTransformation
    {
        private readonly IUserService _userService;

        public RoleClaimsTransformation(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            if (principal.Identity?.IsAuthenticated != true)
                return principal;

            var user = await _userService.GetOrCreateUserAsync(principal);
            
            var claimsIdentity = new ClaimsIdentity();
            claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()));

            principal.AddIdentity(claimsIdentity);

            return principal;
        }
    }
}
```

Add to `Program.cs`:
```csharp
builder.Services.AddScoped<IClaimsTransformation, RoleClaimsTransformation>();
```

## Testing Requirements

### Manual Testing Checklist
- [ ] User can navigate to protected employee page and is redirected to Entra ID login
- [ ] After successful authentication, user is redirected back to the application
- [ ] User information is correctly stored in the database
- [ ] User role is correctly assigned based on group membership
- [ ] Admin users have access to admin features
- [ ] Employee users are restricted from admin features
- [ ] Logout functionality works correctly
- [ ] Session persists across page refreshes

### Integration Tests
```csharp
[Test]
public async Task Authentication_ShouldCreateUserOnFirstLogin()
{
    // Test user creation from claims principal
}

[Test]
public async Task RoleAssignment_ShouldAssignCorrectRole()
{
    // Test role assignment based on group membership
}
```

## Security Considerations
- [ ] HTTPS is enforced in production
- [ ] Client secrets are stored securely (User Secrets/Azure Key Vault)
- [ ] Token validation is properly configured
- [ ] CSRF protection is enabled
- [ ] Secure cookie settings are configured
- [ ] Session timeout is appropriately set
- [ ] Security headers are added (HSTS, X-Frame-Options, etc.)

## Definition of Done
- [ ] Employees can successfully authenticate with Entra ID credentials
- [ ] Role-based access control is functioning correctly
- [ ] User information is properly synced to local database
- [ ] Authentication state is properly managed in Blazor components
- [ ] Development environment supports both authenticated and guest scenarios
- [ ] Security best practices are implemented
- [ ] Error handling provides clear feedback to users
- [ ] Authentication performance meets requirements (< 3 seconds)
- [ ] Documentation for Entra ID app registration is complete

## Troubleshooting Guide

### Common Issues
- **Redirect URI mismatch**: Ensure redirect URIs in Azure match application URLs
- **Token validation failures**: Check tenant ID and client ID configuration
- **Role assignment issues**: Verify group IDs and user group memberships in Azure AD
- **Development authentication**: Use user secrets for local development credentials

## Dependencies for Next Tasks
- TASK-004 (Authorization Policies) builds on this authentication foundation
- TASK-010 (Employee Authentication Pages) requires this implementation
- All employee and admin features depend on this authentication system
