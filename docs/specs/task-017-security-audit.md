# TASK-017: Security Audit & Compliance

## Overview
**Priority**: Critical  
**Dependencies**: All previous tasks  
**Estimated Effort**: 2-3 days  
**Phase**: Security & Compliance  

## Description
Comprehensive security audit and compliance validation to ensure the application meets security best practices and organizational requirements.

## Acceptance Criteria
- [ ] OWASP Top 10 vulnerabilities addressed
- [ ] Security headers properly configured
- [ ] Input validation and sanitization verified
- [ ] Authentication and authorization audited
- [ ] Data protection mechanisms validated
- [ ] Security logging and monitoring implemented
- [ ] Penetration testing completed
- [ ] Security documentation finalized

## Security Assessment

### OWASP Top 10 Checklist

#### 1. Broken Access Control
**Status**: ✅ Mitigated

**Implemented Controls**:
- Role-based access control (RBAC) with three roles: Guest, Employee, Admin
- Authorization policies enforced on all sensitive pages
- Resource-based authorization for visitor records
- Service-level authorization checks

**Verification**:
```csharp
// Test: Employee cannot access admin pages
[Fact]
public async Task Employee_CannotAccessAdminPages()
{
    var user = CreateEmployeeUser();
    var result = await _authService.AuthorizeAsync(user, AdminPolicy);
    Assert.False(result.Succeeded);
}
```

#### 2. Cryptographic Failures
**Status**: ✅ Mitigated

**Implemented Controls**:
- HTTPS enforced for all traffic
- Secure connection strings
- No sensitive data in logs
- Token generation uses cryptographic randomness

**Configuration**:
```csharp
// Force HTTPS
app.UseHttpsRedirection();
app.UseHsts();

// Secure token generation
private string GenerateVisitorToken()
{
    using var rng = RandomNumberGenerator.Create();
    var bytes = new byte[32];
    rng.GetBytes(bytes);
    return Convert.ToBase64String(bytes).Replace("/", "").Replace("+", "")[..12];
}
```

#### 3. Injection
**Status**: ✅ Mitigated

**Implemented Controls**:
- Entity Framework Core with parameterized queries
- Input validation using Data Annotations
- No raw SQL queries
- HTML encoding in Razor views

**Example**:
```csharp
// Parameterized query via EF Core
var visitor = await _context.Visitors
    .Where(v => v.VisitorToken == token)
    .FirstOrDefaultAsync();

// Input validation
[Required, StringLength(100)]
public string Name { get; set; } = "";
```

#### 4. Insecure Design
**Status**: ✅ Mitigated

**Implemented Controls**:
- Secure architecture with separation of concerns
- Principle of least privilege
- Defense in depth with multiple security layers
- Secure state transitions for visitor statuses

#### 5. Security Misconfiguration
**Status**: ⚠️ Needs Verification

**Security Headers Configuration**:
```csharp
// Program.cs - Security Headers Middleware
app.Use(async (context, next) =>
{
    // Prevent MIME type sniffing
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    
    // Prevent clickjacking
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    
    // Enable XSS protection
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    
    // Referrer policy
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    
    // Content Security Policy
    context.Response.Headers.Add("Content-Security-Policy", 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' https://cdn.jsdelivr.net; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none';");
    
    // HTTP Strict Transport Security
    context.Response.Headers.Add("Strict-Transport-Security", 
        "max-age=31536000; includeSubDomains");
    
    // Permissions Policy
    context.Response.Headers.Add("Permissions-Policy", 
        "geolocation=(), microphone=(), camera=()");

    await next();
});
```

**Dockerfile Security**:
```dockerfile
# Use specific version, not latest
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base

# Run as non-root user
RUN addgroup -g 1000 appuser && adduser -u 1000 -G appuser -s /bin/sh -D appuser
USER appuser

# Read-only root filesystem
VOLUME /app/data
```

#### 6. Vulnerable and Outdated Components
**Status**: ✅ Mitigated

**Dependency Management**:
```xml
<!-- Use latest stable versions -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
<PackageReference Include="Microsoft.Identity.Web" Version="2.15.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.OpenIdConnect" Version="8.0.0" />
```

**Automated Scanning**:
```yaml
# .github/workflows/security-scan.yml
name: Security Scan

on:
  schedule:
    - cron: '0 0 * * 0'  # Weekly on Sunday
  push:
    branches: [ main ]

jobs:
  security:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    
    - name: Run Dependency Check
      uses: dependency-check/Dependency-Check_Action@main
      with:
        project: 'VisitorTracking'
        path: '.'
        format: 'HTML'
    
    - name: Upload Results
      uses: actions/upload-artifact@v3
      with:
        name: dependency-check-report
        path: dependency-check-report.html
```

#### 7. Identification and Authentication Failures
**Status**: ✅ Mitigated

**Authentication Implementation**:
- Microsoft Entra ID integration
- No custom authentication logic
- Secure session management
- Proper logout handling

```csharp
// Secure authentication configuration
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
        options.SaveTokens = false; // Don't store tokens in cookie
        options.UseTokenLifetime = true;
    });
```

#### 8. Software and Data Integrity Failures
**Status**: ✅ Mitigated

**Implemented Controls**:
- Code signing in CI/CD pipeline
- Immutable Docker images with SHA256 checksums
- Audit logging for all data changes
- Database migrations version controlled

```csharp
// Audit logging service
public async Task LogActionAsync(string action, string details, string? entityId = null)
{
    var auditLog = new AuditLog
    {
        Id = Guid.NewGuid().ToString(),
        Action = action,
        Details = details,
        EntityId = entityId,
        UserId = GetCurrentUserId(),
        Timestamp = DateTime.UtcNow,
        IpAddress = GetClientIpAddress()
    };
    
    await _context.AuditLogs.AddAsync(auditLog);
    await _context.SaveChangesAsync();
}
```

#### 9. Security Logging and Monitoring Failures
**Status**: ⚠️ Needs Enhancement

**Logging Configuration**:
```json
// appsettings.Production.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "VisitorTracking": "Information"
    },
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  }
}
```

**Security Event Logging**:
```csharp
// SecurityEventLogger.cs
public class SecurityEventLogger : ISecurityEventLogger
{
    private readonly ILogger<SecurityEventLogger> _logger;

    public void LogUnauthorizedAccess(string userId, string resource)
    {
        _logger.LogWarning(
            "Unauthorized access attempt. UserId: {UserId}, Resource: {Resource}, IP: {IpAddress}",
            userId, resource, GetClientIpAddress());
    }

    public void LogAuthenticationFailure(string username, string reason)
    {
        _logger.LogWarning(
            "Authentication failure. Username: {Username}, Reason: {Reason}, IP: {IpAddress}",
            username, reason, GetClientIpAddress());
    }

    public void LogSuspiciousActivity(string description, string? userId = null)
    {
        _logger.LogWarning(
            "Suspicious activity detected. Description: {Description}, UserId: {UserId}, IP: {IpAddress}",
            description, userId, GetClientIpAddress());
    }
}
```

#### 10. Server-Side Request Forgery (SSRF)
**Status**: ✅ Not Applicable

- Application does not make outbound HTTP requests based on user input
- No URL processing or redirect functionality
- Entra ID authentication handled by Microsoft libraries

### Data Protection

**SQLite Database Security**:
```csharp
// Data protection for SQLite
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/data/keys"))
    .SetApplicationName("VisitorTracking");

// Connection string with encryption (if SQLite Encryption Extension available)
"Data Source=/app/data/visitors.db;Mode=ReadWriteCreate;Password=<encrypted-password>"
```

### Rate Limiting
**File: `Program.cs` (Add rate limiting)**
```csharp
using Microsoft.AspNetCore.RateLimiting;

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
    
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

app.UseRateLimiter();
```

### Security Testing Scripts

**File: `Tests/Security/SecurityTests.cs`**
```csharp
using Xunit;

namespace VisitorTracking.Tests.Security;

public class SecurityTests
{
    [Fact]
    public async Task Application_HasSecurityHeaders()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/");
        
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.True(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task SqlInjection_IsBlocked()
    {
        var maliciousInput = "'; DROP TABLE Visitors; --";
        var response = await _client.PostAsJsonAsync("/api/visitors/register", new
        {
            Name = maliciousInput,
            Company = "Test"
        });
        
        // Should succeed without executing injection
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Verify table still exists
        var visitors = await _context.Visitors.ToListAsync();
        Assert.NotNull(visitors);
    }

    [Fact]
    public async Task XSS_IsEncoded()
    {
        var xssPayload = "<script>alert('XSS')</script>";
        var visitor = await _service.RegisterVisitorAsync(new()
        {
            Name = xssPayload,
            Company = "Test"
        });
        
        // Should be stored as-is
        Assert.Equal(xssPayload, visitor.Name);
        
        // Should be encoded in HTML output
        var html = await RenderComponent(visitor);
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }
}
```

## Compliance Requirements

### GDPR Considerations
- **Data Minimization**: Only collect necessary visitor information
- **Right to Erasure**: Admin delete functionality implemented
- **Data Export**: CSV export functionality for data portability
- **Access Logging**: Audit trail for all data access

### Internal Security Policies
- [ ] Password policy enforced (via Entra ID)
- [ ] Multi-factor authentication enabled (via Entra ID)
- [ ] Session timeout configured
- [ ] Automatic logout after inactivity
- [ ] Secure data backup procedures

## Penetration Testing Checklist
- [ ] Authentication bypass attempts
- [ ] Authorization bypass attempts
- [ ] SQL injection testing
- [ ] XSS testing
- [ ] CSRF testing
- [ ] Session hijacking attempts
- [ ] Brute force protection
- [ ] API security testing

## Security Documentation

**File: `docs/SECURITY.md`**
```markdown
# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |

## Reporting a Vulnerability

Please report security vulnerabilities to: security@company.com

## Security Features

- Microsoft Entra ID authentication
- Role-based access control
- HTTPS enforcement
- Security headers
- Audit logging
- Rate limiting
- Input validation

## Security Best Practices

1. Keep all dependencies up to date
2. Review audit logs regularly
3. Monitor for suspicious activity
4. Conduct regular security assessments
5. Follow principle of least privilege
```

## Definition of Done
- [ ] All OWASP Top 10 vulnerabilities addressed
- [ ] Security headers properly configured
- [ ] Rate limiting implemented
- [ ] Security logging comprehensive
- [ ] Penetration testing completed
- [ ] Security documentation finalized
- [ ] Compliance requirements met
- [ ] Security audit report generated
