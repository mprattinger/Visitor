# TASK-014: Admin Dashboard and Analytics (VT-009)

## Overview
**Priority**: Medium  
**Dependencies**: TASK-013  
**Estimated Effort**: 2-3 days  
**Phase**: Admin Features  
**User Story**: VT-009

## Description
Implement comprehensive admin dashboard with statistics, analytics, and reporting capabilities.

## User Story
**As an** admin  
**I want to** view statistics about visitor activity (total visitors, average duration, peak times)  
**So that** I can analyze building usage patterns

## Acceptance Criteria
- [ ] Dashboard displays key metrics (total visitors, current visitors, average duration)
- [ ] Shows visitor trends over time (daily, weekly, monthly)
- [ ] Displays peak hours and busiest days
- [ ] Provides breakdown by company/organization
- [ ] Includes data export functionality
- [ ] Charts visualize visitor activity patterns
- [ ] Date range selector for custom reporting periods

## Implementation

### Admin Dashboard
**File: `Components/Pages/Admin/Dashboard.razor`**
```razor
@page "/admin/dashboard"
@using VisitorTracking.Constants
@attribute [Authorize(Policy = AuthorizationPolicies.AdminPolicy)]
@inject IVisitorService VisitorService
@inject IAnalyticsService AnalyticsService

<PageTitle>Admin Dashboard</PageTitle>

<h3>Admin Dashboard</h3>

<div class="row mb-3">
    <div class="col-md-3">
        <label class="form-label">From</label>
        <input type="date" class="form-control" @bind="dateFrom" />
    </div>
    <div class="col-md-3">
        <label class="form-label">To</label>
        <input type="date" class="form-control" @bind="dateTo" />
    </div>
    <div class="col-md-2">
        <label class="form-label">&nbsp;</label>
        <button class="btn btn-primary w-100" @onclick="LoadDashboard">Refresh</button>
    </div>
</div>

@if (isLoading)
{
    <p>Loading dashboard data...</p>
}
else if (stats != null)
{
    <!-- Key Metrics -->
    <div class="row mb-4">
        <div class="col-md-3">
            <div class="card text-white bg-primary">
                <div class="card-body">
                    <h6 class="card-title">Total Visitors</h6>
                    <p class="card-text display-4">@stats.TotalVisitors</p>
                    <small>@GetDateRangeText()</small>
                </div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="card text-white bg-success">
                <div class="card-body">
                    <h6 class="card-title">Current Visitors</h6>
                    <p class="card-text display-4">@stats.CurrentVisitors</p>
                    <small>Checked in now</small>
                </div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="card text-white bg-info">
                <div class="card-body">
                    <h6 class="card-title">Avg. Duration</h6>
                    <p class="card-text display-4">@stats.AverageDuration.TotalHours.ToString("F1")h</p>
                    <small>Per visit</small>
                </div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="card text-white bg-warning">
                <div class="card-body">
                    <h6 class="card-title">Pre-registered</h6>
                    <p class="card-text display-4">@stats.PreRegisteredCount</p>
                    <small>@((stats.PreRegisteredPercentage).ToString("F1"))% of total</small>
                </div>
            </div>
        </div>
    </div>

    <!-- Daily Visitor Chart -->
    <div class="row mb-4">
        <div class="col-md-12">
            <div class="card">
                <div class="card-header">
                    <h5>Daily Visitor Trends</h5>
                </div>
                <div class="card-body">
                    <canvas id="dailyVisitorChart"></canvas>
                </div>
            </div>
        </div>
    </div>

    <!-- Peak Hours -->
    <div class="row mb-4">
        <div class="col-md-6">
            <div class="card">
                <div class="card-header">
                    <h5>Peak Hours</h5>
                </div>
                <div class="card-body">
                    <table class="table table-sm">
                        <thead>
                            <tr>
                                <th>Hour</th>
                                <th>Check-ins</th>
                                <th>Check-outs</th>
                            </tr>
                        </thead>
                        <tbody>
                            @foreach (var hour in stats.PeakHours.OrderByDescending(h => h.CheckIns).Take(5))
                            {
                                <tr>
                                    <td>@hour.Hour:00</td>
                                    <td>@hour.CheckIns</td>
                                    <td>@hour.CheckOuts</td>
                                </tr>
                            }
                        </tbody>
                    </table>
                </div>
            </div>
        </div>

        <!-- Top Companies -->
        <div class="col-md-6">
            <div class="card">
                <div class="card-header">
                    <h5>Top Companies</h5>
                </div>
                <div class="card-body">
                    <table class="table table-sm">
                        <thead>
                            <tr>
                                <th>Company</th>
                                <th>Visitors</th>
                                <th>Avg. Duration</th>
                            </tr>
                        </thead>
                        <tbody>
                            @foreach (var company in stats.TopCompanies.Take(10))
                            {
                                <tr>
                                    <td>@company.CompanyName</td>
                                    <td>@company.VisitorCount</td>
                                    <td>@company.AverageDuration.TotalHours.ToString("F1")h</td>
                                </tr>
                            }
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    </div>

    <!-- Export Options -->
    <div class="row">
        <div class="col-md-12">
            <div class="card">
                <div class="card-header">
                    <h5>Export Data</h5>
                </div>
                <div class="card-body">
                    <button class="btn btn-success me-2" @onclick="ExportToCsv">
                        Export to CSV
                    </button>
                    <button class="btn btn-primary me-2" @onclick="ExportToExcel">
                        Export to Excel
                    </button>
                    <button class="btn btn-info" @onclick="GeneratePdfReport">
                        Generate PDF Report
                    </button>
                </div>
            </div>
        </div>
    </div>
}

@code {
    private DateTime dateFrom = DateTime.Today.AddDays(-30);
    private DateTime dateTo = DateTime.Today;
    private DashboardStatistics? stats;
    private bool isLoading = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadDashboard();
    }

    private async Task LoadDashboard()
    {
        isLoading = true;
        stats = await AnalyticsService.GetDashboardStatisticsAsync(dateFrom, dateTo);
        isLoading = false;
        
        // Update chart after render
        StateHasChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!isLoading && stats != null)
        {
            await UpdateChart();
        }
    }

    private async Task UpdateChart()
    {
        // Call JavaScript to render Chart.js
        var chartData = stats!.DailyVisitors.Select(d => new 
        { 
            date = d.Date.ToString("yyyy-MM-dd"), 
            count = d.Count 
        }).ToArray();
        
        // await JS.InvokeVoidAsync("renderVisitorChart", chartData);
    }

    private string GetDateRangeText()
    {
        return $"{dateFrom:MMM d} - {dateTo:MMM d, yyyy}";
    }

    private async Task ExportToCsv()
    {
        var csv = await AnalyticsService.ExportStatisticsToCsv(dateFrom, dateTo);
        // Trigger download
    }

    private async Task ExportToExcel()
    {
        // Implementation for Excel export
    }

    private async Task GeneratePdfReport()
    {
        // Implementation for PDF report
    }
}
```

### Analytics Service
**File: `Services/IAnalyticsService.cs`**
```csharp
public interface IAnalyticsService
{
    Task<DashboardStatistics> GetDashboardStatisticsAsync(DateTime from, DateTime to);
    Task<string> ExportStatisticsToCsv(DateTime from, DateTime to);
}
```

**File: `Services/AnalyticsService.cs`**
```csharp
using Microsoft.EntityFrameworkCore;

namespace VisitorTracking.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly VisitorTrackingContext _context;

    public AnalyticsService(VisitorTrackingContext context)
    {
        _context = context;
    }

    public async Task<DashboardStatistics> GetDashboardStatisticsAsync(
        DateTime from, 
        DateTime to)
    {
        var visitors = await _context.Visitors
            .Where(v => v.CreatedAt >= from && v.CreatedAt <= to.AddDays(1))
            .ToListAsync();

        var currentVisitors = visitors.Count(v => v.Status == VisitorStatus.Arrived);
        
        var completedVisits = visitors
            .Where(v => v.CheckInTime.HasValue && v.CheckOutTime.HasValue)
            .ToList();

        var averageDuration = completedVisits.Any()
            ? TimeSpan.FromMinutes(completedVisits
                .Average(v => (v.CheckOutTime!.Value - v.CheckInTime!.Value).TotalMinutes))
            : TimeSpan.Zero;

        var preRegistered = visitors.Count(v => !string.IsNullOrEmpty(v.CreatedByUserId));
        var preRegisteredPercentage = visitors.Any() 
            ? (preRegistered / (double)visitors.Count) * 100 
            : 0;

        // Daily visitor counts
        var dailyVisitors = visitors
            .GroupBy(v => v.CreatedAt.Date)
            .Select(g => new DailyVisitorCount
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();

        // Peak hours
        var peakHours = visitors
            .Where(v => v.CheckInTime.HasValue)
            .GroupBy(v => v.CheckInTime!.Value.Hour)
            .Select(g => new PeakHourData
            {
                Hour = g.Key,
                CheckIns = g.Count(),
                CheckOuts = visitors
                    .Count(v => v.CheckOutTime.HasValue && 
                                v.CheckOutTime.Value.Hour == g.Key)
            })
            .ToList();

        // Top companies
        var topCompanies = visitors
            .GroupBy(v => v.Company)
            .Select(g => new CompanyStatistics
            {
                CompanyName = g.Key,
                VisitorCount = g.Count(),
                AverageDuration = g.Where(v => v.CheckInTime.HasValue && v.CheckOutTime.HasValue)
                    .Any()
                    ? TimeSpan.FromMinutes(g
                        .Where(v => v.CheckInTime.HasValue && v.CheckOutTime.HasValue)
                        .Average(v => (v.CheckOutTime!.Value - v.CheckInTime!.Value).TotalMinutes))
                    : TimeSpan.Zero
            })
            .OrderByDescending(c => c.VisitorCount)
            .ToList();

        return new DashboardStatistics
        {
            TotalVisitors = visitors.Count,
            CurrentVisitors = currentVisitors,
            AverageDuration = averageDuration,
            PreRegisteredCount = preRegistered,
            PreRegisteredPercentage = preRegisteredPercentage,
            DailyVisitors = dailyVisitors,
            PeakHours = peakHours,
            TopCompanies = topCompanies
        };
    }

    public async Task<string> ExportStatisticsToCsv(DateTime from, DateTime to)
    {
        var stats = await GetDashboardStatisticsAsync(from, to);
        var csv = new StringBuilder();
        
        csv.AppendLine("Visitor Analytics Report");
        csv.AppendLine($"Period: {from:yyyy-MM-dd} to {to:yyyy-MM-dd}");
        csv.AppendLine();
        
        csv.AppendLine("Summary Statistics");
        csv.AppendLine($"Total Visitors,{stats.TotalVisitors}");
        csv.AppendLine($"Current Visitors,{stats.CurrentVisitors}");
        csv.AppendLine($"Average Duration (hours),{stats.AverageDuration.TotalHours:F2}");
        csv.AppendLine($"Pre-registered,{stats.PreRegisteredCount}");
        csv.AppendLine();
        
        csv.AppendLine("Daily Visitor Counts");
        csv.AppendLine("Date,Count");
        foreach (var day in stats.DailyVisitors)
        {
            csv.AppendLine($"{day.Date:yyyy-MM-dd},{day.Count}");
        }
        csv.AppendLine();
        
        csv.AppendLine("Top Companies");
        csv.AppendLine("Company,Visitor Count,Average Duration (hours)");
        foreach (var company in stats.TopCompanies)
        {
            csv.AppendLine($"{company.CompanyName},{company.VisitorCount}," +
                          $"{company.AverageDuration.TotalHours:F2}");
        }
        
        return csv.ToString();
    }
}
```

### Statistics Models
**File: `Models/DashboardStatistics.cs`**
```csharp
namespace VisitorTracking.Models;

public class DashboardStatistics
{
    public int TotalVisitors { get; set; }
    public int CurrentVisitors { get; set; }
    public TimeSpan AverageDuration { get; set; }
    public int PreRegisteredCount { get; set; }
    public double PreRegisteredPercentage { get; set; }
    public List<DailyVisitorCount> DailyVisitors { get; set; } = new();
    public List<PeakHourData> PeakHours { get; set; } = new();
    public List<CompanyStatistics> TopCompanies { get; set; } = new();
}

public class DailyVisitorCount
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

public class PeakHourData
{
    public int Hour { get; set; }
    public int CheckIns { get; set; }
    public int CheckOuts { get; set; }
}

public class CompanyStatistics
{
    public string CompanyName { get; set; } = "";
    public int VisitorCount { get; set; }
    public TimeSpan AverageDuration { get; set; }
}
```

### Chart.js Integration
**File: `wwwroot/js/dashboard-charts.js`**
```javascript
window.renderVisitorChart = (data) => {
    const ctx = document.getElementById('dailyVisitorChart');
    new Chart(ctx, {
        type: 'line',
        data: {
            labels: data.map(d => d.date),
            datasets: [{
                label: 'Daily Visitors',
                data: data.map(d => d.count),
                borderColor: 'rgb(75, 192, 192)',
                tension: 0.1
            }]
        },
        options: {
            responsive: true,
            scales: {
                y: {
                    beginAtZero: true
                }
            }
        }
    });
};
```

## Testing Requirements

### Unit Tests
```csharp
[Fact]
public async Task GetDashboardStatisticsAsync_CalculatesCorrectAverageDuration()
{
    // Arrange
    var context = GetInMemoryContext();
    var checkIn = DateTime.UtcNow.AddHours(-2);
    var checkOut = DateTime.UtcNow;
    
    context.Visitors.Add(new Visitor 
    { 
        CheckInTime = checkIn, 
        CheckOutTime = checkOut 
    });
    await context.SaveChangesAsync();
    
    var service = new AnalyticsService(context);

    // Act
    var stats = await service.GetDashboardStatisticsAsync(
        DateTime.Today, 
        DateTime.Today);

    // Assert
    Assert.Equal(2, stats.AverageDuration.TotalHours, 0.1);
}
```

## Definition of Done
- [ ] Dashboard displays all key metrics
- [ ] Charts visualize visitor trends
- [ ] Date range filtering works
- [ ] Peak hours analysis accurate
- [ ] Company statistics calculated correctly
- [ ] CSV export functional
- [ ] Performance tested with large datasets
- [ ] All tests pass
