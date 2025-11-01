# TASK-011: Employee Pre-registration System (VT-006)

## Overview
**Priority**: High  
**Dependencies**: TASK-010  
**Estimated Effort**: 2-3 days  
**Phase**: Core Functionality  
**User Story**: VT-006

## Description
Implement employee functionality to pre-register expected visitors with their details.

## User Story
**As an** authenticated employee  
**I want to** pre-register expected visitors with their details  
**So that** they can quickly check in upon arrival

## Acceptance Criteria
- [ ] Employee can create visitor records with name, company, and planned duration
- [ ] Pre-registered visitors have "Planned" status and are associated with the creating employee
- [ ] Employee can view and modify their own pre-registrations
- [ ] Pre-registered visitors appear in the visitor selection list for guest users
- [ ] System prevents duplicate pre-registrations for same visitor and date
- [ ] Include validation for visitor information

## Implementation

### Pre-Registration Form
**File: `Components/Pages/Employee/PreRegisterVisitor.razor`**
```razor
@page "/employee/visitors/preregister"
@using VisitorTracking.Constants
@attribute [Authorize(Policy = AuthorizationPolicies.EmployeePolicy)]
@inject IVisitorService VisitorService
@inject IUserService UserService
@inject AuthenticationStateProvider AuthProvider

<PageTitle>Pre-Register Visitor</PageTitle>

<h3>Pre-Register Visitor</h3>

<div class="row">
    <div class="col-md-6">
        <EditForm Model="@model" OnValidSubmit="HandleSubmit">
            <DataAnnotationsValidator />
            <ValidationSummary />

            <div class="mb-3">
                <label class="form-label">Visitor Name *</label>
                <InputText @bind-Value="model.Name" class="form-control" />
                <ValidationMessage For="@(() => model.Name)" />
            </div>

            <div class="mb-3">
                <label class="form-label">Company *</label>
                <InputText @bind-Value="model.Company" class="form-control" />
                <ValidationMessage For="@(() => model.Company)" />
            </div>

            <div class="mb-3">
                <label class="form-label">Expected Duration (hours) *</label>
                <InputNumber @bind-Value="model.DurationHours" class="form-control" step="0.25" />
                <ValidationMessage For="@(() => model.DurationHours)" />
            </div>

            <button type="submit" class="btn btn-primary" disabled="@isSubmitting">
                @if (isSubmitting)
                {
                    <span class="spinner-border spinner-border-sm me-2"></span>
                }
                Pre-Register Visitor
            </button>
        </EditForm>
    </div>
</div>

@code {
    private VisitorRegistrationViewModel model = new();
    private bool isSubmitting = false;
    private string? userId;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthProvider.GetAuthenticationStateAsync();
        var user = await UserService.GetOrCreateUserAsync(authState.User);
        userId = user.Id;
    }

    private async Task HandleSubmit()
    {
        isSubmitting = true;
        try
        {
            var dto = new VisitorRegistrationDto
            {
                Name = model.Name,
                Company = model.Company,
                PlannedDuration = model.PlannedDuration,
                CreatedByUserId = userId
            };

            await VisitorService.RegisterVisitorAsync(dto);
            // Navigate to success or list
        }
        finally
        {
            isSubmitting = false;
        }
    }
}
```

### My Visitors List
**File: `Components/Pages/Employee/MyVisitors.razor`**
```razor
@page "/employee/visitors"
@using VisitorTracking.Constants
@attribute [Authorize(Policy = AuthorizationPolicies.EmployeePolicy)]
@inject IVisitorService VisitorService

<PageTitle>My Pre-Registered Visitors</PageTitle>

<h3>My Pre-Registered Visitors</h3>

<div class="mb-3">
    <a href="/employee/visitors/preregister" class="btn btn-primary">
        + Pre-Register New Visitor
    </a>
</div>

@if (visitors == null)
{
    <p>Loading...</p>
}
else if (!visitors.Any())
{
    <div class="alert alert-info">No pre-registered visitors found.</div>
}
else
{
    <table class="table">
        <thead>
            <tr>
                <th>Name</th>
                <th>Company</th>
                <th>Status</th>
                <th>Registered</th>
                <th>Actions</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var visitor in visitors)
            {
                <tr>
                    <td>@visitor.Name</td>
                    <td>@visitor.Company</td>
                    <td><span class="badge bg-@GetStatusColor(visitor.Status)">@visitor.Status</span></td>
                    <td>@visitor.CreatedAt.ToLocalTime().ToString("g")</td>
                    <td>
                        <a href="/employee/visitors/@visitor.Id" class="btn btn-sm btn-info">Details</a>
                    </td>
                </tr>
            }
        </tbody>
    </table>
}

@code {
    private IEnumerable<Visitor>? visitors;

    protected override async Task OnInitializedAsync()
    {
        // Load employee's pre-registered visitors
        visitors = await VisitorService.GetTodaysVisitorsAsync();
    }

    private string GetStatusColor(VisitorStatus status) => status switch
    {
        VisitorStatus.Planned => "warning",
        VisitorStatus.Arrived => "success",
        VisitorStatus.Left => "secondary",
        _ => "primary"
    };
}
```

## Definition of Done
- [ ] Employees can pre-register visitors
- [ ] Pre-registered visitors appear in selection lists
- [ ] Employees can view their pre-registrations
- [ ] Duplicate prevention works
- [ ] Validation ensures data quality
- [ ] Tests pass
