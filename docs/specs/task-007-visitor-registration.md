# TASK-007: Guest Visitor Registration (VT-001)

## Overview
**Priority**: High  
**Dependencies**: TASK-002, TASK-004, TASK-005  
**Estimated Effort**: 2-3 days  
**Phase**: Core Functionality Implementation  
**User Story**: VT-001

## Description
Implement the visitor self-registration functionality allowing guests to register without authentication by providing their name, company, and planned duration.

## User Story
**As a** company visitor  
**I want to** register my visit by providing my name, company, and planned duration  
**So that** the company has a record of my presence

## Acceptance Criteria
- [ ] Visitor can access registration form without authentication
- [ ] Form captures visitor name, company name, and planned visit duration
- [ ] System generates visitor record with unique identifier and "Planned" status
- [ ] Visitor receives confirmation of successful registration with access token
- [ ] Registration data is immediately available to authenticated users
- [ ] Form includes proper validation and error handling
- [ ] Page is responsive and works on mobile devices
- [ ] Visitor token is securely generated and unique

## Implementation Tasks

### 1. Visitor Service Interface and Implementation

**File: `Services/Interfaces/IVisitorService.cs`**
```csharp
using VisitorTracking.Data.Entities;
using VisitorTracking.Models.DTOs;

namespace VisitorTracking.Services.Interfaces
{
    public interface IVisitorService
    {
        Task<Visitor> RegisterVisitorAsync(VisitorRegistrationDto registration);
        Task<Visitor?> GetVisitorByTokenAsync(string token);
        Task<Visitor?> GetVisitorByIdAsync(Guid id);
        Task<bool> UpdateVisitorStatusAsync(string token, VisitorStatus newStatus);
        Task<IEnumerable<Visitor>> GetVisitorsByDateAsync(DateTime date);
        Task<IEnumerable<Visitor>> GetTodaysVisitorsAsync();
    }
}
```

**File: `Services/VisitorService.cs`**
```csharp
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using VisitorTracking.Data.Context;
using VisitorTracking.Data.Entities;
using VisitorTracking.Models.DTOs;
using VisitorTracking.Services.Interfaces;

namespace VisitorTracking.Services
{
    public class VisitorService : IVisitorService
    {
        private readonly VisitorTrackingContext _context;
        private readonly ILogger<VisitorService> _logger;

        public VisitorService(
            VisitorTrackingContext context,
            ILogger<VisitorService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Visitor> RegisterVisitorAsync(VisitorRegistrationDto registration)
        {
            try
            {
                var visitor = new Visitor
                {
                    Id = Guid.NewGuid(),
                    Name = registration.Name.Trim(),
                    Company = registration.Company.Trim(),
                    PlannedDuration = registration.PlannedDuration,
                    Status = VisitorStatus.Planned,
                    CreatedAt = DateTime.UtcNow,
                    VisitorToken = GenerateUniqueToken(),
                    CreatedByUserId = registration.CreatedByUserId
                };

                _context.Visitors.Add(visitor);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Visitor registered: {Name} from {Company} - Token: {Token}",
                    visitor.Name,
                    visitor.Company,
                    visitor.VisitorToken);

                return visitor;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering visitor");
                throw;
            }
        }

        public async Task<Visitor?> GetVisitorByTokenAsync(string token)
        {
            return await _context.Visitors
                .Include(v => v.CreatedByUser)
                .FirstOrDefaultAsync(v => v.VisitorToken == token);
        }

        public async Task<Visitor?> GetVisitorByIdAsync(Guid id)
        {
            return await _context.Visitors
                .Include(v => v.CreatedByUser)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<bool> UpdateVisitorStatusAsync(string token, VisitorStatus newStatus)
        {
            var visitor = await GetVisitorByTokenAsync(token);
            if (visitor == null)
                return false;

            // Validate status transition
            if (!IsValidStatusTransition(visitor.Status, newStatus))
            {
                _logger.LogWarning(
                    "Invalid status transition attempted: {CurrentStatus} -> {NewStatus}",
                    visitor.Status,
                    newStatus);
                return false;
            }

            visitor.Status = newStatus;

            if (newStatus == VisitorStatus.Arrived && !visitor.ArrivedAt.HasValue)
            {
                visitor.ArrivedAt = DateTime.UtcNow;
            }
            else if (newStatus == VisitorStatus.Left && !visitor.LeftAt.HasValue)
            {
                visitor.LeftAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Visitor {Name} status updated to {Status}",
                visitor.Name,
                newStatus);

            return true;
        }

        public async Task<IEnumerable<Visitor>> GetVisitorsByDateAsync(DateTime date)
        {
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1);

            return await _context.Visitors
                .Include(v => v.CreatedByUser)
                .Where(v => v.CreatedAt >= startOfDay && v.CreatedAt < endOfDay)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Visitor>> GetTodaysVisitorsAsync()
        {
            return await GetVisitorsByDateAsync(DateTime.UtcNow.Date);
        }

        private bool IsValidStatusTransition(VisitorStatus current, VisitorStatus next)
        {
            return (current, next) switch
            {
                (VisitorStatus.Planned, VisitorStatus.Arrived) => true,
                (VisitorStatus.Arrived, VisitorStatus.Left) => true,
                _ => false
            };
        }

        private string GenerateUniqueToken()
        {
            // Generate a cryptographically secure random token
            using var rng = RandomNumberGenerator.Create();
            var tokenBytes = new byte[16];
            rng.GetBytes(tokenBytes);
            return Convert.ToBase64String(tokenBytes)
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", "")
                .Substring(0, 16)
                .ToUpper();
        }
    }
}
```

Add service to `Program.cs`:
```csharp
builder.Services.AddScoped<IVisitorService, VisitorService>();
```

### 2. Data Transfer Objects (DTOs)

**File: `Models/DTOs/VisitorRegistrationDto.cs`**
```csharp
using System.ComponentModel.DataAnnotations;

namespace VisitorTracking.Models.DTOs
{
    public class VisitorRegistrationDto
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Company name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Company name must be between 2 and 100 characters")]
        public string Company { get; set; } = string.Empty;

        [Required(ErrorMessage = "Planned duration is required")]
        [Range(typeof(TimeSpan), "00:15:00", "12:00:00", ErrorMessage = "Duration must be between 15 minutes and 12 hours")]
        public TimeSpan PlannedDuration { get; set; }

        // Optional: Set by employee when pre-registering
        public string? CreatedByUserId { get; set; }
    }
}
```

**File: `Models/ViewModels/VisitorRegistrationViewModel.cs`**
```csharp
using System.ComponentModel.DataAnnotations;

namespace VisitorTracking.Models.ViewModels
{
    public class VisitorRegistrationViewModel
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Company is required")]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Company Name")]
        public string Company { get; set; } = string.Empty;

        [Required(ErrorMessage = "Duration is required")]
        [Display(Name = "Planned Duration (hours)")]
        [Range(0.25, 12, ErrorMessage = "Duration must be between 15 minutes and 12 hours")]
        public double DurationHours { get; set; } = 1.0;

        public TimeSpan PlannedDuration => TimeSpan.FromHours(DurationHours);
    }
}
```

### 3. Visitor Registration Page

**File: `Components/Pages/Visitor/Register.razor`**
```razor
@page "/visitor/register"
@using VisitorTracking.Models.ViewModels
@using VisitorTracking.Models.DTOs
@using VisitorTracking.Services.Interfaces
@attribute [AllowAnonymous]
@inject IVisitorService VisitorService
@inject NavigationManager Navigation

<PageTitle>Visitor Registration</PageTitle>

<div class="container mt-5">
    <div class="row justify-content-center">
        <div class="col-md-6">
            <div class="card shadow">
                <div class="card-header bg-primary text-white">
                    <h3 class="mb-0">Welcome!</h3>
                    <p class="mb-0 small">Please register your visit</p>
                </div>
                <div class="card-body">
                    @if (!string.IsNullOrEmpty(errorMessage))
                    {
                        <div class="alert alert-danger" role="alert">
                            @errorMessage
                        </div>
                    }

                    <EditForm Model="@model" OnValidSubmit="HandleRegistration">
                        <DataAnnotationsValidator />
                        <ValidationSummary class="text-danger" />

                        <div class="mb-3">
                            <label for="name" class="form-label">Full Name *</label>
                            <InputText id="name" @bind-Value="model.Name" class="form-control" placeholder="John Doe" />
                            <ValidationMessage For="@(() => model.Name)" class="text-danger" />
                        </div>

                        <div class="mb-3">
                            <label for="company" class="form-label">Company Name *</label>
                            <InputText id="company" @bind-Value="model.Company" class="form-control" placeholder="Acme Corporation" />
                            <ValidationMessage For="@(() => model.Company)" class="text-danger" />
                        </div>

                        <div class="mb-3">
                            <label for="duration" class="form-label">Expected Duration (hours) *</label>
                            <InputNumber id="duration" @bind-Value="model.DurationHours" class="form-control" step="0.25" min="0.25" max="12" />
                            <ValidationMessage For="@(() => model.DurationHours)" class="text-danger" />
                            <small class="form-text text-muted">
                                How long do you expect to stay? (0.25 = 15 minutes)
                            </small>
                        </div>

                        <div class="d-grid gap-2">
                            <button type="submit" class="btn btn-primary btn-lg" disabled="@isSubmitting">
                                @if (isSubmitting)
                                {
                                    <span class="spinner-border spinner-border-sm me-2" role="status"></span>
                                    <span>Registering...</span>
                                }
                                else
                                {
                                    <span>Register Visit</span>
                                }
                            </button>
                        </div>
                    </EditForm>

                    <div class="mt-3 text-center">
                        <small class="text-muted">
                            Already registered? <a href="/visitor/checkin">Check in here</a>
                        </small>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

@code {
    private VisitorRegistrationViewModel model = new();
    private bool isSubmitting = false;
    private string? errorMessage;

    private async Task HandleRegistration()
    {
        try
        {
            isSubmitting = true;
            errorMessage = null;

            var registrationDto = new VisitorRegistrationDto
            {
                Name = model.Name,
                Company = model.Company,
                PlannedDuration = model.PlannedDuration
            };

            var visitor = await VisitorService.RegisterVisitorAsync(registrationDto);

            // Navigate to confirmation page with token
            Navigation.NavigateTo($"/visitor/confirmation/{visitor.VisitorToken}");
        }
        catch (Exception ex)
        {
            errorMessage = "An error occurred during registration. Please try again.";
            // Log error
        }
        finally
        {
            isSubmitting = false;
        }
    }
}
```

### 4. Registration Confirmation Page

**File: `Components/Pages/Visitor/Confirmation.razor`**
```razor
@page "/visitor/confirmation/{token}"
@using VisitorTracking.Services.Interfaces
@using VisitorTracking.Data.Entities
@attribute [AllowAnonymous]
@inject IVisitorService VisitorService

<PageTitle>Registration Confirmed</PageTitle>

<div class="container mt-5">
    <div class="row justify-content-center">
        <div class="col-md-6">
            @if (visitor != null)
            {
                <div class="card shadow">
                    <div class="card-header bg-success text-white text-center">
                        <h3 class="mb-0">✓ Registration Successful</h3>
                    </div>
                    <div class="card-body">
                        <div class="text-center mb-4">
                            <div class="alert alert-info">
                                <h4>Your Access Code:</h4>
                                <h2 class="font-monospace">@Token</h2>
                                <p class="mb-0 small">Please save this code to update your status later</p>
                            </div>
                        </div>

                        <dl class="row">
                            <dt class="col-sm-4">Name:</dt>
                            <dd class="col-sm-8">@visitor.Name</dd>

                            <dt class="col-sm-4">Company:</dt>
                            <dd class="col-sm-8">@visitor.Company</dd>

                            <dt class="col-sm-4">Expected Duration:</dt>
                            <dd class="col-sm-8">@FormatDuration(visitor.PlannedDuration)</dd>

                            <dt class="col-sm-4">Status:</dt>
                            <dd class="col-sm-8">
                                <span class="badge bg-warning">@visitor.Status</span>
                            </dd>

                            <dt class="col-sm-4">Registered At:</dt>
                            <dd class="col-sm-8">@visitor.CreatedAt.ToLocalTime().ToString("g")</dd>
                        </dl>

                        <div class="d-grid gap-2 mt-4">
                            <a href="/visitor/status/@Token" class="btn btn-primary">
                                Check In Now
                            </a>
                            <a href="/visitor/register" class="btn btn-outline-secondary">
                                Register Another Visitor
                            </a>
                        </div>

                        <div class="alert alert-warning mt-3">
                            <small>
                                <strong>Important:</strong> Keep your access code safe. You'll need it to check in and check out.
                            </small>
                        </div>
                    </div>
                </div>
            }
            else
            {
                <div class="alert alert-danger">
                    <h4>Invalid Access Code</h4>
                    <p>The registration could not be found. Please try registering again.</p>
                    <a href="/visitor/register" class="btn btn-primary">Back to Registration</a>
                </div>
            }
        </div>
    </div>
</div>

@code {
    [Parameter]
    public string Token { get; set; } = string.Empty;

    private Visitor? visitor;

    protected override async Task OnInitializedAsync()
    {
        if (!string.IsNullOrEmpty(Token))
        {
            visitor = await VisitorService.GetVisitorByTokenAsync(Token);
        }
    }

    private string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return $"{duration.TotalHours:F1} hours";
        return $"{duration.TotalMinutes:F0} minutes";
    }
}
```

### 5. Styling (Optional)

**File: `wwwroot/css/visitor.css`**
```css
/* Visitor Registration Styles */
.visitor-card {
    border-radius: 10px;
    box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
}

.access-code {
    font-size: 2rem;
    letter-spacing: 0.2rem;
    font-weight: bold;
    padding: 1rem;
    background-color: #f8f9fa;
    border-radius: 5px;
}

.status-badge {
    font-size: 1rem;
    padding: 0.5rem 1rem;
}

/* Mobile responsiveness */
@media (max-width: 768px) {
    .container {
        padding: 1rem;
    }

    .access-code {
        font-size: 1.5rem;
    }
}
```

## Testing Requirements

### Unit Tests
**File: `Tests/Services/VisitorServiceTests.cs`**
```csharp
using Xunit;
using VisitorTracking.Services;
using VisitorTracking.Models.DTOs;

namespace VisitorTracking.Tests.Services
{
    public class VisitorServiceTests
    {
        [Fact]
        public async Task RegisterVisitor_ShouldCreateVisitorWithUniqueToken()
        {
            // Arrange
            var service = CreateVisitorService();
            var dto = new VisitorRegistrationDto
            {
                Name = "Test User",
                Company = "Test Company",
                PlannedDuration = TimeSpan.FromHours(2)
            };

            // Act
            var visitor = await service.RegisterVisitorAsync(dto);

            // Assert
            Assert.NotNull(visitor);
            Assert.NotEqual(Guid.Empty, visitor.Id);
            Assert.False(string.IsNullOrEmpty(visitor.VisitorToken));
            Assert.Equal(VisitorStatus.Planned, visitor.Status);
        }

        [Fact]
        public async Task RegisterVisitor_ShouldGenerateUniqueTokens()
        {
            // Test that tokens are unique
        }

        // Additional tests...
    }
}
```

### Integration Tests
- Test full registration flow
- Validate token uniqueness
- Test concurrent registrations

## Definition of Done
- [ ] Visitors can successfully register without authentication
- [ ] Form validation works correctly with appropriate error messages
- [ ] Visitor records are created with proper status and timestamps
- [ ] Registration confirmation displays access code
- [ ] Visitor tokens are unique and cryptographically secure
- [ ] Page is responsive and works on mobile devices
- [ ] All unit tests pass
- [ ] Integration tests verify complete workflow
- [ ] Error handling provides user-friendly messages
- [ ] Logging captures registration events

## Dependencies for Next Tasks
- TASK-008 (Visitor Status Management) builds on this registration functionality
- TASK-009 (Pre-registered Visitor Selection) extends this feature
- Employee and admin dashboards will display registered visitors
