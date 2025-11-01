# TASK-008: Visitor Status Management (VT-002, VT-003)

## Overview
**Priority**: High  
**Dependencies**: TASK-007  
**Estimated Effort**: 2-3 days  
**Phase**: Core Functionality Implementation  
**User Stories**: VT-002, VT-003

## Description
Implement visitor check-in and check-out functionality allowing visitors to update their status throughout their visit using their access token.

## User Stories

### VT-002: Visitor Status Check-in
**As a** visitor who has registered  
**I want to** update my status to "Arrived" when I enter the company premises  
**So that** my presence is accurately tracked

### VT-003: Visitor Status Check-out
**As a** departing visitor  
**I want to** update my status to "Left" when leaving the company  
**So that** my visit is properly concluded

## Acceptance Criteria
- [ ] Visitor can update their status from "Planned" to "Arrived"
- [ ] Visitor can update their status from "Arrived" to "Left"
- [ ] Status change is reflected in real-time across the system
- [ ] Only the visitor who created the record can update their own status (via token)
- [ ] System prevents invalid status transitions
- [ ] Timestamps are recorded for each status change
- [ ] Visit duration is calculated and stored when visitor leaves
- [ ] Status changes are logged for audit purposes

## Implementation Tasks

### 1. Visitor Status Update Page

**File: `Components/Pages/Visitor/Status.razor`**
```razor
@page "/visitor/status/{token?}"
@using VisitorTracking.Services.Interfaces
@using VisitorTracking.Data.Entities
@attribute [AllowAnonymous]
@inject IVisitorService VisitorService
@inject NavigationManager Navigation

<PageTitle>Visitor Status</PageTitle>

<div class="container mt-5">
    <div class="row justify-content-center">
        <div class="col-md-6">
            @if (!hasToken)
            {
                <div class="card shadow">
                    <div class="card-header bg-primary text-white">
                        <h3>Enter Your Access Code</h3>
                    </div>
                    <div class="card-body">
                        <EditForm Model="@tokenInput" OnValidSubmit="LoadVisitor">
                            <div class="mb-3">
                                <label class="form-label">Access Code</label>
                                <InputText @bind-Value="tokenInput.Token" class="form-control text-uppercase" 
                                          placeholder="Enter your code" maxlength="16" />
                            </div>
                            <button type="submit" class="btn btn-primary w-100">Continue</button>
                        </EditForm>
                    </div>
                </div>
            }
            else if (visitor != null)
            {
                <div class="card shadow">
                    <div class="card-header @GetHeaderClass() text-white">
                        <h3>@visitor.Name</h3>
                        <p class="mb-0">@visitor.Company</p>
                    </div>
                    <div class="card-body">
                        @if (!string.IsNullOrEmpty(successMessage))
                        {
                            <div class="alert alert-success">@successMessage</div>
                        }
                        @if (!string.IsNullOrEmpty(errorMessage))
                        {
                            <div class="alert alert-danger">@errorMessage</div>
                        }

                        <div class="mb-4">
                            <h5>Current Status</h5>
                            <span class="badge @GetStatusBadgeClass() fs-5">@visitor.Status</span>
                        </div>

                        <dl class="row">
                            <dt class="col-6">Registered:</dt>
                            <dd class="col-6">@visitor.CreatedAt.ToLocalTime().ToString("g")</dd>

                            @if (visitor.ArrivedAt.HasValue)
                            {
                                <dt class="col-6">Arrived:</dt>
                                <dd class="col-6">@visitor.ArrivedAt.Value.ToLocalTime().ToString("g")</dd>
                            }

                            @if (visitor.LeftAt.HasValue)
                            {
                                <dt class="col-6">Left:</dt>
                                <dd class="col-6">@visitor.LeftAt.Value.ToLocalTime().ToString("g")</dd>

                                <dt class="col-6">Total Duration:</dt>
                                <dd class="col-6">@CalculateActualDuration()</dd>
                            }

                            <dt class="col-6">Expected Duration:</dt>
                            <dd class="col-6">@FormatDuration(visitor.PlannedDuration)</dd>
                        </dl>

                        <div class="d-grid gap-2 mt-4">
                            @if (visitor.Status == VisitorStatus.Planned)
                            {
                                <button class="btn btn-success btn-lg" @onclick="CheckIn" disabled="@isProcessing">
                                    @if (isProcessing)
                                    {
                                        <span class="spinner-border spinner-border-sm me-2"></span>
                                    }
                                    Check In (Arrived)
                                </button>
                            }
                            else if (visitor.Status == VisitorStatus.Arrived)
                            {
                                <button class="btn btn-danger btn-lg" @onclick="CheckOut" disabled="@isProcessing">
                                    @if (isProcessing)
                                    {
                                        <span class="spinner-border spinner-border-sm me-2"></span>
                                    }
                                    Check Out (Leaving)
                                </button>
                            }
                            else if (visitor.Status == VisitorStatus.Left)
                            {
                                <div class="alert alert-info">
                                    <strong>Visit Complete</strong><br/>
                                    Thank you for visiting!
                                </div>
                            }
                        </div>
                    </div>
                </div>
            }
            else
            {
                <div class="alert alert-danger">
                    <h4>Invalid Access Code</h4>
                    <p>No visitor found with this code.</p>
                    <a href="/visitor/register" class="btn btn-primary">Register New Visit</a>
                </div>
            }
        </div>
    </div>
</div>

@code {
    [Parameter]
    public string? Token { get; set; }

    private class TokenInput
    {
        public string Token { get; set; } = string.Empty;
    }

    private TokenInput tokenInput = new();
    private Visitor? visitor;
    private bool hasToken => !string.IsNullOrEmpty(Token);
    private bool isProcessing = false;
    private string? successMessage;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        if (hasToken)
        {
            await LoadVisitor();
        }
    }

    private async Task LoadVisitor()
    {
        var token = Token ?? tokenInput.Token;
        if (!string.IsNullOrEmpty(token))
        {
            visitor = await VisitorService.GetVisitorByTokenAsync(token.ToUpper());
            if (visitor != null && string.IsNullOrEmpty(Token))
            {
                Navigation.NavigateTo($"/visitor/status/{token.ToUpper()}");
            }
        }
    }

    private async Task CheckIn()
    {
        await UpdateStatus(VisitorStatus.Arrived, "Successfully checked in!");
    }

    private async Task CheckOut()
    {
        await UpdateStatus(VisitorStatus.Left, "Thank you for visiting! Your visit is now complete.");
    }

    private async Task UpdateStatus(VisitorStatus newStatus, string message)
    {
        try
        {
            isProcessing = true;
            errorMessage = null;
            successMessage = null;

            var success = await VisitorService.UpdateVisitorStatusAsync(Token!, newStatus);

            if (success)
            {
                successMessage = message;
                await LoadVisitor(); // Refresh visitor data
            }
            else
            {
                errorMessage = "Unable to update status. Please try again.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = "An error occurred. Please try again.";
        }
        finally
        {
            isProcessing = false;
        }
    }

    private string GetHeaderClass()
    {
        return visitor?.Status switch
        {
            VisitorStatus.Planned => "bg-warning",
            VisitorStatus.Arrived => "bg-success",
            VisitorStatus.Left => "bg-secondary",
            _ => "bg-primary"
        };
    }

    private string GetStatusBadgeClass()
    {
        return visitor?.Status switch
        {
            VisitorStatus.Planned => "bg-warning text-dark",
            VisitorStatus.Arrived => "bg-success",
            VisitorStatus.Left => "bg-secondary",
            _ => "bg-primary"
        };
    }

    private string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return $"{duration.TotalHours:F1} hours";
        return $"{duration.TotalMinutes:F0} minutes";
    }

    private string CalculateActualDuration()
    {
        if (visitor?.ArrivedAt.HasValue == true && visitor?.LeftAt.HasValue == true)
        {
            var duration = visitor.LeftAt.Value - visitor.ArrivedAt.Value;
            return FormatDuration(duration);
        }
        return "N/A";
    }
}
```

### 2. Check-in Quick Access Page

**File: `Components/Pages/Visitor/CheckIn.razor`**
```razor
@page "/visitor/checkin"
@attribute [AllowAnonymous]
@inject NavigationManager Navigation

<PageTitle>Quick Check-In</PageTitle>

<div class="container mt-5">
    <div class="row justify-content-center">
        <div class="col-md-6">
            <div class="card shadow">
                <div class="card-header bg-primary text-white">
                    <h3>Quick Check-In</h3>
                    <p class="mb-0 small">Enter your access code to check in or check out</p>
                </div>
                <div class="card-body">
                    <EditForm Model="@model" OnValidSubmit="HandleSubmit">
                        <div class="mb-3">
                            <label class="form-label">Access Code</label>
                            <InputText @bind-Value="model.Token" class="form-control form-control-lg text-center text-uppercase" 
                                      placeholder="XXXXXXXXXXXX" maxlength="16" style="letter-spacing: 0.2em;" />
                            <small class="form-text text-muted">
                                Enter the code you received when you registered
                            </small>
                        </div>
                        <div class="d-grid">
                            <button type="submit" class="btn btn-primary btn-lg">Continue</button>
                        </div>
                    </EditForm>

                    <div class="text-center mt-4">
                        <hr />
                        <p class="text-muted">Don't have a code?</p>
                        <a href="/visitor/register" class="btn btn-outline-primary">Register as New Visitor</a>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

@code {
    private class TokenModel
    {
        public string Token { get; set; } = string.Empty;
    }

    private TokenModel model = new();

    private void HandleSubmit()
    {
        if (!string.IsNullOrWhiteSpace(model.Token))
        {
            Navigation.NavigateTo($"/visitor/status/{model.Token.ToUpper().Trim()}");
        }
    }
}
```

### 3. Status Validation Extension

**File: `Services/Extensions/VisitorStatusExtensions.cs`**
```csharp
using VisitorTracking.Data.Entities;

namespace VisitorTracking.Services.Extensions
{
    public static class VisitorStatusExtensions
    {
        public static bool CanTransitionTo(this VisitorStatus current, VisitorStatus target)
        {
            return (current, target) switch
            {
                (VisitorStatus.Planned, VisitorStatus.Arrived) => true,
                (VisitorStatus.Arrived, VisitorStatus.Left) => true,
                _ => false
            };
        }

        public static string GetDisplayName(this VisitorStatus status)
        {
            return status switch
            {
                VisitorStatus.Planned => "Registered",
                VisitorStatus.Arrived => "On Premises",
                VisitorStatus.Left => "Visit Complete",
                _ => status.ToString()
            };
        }

        public static string GetDescription(this VisitorStatus status)
        {
            return status switch
            {
                VisitorStatus.Planned => "Visitor has registered but not yet arrived",
                VisitorStatus.Arrived => "Visitor is currently on company premises",
                VisitorStatus.Left => "Visitor has completed their visit",
                _ => string.Empty
            };
        }
    }
}
```

## Testing Requirements

### Unit Tests
```csharp
[Fact]
public async Task UpdateStatus_PlannedToArrived_ShouldSucceed()
{
    // Test valid status transition
}

[Fact]
public async Task UpdateStatus_PlannedToLeft_ShouldFail()
{
    // Test invalid status transition
}

[Fact]
public async Task UpdateStatus_ShouldRecordTimestamp()
{
    // Verify timestamp is recorded
}

[Fact]
public async Task CheckOut_ShouldCalculateDuration()
{
    // Verify duration calculation
}
```

### Integration Tests
- Test complete check-in/check-out workflow
- Verify status cannot skip states
- Test concurrent status updates
- Validate timestamp accuracy

## Definition of Done
- [ ] Visitors can successfully check in (Planned → Arrived)
- [ ] Visitors can successfully check out (Arrived → Left)
- [ ] Invalid status transitions are prevented
- [ ] Timestamps are accurately recorded
- [ ] Visit duration is calculated correctly
- [ ] Real-time status updates work
- [ ] Token-based access control is secure
- [ ] Mobile-responsive UI works correctly
- [ ] All unit and integration tests pass
- [ ] Error handling provides clear feedback

## Dependencies for Next Tasks
- TASK-012 (Employee Dashboard) will display these status updates in real-time
- TASK-013 (Admin Management) will use status data for reporting
