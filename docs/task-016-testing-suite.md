# TASK-016: Comprehensive Testing Suite

## Overview
**Priority**: Critical  
**Dependencies**: All previous tasks  
**Estimated Effort**: 3-4 days  
**Phase**: Quality Assurance  

## Description
Implement comprehensive testing coverage including unit tests, integration tests, and end-to-end tests.

## Acceptance Criteria
- [ ] Unit tests for all services (>80% coverage)
- [ ] Integration tests for database operations
- [ ] End-to-end tests for critical user flows
- [ ] Performance tests for high-load scenarios
- [ ] Security testing for authorization
- [ ] All tests pass in CI/CD pipeline
- [ ] Test documentation complete

## Implementation

### Unit Tests - Visitor Service
**File: `Tests/Services/VisitorServiceTests.cs`**
```csharp
using Microsoft.EntityFrameworkCore;
using VisitorTracking.Data;
using VisitorTracking.Models;
using VisitorTracking.Services;
using Xunit;

namespace VisitorTracking.Tests.Services;

public class VisitorServiceTests : IDisposable
{
    private readonly VisitorTrackingContext _context;
    private readonly VisitorService _service;

    public VisitorServiceTests()
    {
        var options = new DbContextOptionsBuilder<VisitorTrackingContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new VisitorTrackingContext(options);
        _service = new VisitorService(_context, null!, null!);
    }

    [Fact]
    public async Task RegisterVisitorAsync_CreatesNewVisitor()
    {
        // Arrange
        var dto = new VisitorRegistrationDto
        {
            Name = "John Doe",
            Company = "Acme Corp",
            PlannedDuration = TimeSpan.FromHours(2)
        };

        // Act
        var result = await _service.RegisterVisitorAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal(VisitorStatus.Planned, result.Status);
        Assert.NotEmpty(result.VisitorToken);
    }

    [Fact]
    public async Task GetVisitorByTokenAsync_ReturnsCorrectVisitor()
    {
        // Arrange
        var visitor = new Visitor
        {
            Id = "1",
            Name = "Jane Smith",
            VisitorToken = "TOKEN123",
            Status = VisitorStatus.Planned
        };
        _context.Visitors.Add(visitor);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetVisitorByTokenAsync("TOKEN123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Jane Smith", result.Name);
    }

    [Fact]
    public async Task UpdateVisitorStatusAsync_TransitionsCorrectly()
    {
        // Arrange
        var visitor = new Visitor
        {
            Id = "1",
            Name = "Test User",
            Status = VisitorStatus.Planned,
            VisitorToken = "TOKEN"
        };
        _context.Visitors.Add(visitor);
        await _context.SaveChangesAsync();

        // Act - Check in
        var checkedIn = await _service.UpdateVisitorStatusAsync("TOKEN", VisitorStatus.Arrived);

        // Assert
        Assert.Equal(VisitorStatus.Arrived, checkedIn.Status);
        Assert.NotNull(checkedIn.CheckInTime);

        // Act - Check out
        var checkedOut = await _service.UpdateVisitorStatusAsync("TOKEN", VisitorStatus.Left);

        // Assert
        Assert.Equal(VisitorStatus.Left, checkedOut.Status);
        Assert.NotNull(checkedOut.CheckOutTime);
    }

    [Fact]
    public async Task GetTodaysVisitorsAsync_ReturnsOnlyTodaysVisitors()
    {
        // Arrange
        var today = DateTime.UtcNow;
        var yesterday = DateTime.UtcNow.AddDays(-1);

        _context.Visitors.AddRange(
            new Visitor { Id = "1", CreatedAt = today, Status = VisitorStatus.Planned },
            new Visitor { Id = "2", CreatedAt = yesterday, Status = VisitorStatus.Planned },
            new Visitor { Id = "3", CreatedAt = today, Status = VisitorStatus.Arrived }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetTodaysVisitorsAsync();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, v => Assert.Equal(today.Date, v.CreatedAt.Date));
    }

    [Fact]
    public async Task DeleteVisitorAsync_RemovesVisitor()
    {
        // Arrange
        var visitor = new Visitor { Id = "1", Name = "Test" };
        _context.Visitors.Add(visitor);
        await _context.SaveChangesAsync();

        // Act
        await _service.DeleteVisitorAsync("1");

        // Assert
        var result = await _context.Visitors.FindAsync("1");
        Assert.Null(result);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
```

### Unit Tests - Analytics Service
**File: `Tests/Services/AnalyticsServiceTests.cs`**
```csharp
using Xunit;

namespace VisitorTracking.Tests.Services;

public class AnalyticsServiceTests : IDisposable
{
    private readonly VisitorTrackingContext _context;
    private readonly AnalyticsService _service;

    public AnalyticsServiceTests()
    {
        var options = new DbContextOptionsBuilder<VisitorTrackingContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new VisitorTrackingContext(options);
        _service = new AnalyticsService(_context);
    }

    [Fact]
    public async Task GetDashboardStatisticsAsync_CalculatesCorrectTotals()
    {
        // Arrange
        var today = DateTime.UtcNow;
        _context.Visitors.AddRange(
            new Visitor 
            { 
                Id = "1", 
                CreatedAt = today, 
                Status = VisitorStatus.Arrived,
                CheckInTime = today.AddHours(-2),
                CheckOutTime = today
            },
            new Visitor 
            { 
                Id = "2", 
                CreatedAt = today, 
                Status = VisitorStatus.Left,
                CheckInTime = today.AddHours(-3),
                CheckOutTime = today.AddHours(-1)
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var stats = await _service.GetDashboardStatisticsAsync(
            today.Date, 
            today.Date);

        // Assert
        Assert.Equal(2, stats.TotalVisitors);
        Assert.Equal(2.5, stats.AverageDuration.TotalHours, 1);
    }

    [Fact]
    public async Task GetDashboardStatisticsAsync_CalculatesPeakHours()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        _context.Visitors.AddRange(
            new Visitor { CheckInTime = today.AddHours(9) },  // 9 AM
            new Visitor { CheckInTime = today.AddHours(9) },  // 9 AM
            new Visitor { CheckInTime = today.AddHours(14) }  // 2 PM
        );
        await _context.SaveChangesAsync();

        // Act
        var stats = await _service.GetDashboardStatisticsAsync(today, today);

        // Assert
        var peakHour = stats.PeakHours.OrderByDescending(h => h.CheckIns).First();
        Assert.Equal(9, peakHour.Hour);
        Assert.Equal(2, peakHour.CheckIns);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
```

### Integration Tests
**File: `Tests/Integration/VisitorFlowIntegrationTests.cs`**
```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace VisitorTracking.Tests.Integration;

public class VisitorFlowIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public VisitorFlowIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace production DB with test DB
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<VisitorTrackingContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<VisitorTrackingContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });
            });
        });
    }

    [Fact]
    public async Task CompleteVisitorFlow_RegisterCheckInCheckOut_Success()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act 1: Register visitor
        var registrationDto = new VisitorRegistrationDto
        {
            Name = "Integration Test User",
            Company = "Test Corp",
            PlannedDuration = TimeSpan.FromHours(1)
        };

        var registerResponse = await client.PostAsJsonAsync(
            "/api/visitors/register", 
            registrationDto);

        // Assert 1
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        var visitor = await registerResponse.Content.ReadFromJsonAsync<Visitor>();
        Assert.NotNull(visitor);
        Assert.Equal(VisitorStatus.Planned, visitor.Status);

        // Act 2: Check in
        var checkInResponse = await client.PostAsync(
            $"/api/visitors/{visitor.VisitorToken}/checkin", 
            null);

        // Assert 2
        Assert.Equal(HttpStatusCode.OK, checkInResponse.StatusCode);
        var checkedInVisitor = await checkInResponse.Content.ReadFromJsonAsync<Visitor>();
        Assert.Equal(VisitorStatus.Arrived, checkedInVisitor!.Status);

        // Act 3: Check out
        var checkOutResponse = await client.PostAsync(
            $"/api/visitors/{visitor.VisitorToken}/checkout", 
            null);

        // Assert 3
        Assert.Equal(HttpStatusCode.OK, checkOutResponse.StatusCode);
        var checkedOutVisitor = await checkOutResponse.Content.ReadFromJsonAsync<Visitor>();
        Assert.Equal(VisitorStatus.Left, checkedOutVisitor!.Status);
    }
}
```

### End-to-End Tests with Playwright
**File: `Tests/E2E/VisitorRegistrationTests.cs`**
```csharp
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace VisitorTracking.Tests.E2E;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class VisitorRegistrationTests : PageTest
{
    [Test]
    public async Task GuestUser_CanRegisterVisit()
    {
        // Navigate to registration page
        await Page.GotoAsync("https://localhost:5001/register");

        // Fill in form
        await Page.FillAsync("#Name", "John Doe");
        await Page.FillAsync("#Company", "Acme Corp");
        await Page.FillAsync("#PlannedDuration", "2");

        // Submit
        await Page.ClickAsync("button[type='submit']");

        // Wait for confirmation
        await Page.WaitForSelectorAsync("text=Registration Successful");

        // Verify token displayed
        var token = await Page.TextContentAsync(".visitor-token");
        Assert.That(token, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task GuestUser_CanCheckInWithToken()
    {
        // Setup: Create a visitor first
        var token = await CreateTestVisitor();

        // Navigate to check-in page
        await Page.GotoAsync($"https://localhost:5001/status/{token}");

        // Click check-in button
        await Page.ClickAsync("button:has-text('Check In')");

        // Verify status changed
        await Page.WaitForSelectorAsync("text=Status: Arrived");
    }

    private async Task<string> CreateTestVisitor()
    {
        // Implementation to create test visitor via API
        return "TEST-TOKEN";
    }
}
```

### Performance Tests
**File: `Tests/Performance/LoadTests.cs`**
```csharp
using NBomber.CSharp;
using NBomber.Http.CSharp;
using Xunit;

namespace VisitorTracking.Tests.Performance;

public class LoadTests
{
    [Fact]
    public void VisitorRegistration_HandlesHighLoad()
    {
        var scenario = Scenario.Create("visitor_registration", async context =>
        {
            var request = Http.CreateRequest("POST", "https://localhost:5001/api/visitors/register")
                .WithJsonBody(new
                {
                    Name = "Load Test User",
                    Company = "Test Corp",
                    PlannedDuration = "PT2H"
                });

            var response = await Http.Send(request, context);
            return response;
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        var okCount = stats.ScenarioStats[0].Ok.Request.Count;
        var failCount = stats.ScenarioStats[0].Fail.Request.Count;

        Assert.True(okCount > 2000); // At least 2000 successful requests
        Assert.True(failCount < 100);  // Less than 100 failures
    }
}
```

### Test Configuration
**File: `Tests/VisitorTracking.Tests.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
    <PackageReference Include="coverlet.collector" Version="6.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
    <PackageReference Include="Moq" Version="4.20.70" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.Playwright.NUnit" Version="1.40.0" />
    <PackageReference Include="NBomber" Version="5.4.0" />
    <PackageReference Include="NBomber.Http" Version="5.4.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\VisitorTracking\VisitorTracking.csproj" />
  </ItemGroup>

</Project>
```

### GitHub Actions Test Workflow
**File: `.github/workflows/tests.yml`**
```yaml
name: Run Tests

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '10.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore

    - name: Run Unit Tests
      run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"

    - name: Upload Coverage
      uses: codecov/codecov-action@v3
      with:
        files: '**/coverage.cobertura.xml'

    - name: Install Playwright
      run: pwsh Tests/bin/Debug/net10.0/playwright.ps1 install

    - name: Run E2E Tests
      run: dotnet test Tests/VisitorTracking.Tests.csproj --filter "Category=E2E"
```

## Testing Checklist
- [ ] All service methods have unit tests
- [ ] Database operations have integration tests
- [ ] Critical user flows have E2E tests
- [ ] Authorization logic tested
- [ ] Performance under load validated
- [ ] Test coverage >80%
- [ ] All tests pass in CI/CD
- [ ] Test data properly cleaned up

## Definition of Done
- [ ] Unit test coverage >80%
- [ ] All integration tests pass
- [ ] E2E tests cover main workflows
- [ ] Performance tests meet requirements
- [ ] Security tests validate authorization
- [ ] CI/CD pipeline runs all tests
- [ ] Test documentation complete
