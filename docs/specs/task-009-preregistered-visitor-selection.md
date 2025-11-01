# TASK-009: Pre-registered Visitor Selection (VT-004)

## Overview
**Priority**: High  
**Dependencies**: TASK-008, TASK-011  
**Estimated Effort**: 1-2 days  
**Phase**: Core Functionality Implementation  
**User Story**: VT-004

## Description
Implement functionality for visitors to select their name from pre-registered visitor list instead of filling out the registration form for quick check-in.

## User Story
**As a** visitor whose visit was pre-registered by an employee  
**I want to** select my name from a list instead of filling out the registration form  
**So that** I can check in quickly

## Acceptance Criteria
- [ ] System displays list of pre-registered visitors for current date on registration page
- [ ] Visitor can select their name from the list
- [ ] Selection automatically updates status to "Arrived" with timestamp
- [ ] Pre-registered visitor information (company, duration) is preserved
- [ ] System prevents duplicate selections of the same visitor record
- [ ] Provide fallback to manual registration if name not found
- [ ] Show confirmation after successful selection

## Implementation

### Visitor Selection Component
**File: `Components/Pages/Visitor/QuickCheckIn.razor`**
```razor
@page "/visitor/quickcheckin"
@using VisitorTracking.Services.Interfaces
@attribute [AllowAnonymous]
@inject IVisitorService VisitorService
@inject NavigationManager Navigation

<PageTitle>Quick Check-In</PageTitle>

<div class="container mt-5">
    <div class="row justify-content-center">
        <div class="col-md-8">
            <div class="card shadow">
                <div class="card-header bg-success text-white">
                    <h3>Quick Check-In</h3>
                    <p class="mb-0">Select your name if you were pre-registered</p>
                </div>
                <div class="card-body">
                    @if (isLoading)
                    {
                        <div class="text-center p-5">
                            <div class="spinner-border" role="status"></div>
                            <p class="mt-2">Loading visitors...</p>
                        </div>
                    }
                    else if (plannedVisitors.Any())
                    {
                        <div class="mb-3">
                            <input type="text" class="form-control" @bind="searchTerm" @bind:event="oninput"
                                   placeholder="Search by name or company..." />
                        </div>

                        <div class="list-group">
                            @foreach (var visitor in FilteredVisitors)
                            {
                                <button type="button" class="list-group-item list-group-item-action"
                                        @onclick="() => SelectVisitor(visitor)">
                                    <div class="d-flex w-100 justify-content-between">
                                        <h5 class="mb-1">@visitor.Name</h5>
                                        <small class="text-muted">@visitor.Company</small>
                                    </div>
                                    <small>Expected duration: @FormatDuration(visitor.PlannedDuration)</small>
                                </button>
                            }
                        </div>
                    }
                    else
                    {
                        <div class="alert alert-info">
                            <p>No pre-registered visitors found for today.</p>
                        </div>
                    }

                    <div class="mt-3 text-center">
                        <hr />
                        <p>Don't see your name?</p>
                        <a href="/visitor/register" class="btn btn-primary">Register as New Visitor</a>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

@code {
    private List<Visitor> plannedVisitors = new();
    private string searchTerm = string.Empty;
    private bool isLoading = true;

    private IEnumerable<Visitor> FilteredVisitors =>
        plannedVisitors.Where(v =>
            string.IsNullOrWhiteSpace(searchTerm) ||
            v.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            v.Company.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var allVisitors = await VisitorService.GetTodaysVisitorsAsync();
            plannedVisitors = allVisitors
                .Where(v => v.Status == VisitorStatus.Planned)
                .OrderBy(v => v.Name)
                .ToList();
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task SelectVisitor(Visitor visitor)
    {
        await VisitorService.UpdateVisitorStatusAsync(visitor.VisitorToken, VisitorStatus.Arrived);
        Navigation.NavigateTo($"/visitor/status/{visitor.VisitorToken}");
    }

    private string FormatDuration(TimeSpan duration) =>
        duration.TotalHours >= 1 ? $"{duration.TotalHours:F1} hours" : $"{duration.TotalMinutes:F0} minutes";
}
```

## Testing
- Test visitor selection updates status correctly
- Verify duplicate selection prevention
- Test search/filter functionality
- Validate navigation after selection

## Definition of Done
- [ ] Pre-registered visitors can be selected from list
- [ ] Status updates to "Arrived" automatically
- [ ] Search/filter works correctly
- [ ] Prevents duplicate check-ins
- [ ] Mobile-responsive design
- [ ] All tests pass
