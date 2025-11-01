# TASK-013: Admin Visitor Management Interface (VT-008)

## Overview
**Priority**: High  
**Dependencies**: TASK-012  
**Estimated Effort**: 2-3 days  
**Phase**: Admin Features  
**User Story**: VT-008

## Description
Implement comprehensive admin interface for managing all visitor records including create, read, update, and delete operations.

## User Story
**As an** admin  
**I want to** view, edit, and delete any visitor record  
**So that** I can manage incorrect data and maintain system integrity

## Acceptance Criteria
- [ ] Admin can view all visitor records (past and present)
- [ ] Admin can edit any visitor information
- [ ] Admin can delete visitor records
- [ ] Deletion requires confirmation
- [ ] Audit trail for all admin actions
- [ ] Pagination and filtering for large datasets
- [ ] Export visitor data to CSV

## Implementation

### Admin Visitor Management Page
**File: `Components/Pages/Admin/VisitorManagement.razor`**
```razor
@page "/admin/visitors"
@using VisitorTracking.Constants
@attribute [Authorize(Policy = AuthorizationPolicies.AdminPolicy)]
@inject IVisitorService VisitorService
@inject IAuditService AuditService
@inject NavigationManager Navigation

<PageTitle>Visitor Management</PageTitle>

<h3>Visitor Management</h3>

<div class="row mb-3">
    <div class="col-md-4">
        <input type="text" class="form-control" placeholder="Search..." 
               @bind="searchTerm" @bind:event="oninput" />
    </div>
    <div class="col-md-3">
        <select class="form-select" @bind="filterStatus">
            <option value="">All Statuses</option>
            <option value="Planned">Planned</option>
            <option value="Arrived">Arrived</option>
            <option value="Left">Left</option>
        </select>
    </div>
    <div class="col-md-3">
        <input type="date" class="form-control" @bind="filterDate" />
    </div>
    <div class="col-md-2">
        <button class="btn btn-success w-100" @onclick="ExportToCsv">
            Export CSV
        </button>
    </div>
</div>

@if (isLoading)
{
    <p>Loading visitors...</p>
}
else
{
    <table class="table table-striped">
        <thead>
            <tr>
                <th>Name</th>
                <th>Company</th>
                <th>Status</th>
                <th>Check-in</th>
                <th>Check-out</th>
                <th>Registered By</th>
                <th>Actions</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var visitor in paginatedVisitors)
            {
                <tr>
                    <td>@visitor.Name</td>
                    <td>@visitor.Company</td>
                    <td><span class="badge bg-@GetStatusColor(visitor.Status)">@visitor.Status</span></td>
                    <td>@(visitor.CheckInTime?.ToLocalTime().ToString("g") ?? "-")</td>
                    <td>@(visitor.CheckOutTime?.ToLocalTime().ToString("g") ?? "-")</td>
                    <td>@(visitor.CreatedByUser?.DisplayName ?? "Self-registered")</td>
                    <td>
                        <button class="btn btn-sm btn-primary" 
                                @onclick="() => EditVisitor(visitor.Id)">
                            Edit
                        </button>
                        <button class="btn btn-sm btn-danger" 
                                @onclick="() => ConfirmDelete(visitor)">
                            Delete
                        </button>
                    </td>
                </tr>
            }
        </tbody>
    </table>

    <nav>
        <ul class="pagination">
            <li class="page-item @(currentPage == 1 ? "disabled" : "")">
                <button class="page-link" @onclick="() => ChangePage(currentPage - 1)">Previous</button>
            </li>
            @for (int i = 1; i <= totalPages; i++)
            {
                var page = i;
                <li class="page-item @(currentPage == page ? "active" : "")">
                    <button class="page-link" @onclick="() => ChangePage(page)">@page</button>
                </li>
            }
            <li class="page-item @(currentPage == totalPages ? "disabled" : "")">
                <button class="page-link" @onclick="() => ChangePage(currentPage + 1)">Next</button>
            </li>
        </ul>
    </nav>
}

@if (showDeleteConfirm)
{
    <div class="modal show d-block" tabindex="-1">
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Confirm Delete</h5>
                </div>
                <div class="modal-body">
                    <p>Are you sure you want to delete visitor <strong>@visitorToDelete?.Name</strong>?</p>
                    <p class="text-danger">This action cannot be undone.</p>
                </div>
                <div class="modal-footer">
                    <button class="btn btn-secondary" @onclick="CancelDelete">Cancel</button>
                    <button class="btn btn-danger" @onclick="DeleteVisitor">Delete</button>
                </div>
            </div>
        </div>
    </div>
    <div class="modal-backdrop show"></div>
}

@code {
    private List<Visitor> allVisitors = new();
    private IEnumerable<Visitor> filteredVisitors => allVisitors
        .Where(v => (string.IsNullOrEmpty(searchTerm) ||
                     v.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                     v.Company.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrEmpty(filterStatus) || v.Status.ToString() == filterStatus) &&
                    (!filterDate.HasValue || v.CreatedAt.Date == filterDate.Value.Date));
    
    private IEnumerable<Visitor> paginatedVisitors => filteredVisitors
        .Skip((currentPage - 1) * pageSize)
        .Take(pageSize);

    private string searchTerm = "";
    private string filterStatus = "";
    private DateTime? filterDate;
    private bool isLoading = true;
    private int currentPage = 1;
    private int pageSize = 20;
    private int totalPages => (int)Math.Ceiling(filteredVisitors.Count() / (double)pageSize);

    private bool showDeleteConfirm = false;
    private Visitor? visitorToDelete;

    protected override async Task OnInitializedAsync()
    {
        await LoadVisitors();
    }

    private async Task LoadVisitors()
    {
        isLoading = true;
        allVisitors = (await VisitorService.GetAllVisitorsAsync()).ToList();
        isLoading = false;
    }

    private void ChangePage(int page)
    {
        if (page >= 1 && page <= totalPages)
        {
            currentPage = page;
        }
    }

    private void EditVisitor(string visitorId)
    {
        Navigation.NavigateTo($"/admin/visitors/{visitorId}/edit");
    }

    private void ConfirmDelete(Visitor visitor)
    {
        visitorToDelete = visitor;
        showDeleteConfirm = true;
    }

    private void CancelDelete()
    {
        visitorToDelete = null;
        showDeleteConfirm = false;
    }

    private async Task DeleteVisitor()
    {
        if (visitorToDelete != null)
        {
            await VisitorService.DeleteVisitorAsync(visitorToDelete.Id);
            await AuditService.LogActionAsync(
                "DELETE_VISITOR",
                $"Deleted visitor: {visitorToDelete.Name}",
                visitorToDelete.Id
            );
            
            allVisitors.Remove(visitorToDelete);
            showDeleteConfirm = false;
            visitorToDelete = null;
        }
    }

    private async Task ExportToCsv()
    {
        var csv = VisitorService.ExportToCsv(filteredVisitors);
        // Trigger download
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

### Edit Visitor Page
**File: `Components/Pages/Admin/EditVisitor.razor`**
```razor
@page "/admin/visitors/{VisitorId}/edit"
@using VisitorTracking.Constants
@attribute [Authorize(Policy = AuthorizationPolicies.AdminPolicy)]
@inject IVisitorService VisitorService
@inject IAuditService AuditService
@inject NavigationManager Navigation

<PageTitle>Edit Visitor</PageTitle>

<h3>Edit Visitor</h3>

@if (visitor == null)
{
    <p>Loading...</p>
}
else
{
    <div class="row">
        <div class="col-md-6">
            <EditForm Model="@visitor" OnValidSubmit="HandleSubmit">
                <DataAnnotationsValidator />
                <ValidationSummary />

                <div class="mb-3">
                    <label class="form-label">Name</label>
                    <InputText @bind-Value="visitor.Name" class="form-control" />
                </div>

                <div class="mb-3">
                    <label class="form-label">Company</label>
                    <InputText @bind-Value="visitor.Company" class="form-control" />
                </div>

                <div class="mb-3">
                    <label class="form-label">Status</label>
                    <InputSelect @bind-Value="visitor.Status" class="form-select">
                        <option value="@VisitorStatus.Planned">Planned</option>
                        <option value="@VisitorStatus.Arrived">Arrived</option>
                        <option value="@VisitorStatus.Left">Left</option>
                    </InputSelect>
                </div>

                <div class="mb-3">
                    <label class="form-label">Check-in Time</label>
                    <InputDate Type="InputDateType.DateTimeLocal" 
                               @bind-Value="visitor.CheckInTime" 
                               class="form-control" />
                </div>

                <div class="mb-3">
                    <label class="form-label">Check-out Time</label>
                    <InputDate Type="InputDateType.DateTimeLocal" 
                               @bind-Value="visitor.CheckOutTime" 
                               class="form-control" />
                </div>

                <button type="submit" class="btn btn-primary">Save Changes</button>
                <button type="button" class="btn btn-secondary" 
                        @onclick="Cancel">Cancel</button>
            </EditForm>
        </div>
    </div>
}

@code {
    [Parameter]
    public string VisitorId { get; set; } = "";

    private Visitor? visitor;

    protected override async Task OnInitializedAsync()
    {
        visitor = await VisitorService.GetVisitorByIdAsync(VisitorId);
    }

    private async Task HandleSubmit()
    {
        if (visitor != null)
        {
            await VisitorService.UpdateVisitorAsync(visitor);
            await AuditService.LogActionAsync(
                "UPDATE_VISITOR",
                $"Updated visitor: {visitor.Name}",
                visitor.Id
            );
            Navigation.NavigateTo("/admin/visitors");
        }
    }

    private void Cancel()
    {
        Navigation.NavigateTo("/admin/visitors");
    }
}
```

### Visitor Service - Admin Methods
**File: `Services/VisitorService.cs` (Add methods)**
```csharp
public async Task<IEnumerable<Visitor>> GetAllVisitorsAsync()
{
    return await _context.Visitors
        .Include(v => v.CreatedByUser)
        .OrderByDescending(v => v.CreatedAt)
        .ToListAsync();
}

public async Task<Visitor?> GetVisitorByIdAsync(string id)
{
    return await _context.Visitors
        .Include(v => v.CreatedByUser)
        .FirstOrDefaultAsync(v => v.Id == id);
}

public async Task UpdateVisitorAsync(Visitor visitor)
{
    _context.Visitors.Update(visitor);
    await _context.SaveChangesAsync();
}

public async Task DeleteVisitorAsync(string id)
{
    var visitor = await _context.Visitors.FindAsync(id);
    if (visitor != null)
    {
        _context.Visitors.Remove(visitor);
        await _context.SaveChangesAsync();
    }
}

public string ExportToCsv(IEnumerable<Visitor> visitors)
{
    var csv = new StringBuilder();
    csv.AppendLine("Name,Company,Status,CheckInTime,CheckOutTime,PlannedDuration");
    
    foreach (var visitor in visitors)
    {
        csv.AppendLine($"{visitor.Name},{visitor.Company},{visitor.Status}," +
                      $"{visitor.CheckInTime},{visitor.CheckOutTime}," +
                      $"{visitor.PlannedDuration.TotalHours}");
    }
    
    return csv.ToString();
}
```

### Audit Service
**File: `Services/IAuditService.cs`**
```csharp
public interface IAuditService
{
    Task LogActionAsync(string action, string details, string? entityId = null);
    Task<IEnumerable<AuditLog>> GetAuditLogsAsync(DateTime? from = null, DateTime? to = null);
}
```

**File: `Services/AuditService.cs`**
```csharp
public class AuditService : IAuditService
{
    private readonly VisitorTrackingContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(
        VisitorTrackingContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogActionAsync(string action, string details, string? entityId = null)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
        var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid().ToString(),
            Action = action,
            Details = details,
            EntityId = entityId,
            UserId = userId,
            UserName = userName,
            Timestamp = DateTime.UtcNow,
            IpAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString()
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(
        DateTime? from = null, 
        DateTime? to = null)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (from.HasValue)
            query = query.Where(a => a.Timestamp >= from.Value);
        
        if (to.HasValue)
            query = query.Where(a => a.Timestamp <= to.Value);

        return await query
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    }
}
```

### Audit Log Entity
**File: `Models/AuditLog.cs`**
```csharp
public class AuditLog
{
    public string Id { get; set; } = "";
    public string Action { get; set; } = "";
    public string Details { get; set; } = "";
    public string? EntityId { get; set; }
    public string? UserId { get; set; }
    public string UserName { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
}
```

## Testing Requirements

### Unit Tests
```csharp
[Fact]
public async Task DeleteVisitorAsync_RemovesVisitor()
{
    // Arrange
    var context = GetInMemoryContext();
    var visitor = new Visitor { Id = "1", Name = "Test" };
    context.Visitors.Add(visitor);
    await context.SaveChangesAsync();
    var service = new VisitorService(context);

    // Act
    await service.DeleteVisitorAsync("1");

    // Assert
    Assert.Null(await context.Visitors.FindAsync("1"));
}

[Fact]
public async Task AuditService_LogsAdminAction()
{
    // Test audit logging
}
```

## Definition of Done
- [ ] Admin can view all visitors
- [ ] Admin can edit visitor records
- [ ] Admin can delete visitors with confirmation
- [ ] Audit trail captures all admin actions
- [ ] Pagination works correctly
- [ ] CSV export functionality works
- [ ] All tests pass
