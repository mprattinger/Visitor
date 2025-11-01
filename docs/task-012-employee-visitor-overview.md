# TASK-012: Employee Visitor Overview Dashboard (VT-007)

## Overview
**Priority**: High  
**Dependencies**: TASK-011  
**Estimated Effort**: 2-3 days  
**Phase**: Core Functionality  
**User Story**: VT-007

## Description
Implement an employee dashboard that shows all current visitors (checked in and not yet departed) with real-time updates.

## User Story
**As an** authenticated employee  
**I want to** view all current visitors (checked in and not yet departed)  
**So that** I can see who is currently in the building

## Acceptance Criteria
- [ ] Dashboard displays all visitors with "Arrived" status
- [ ] Shows visitor name, company, check-in time, and planned duration
- [ ] Updates in real-time when visitor status changes
- [ ] Provides filtering by name or company
- [ ] Shows total count of current visitors
- [ ] Displays visual indicator for visitors exceeding planned duration

## Implementation

### Visitor Overview Dashboard
**File: `Components/Pages/Employee/VisitorOverview.razor`**
```razor
@page "/employee/visitors/current"
@using VisitorTracking.Constants
@attribute [Authorize(Policy = AuthorizationPolicies.EmployeePolicy)]
@inject IVisitorService VisitorService
@implements IDisposable

<PageTitle>Current Visitors</PageTitle>

<h3>Current Visitors</h3>

<div class="row mb-3">
    <div class="col-md-4">
        <div class="card text-white bg-primary">
            <div class="card-body">
                <h5 class="card-title">Current Visitors</h5>
                <p class="card-text display-4">@currentVisitors.Count()</p>
            </div>
        </div>
    </div>
</div>

<div class="row mb-3">
    <div class="col-md-6">
        <input type="text" class="form-control" placeholder="Search by name or company..." 
               @bind="searchTerm" @bind:event="oninput" />
    </div>
</div>

@if (isLoading)
{
    <p>Loading current visitors...</p>
}
else if (!filteredVisitors.Any())
{
    <div class="alert alert-info">No visitors currently checked in.</div>
}
else
{
    <table class="table table-hover">
        <thead>
            <tr>
                <th>Name</th>
                <th>Company</th>
                <th>Check-in Time</th>
                <th>Duration</th>
                <th>Status</th>
                <th>Actions</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var visitor in filteredVisitors)
            {
                <tr class="@GetRowClass(visitor)">
                    <td>@visitor.Name</td>
                    <td>@visitor.Company</td>
                    <td>@visitor.CheckInTime?.ToLocalTime().ToString("HH:mm")</td>
                    <td>
                        @GetDurationDisplay(visitor)
                        @if (IsOverDuration(visitor))
                        {
                            <span class="badge bg-warning ms-2">Exceeded</span>
                        }
                    </td>
                    <td><span class="badge bg-success">Arrived</span></td>
                    <td>
                        <button class="btn btn-sm btn-primary" 
                                @onclick="() => ViewDetails(visitor.Id)">
                            Details
                        </button>
                    </td>
                </tr>
            }
        </tbody>
    </table>
}

<p class="text-muted">Last updated: @lastUpdateTime.ToLocalTime().ToString("HH:mm:ss")</p>

@code {
    private List<Visitor> currentVisitors = new();
    private IEnumerable<Visitor> filteredVisitors => currentVisitors
        .Where(v => string.IsNullOrEmpty(searchTerm) ||
                    v.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    v.Company.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
    
    private string searchTerm = "";
    private bool isLoading = true;
    private DateTime lastUpdateTime = DateTime.UtcNow;
    private System.Threading.Timer? refreshTimer;

    protected override async Task OnInitializedAsync()
    {
        await LoadCurrentVisitors();
        
        // Setup auto-refresh every 30 seconds
        refreshTimer = new System.Threading.Timer(async _ =>
        {
            await InvokeAsync(async () =>
            {
                await LoadCurrentVisitors();
                StateHasChanged();
            });
        }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    private async Task LoadCurrentVisitors()
    {
        isLoading = true;
        currentVisitors = (await VisitorService.GetCurrentVisitorsAsync()).ToList();
        lastUpdateTime = DateTime.UtcNow;
        isLoading = false;
    }

    private string GetDurationDisplay(Visitor visitor)
    {
        if (visitor.CheckInTime == null) return "-";
        
        var elapsed = DateTime.UtcNow - visitor.CheckInTime.Value;
        return $"{elapsed.Hours}h {elapsed.Minutes}m / {visitor.PlannedDuration.TotalHours:F1}h";
    }

    private bool IsOverDuration(Visitor visitor)
    {
        if (visitor.CheckInTime == null) return false;
        var elapsed = DateTime.UtcNow - visitor.CheckInTime.Value;
        return elapsed > visitor.PlannedDuration;
    }

    private string GetRowClass(Visitor visitor)
    {
        return IsOverDuration(visitor) ? "table-warning" : "";
    }

    private void ViewDetails(string visitorId)
    {
        // Navigate to detail page
    }

    public void Dispose()
    {
        refreshTimer?.Dispose();
    }
}
```

### Visitor Service - Current Visitors
**File: `Services/VisitorService.cs` (Add method)**
```csharp
public async Task<IEnumerable<Visitor>> GetCurrentVisitorsAsync()
{
    return await _context.Visitors
        .Where(v => v.Status == VisitorStatus.Arrived)
        .OrderBy(v => v.CheckInTime)
        .ToListAsync();
}
```

### Real-Time Updates with SignalR (Optional Enhancement)
**File: `Hubs/VisitorHub.cs`**
```csharp
using Microsoft.AspNetCore.SignalR;

namespace VisitorTracking.Hubs;

public class VisitorHub : Hub
{
    public async Task SendVisitorUpdate(string message)
    {
        await Clients.All.SendAsync("VisitorStatusChanged", message);
    }
}
```

**File: `Program.cs` (Add SignalR)**
```csharp
builder.Services.AddSignalR();

// After MapRazorComponents
app.MapHub<VisitorHub>("/visitorhub");
```

**File: `VisitorOverview.razor` (Add SignalR client)**
```razor
@inject IJSRuntime JS

@code {
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JS.InvokeVoidAsync("setupVisitorConnection", 
                DotNetObjectReference.Create(this));
        }
    }

    [JSInvokable]
    public async Task OnVisitorStatusChanged()
    {
        await LoadCurrentVisitors();
        StateHasChanged();
    }
}
```

**File: `wwwroot/js/visitor-signalr.js`**
```javascript
window.setupVisitorConnection = (dotnetHelper) => {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/visitorhub")
        .build();

    connection.on("VisitorStatusChanged", () => {
        dotnetHelper.invokeMethodAsync("OnVisitorStatusChanged");
    });

    connection.start();
};
```

## Testing Requirements

### Unit Tests
**File: `Tests/Services/VisitorServiceTests.cs`**
```csharp
[Fact]
public async Task GetCurrentVisitorsAsync_ReturnsOnlyArrivedVisitors()
{
    // Arrange
    var context = GetInMemoryContext();
    context.Visitors.AddRange(
        new Visitor { Id = "1", Status = VisitorStatus.Arrived },
        new Visitor { Id = "2", Status = VisitorStatus.Planned },
        new Visitor { Id = "3", Status = VisitorStatus.Arrived },
        new Visitor { Id = "4", Status = VisitorStatus.Left }
    );
    await context.SaveChangesAsync();
    var service = new VisitorService(context);

    // Act
    var result = await service.GetCurrentVisitorsAsync();

    // Assert
    Assert.Equal(2, result.Count());
    Assert.All(result, v => Assert.Equal(VisitorStatus.Arrived, v.Status));
}
```

## Definition of Done
- [ ] Dashboard displays all checked-in visitors
- [ ] Real-time updates working (or periodic refresh)
- [ ] Search/filter functionality works
- [ ] Visual indicators for exceeded duration
- [ ] Visitor count display accurate
- [ ] All tests pass
- [ ] Performance tested with 50+ concurrent visitors
