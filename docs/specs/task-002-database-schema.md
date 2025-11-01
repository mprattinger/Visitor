# TASK-002: Database Schema Design & EF Core Setup

## Overview
**Priority**: High  
**Dependencies**: TASK-001  
**Estimated Effort**: 2-3 days  
**Phase**: Infrastructure & Setup

## Description
Design and implement the complete database schema using Entity Framework Core with SQLite, including all entities, relationships, and initial migrations for the visitor tracking system.

## Acceptance Criteria
- [ ] Design and implement `Visitor` entity with all required properties
- [ ] Design and implement `User` entity for employee information
- [ ] Create `VisitorStatus` enum (Planned, Arrived, Left)
- [ ] Create `UserRole` enum (Employee, Admin)
- [ ] Implement proper entity relationships and constraints
- [ ] Create initial EF Core migration
- [ ] Set up database seeding for development data
- [ ] Configure EF Core context with proper configurations
- [ ] Implement database indexing for performance optimization

## Entity Specifications

### Visitor Entity
**File: `Data/Entities/Visitor.cs`**
```csharp
using System.ComponentModel.DataAnnotations;

namespace VisitorTracking.Data.Entities
{
    public class Visitor
    {
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Company { get; set; } = string.Empty;
        
        [Required]
        public TimeSpan PlannedDuration { get; set; }
        
        public VisitorStatus Status { get; set; } = VisitorStatus.Planned;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ArrivedAt { get; set; }
        
        public DateTime? LeftAt { get; set; }
        
        public string? CreatedByUserId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string VisitorToken { get; set; } = string.Empty;
        
        // Navigation Properties
        public User? CreatedByUser { get; set; }
    }
}
```

### User Entity
**File: `Data/Entities/User.cs`**
```csharp
using System.ComponentModel.DataAnnotations;

namespace VisitorTracking.Data.Entities
{
    public class User
    {
        [Required]
        [MaxLength(100)]
        public string Id { get; set; } = string.Empty; // From Entra ID
        
        [Required]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string DisplayName { get; set; } = string.Empty;
        
        public UserRole Role { get; set; } = UserRole.Employee;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastLoginAt { get; set; }
        
        // Navigation Properties
        public ICollection<Visitor> PreRegisteredVisitors { get; set; } = new List<Visitor>();
    }
}
```

### Enums
**File: `Data/Entities/VisitorStatus.cs`**
```csharp
namespace VisitorTracking.Data.Entities
{
    public enum VisitorStatus
    {
        Planned = 0,
        Arrived = 1,
        Left = 2
    }
}
```

**File: `Data/Entities/UserRole.cs`**
```csharp
namespace VisitorTracking.Data.Entities
{
    public enum UserRole
    {
        Employee = 0,
        Admin = 1
    }
}
```

## Database Context Configuration

### DbContext Implementation
**File: `Data/Context/VisitorTrackingContext.cs`**
```csharp
using Microsoft.EntityFrameworkCore;
using VisitorTracking.Data.Entities;

namespace VisitorTracking.Data.Context
{
    public class VisitorTrackingContext : DbContext
    {
        public VisitorTrackingContext(DbContextOptions<VisitorTrackingContext> options)
            : base(options)
        {
        }

        public DbSet<Visitor> Visitors { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Visitor Configuration
            modelBuilder.Entity<Visitor>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Company).IsRequired().HasMaxLength(100);
                entity.Property(e => e.VisitorToken).IsRequired().HasMaxLength(50);
                
                entity.HasIndex(e => e.VisitorToken).IsUnique();
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => new { e.Name, e.Company, e.CreatedAt });
                
                entity.HasOne(e => e.CreatedByUser)
                      .WithMany(u => u.PreRegisteredVisitors)
                      .HasForeignKey(e => e.CreatedByUserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // User Configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
                
                entity.HasIndex(e => e.Email).IsUnique();
            });
        }
    }
}
```

### Program.cs Configuration
Add to `Program.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using VisitorTracking.Data.Context;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<VisitorTrackingContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ... rest of configuration
```

## Implementation Tasks

### 1. Create Entity Classes
- [ ] Create `Visitor` entity with all properties
- [ ] Create `User` entity with all properties
- [ ] Create `VisitorStatus` enum
- [ ] Create `UserRole` enum

### 2. Configure Entity Framework
- [ ] Implement `VisitorTrackingContext`
- [ ] Configure entity relationships
- [ ] Set up database indexes
- [ ] Configure cascade delete behaviors

### 3. Create Initial Migration
```bash
dotnet ef migrations add InitialCreate
```

### 4. Apply Migration
```bash
dotnet ef database update
```

### 5. Database Seeding
**File: `Data/Context/DbSeeder.cs`**
```csharp
using VisitorTracking.Data.Entities;

namespace VisitorTracking.Data.Context
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(VisitorTrackingContext context)
        {
            // Seed test users (for development without Entra ID)
            if (!context.Users.Any())
            {
                var admin = new User
                {
                    Id = "test-admin-001",
                    Email = "admin@company.com",
                    DisplayName = "Test Administrator",
                    Role = UserRole.Admin,
                    CreatedAt = DateTime.UtcNow
                };

                var employee = new User
                {
                    Id = "test-employee-001",
                    Email = "employee@company.com",
                    DisplayName = "Test Employee",
                    Role = UserRole.Employee,
                    CreatedAt = DateTime.UtcNow
                };

                context.Users.AddRange(admin, employee);
                await context.SaveChangesAsync();
            }

            // Seed test visitors
            if (!context.Visitors.Any())
            {
                var visitors = new List<Visitor>
                {
                    new Visitor
                    {
                        Id = Guid.NewGuid(),
                        Name = "John Doe",
                        Company = "Acme Corp",
                        PlannedDuration = TimeSpan.FromHours(2),
                        Status = VisitorStatus.Planned,
                        CreatedAt = DateTime.UtcNow,
                        VisitorToken = GenerateToken()
                    },
                    new Visitor
                    {
                        Id = Guid.NewGuid(),
                        Name = "Jane Smith",
                        Company = "TechCo",
                        PlannedDuration = TimeSpan.FromHours(1),
                        Status = VisitorStatus.Arrived,
                        CreatedAt = DateTime.UtcNow.AddHours(-1),
                        ArrivedAt = DateTime.UtcNow.AddMinutes(-30),
                        VisitorToken = GenerateToken()
                    }
                };

                context.Visitors.AddRange(visitors);
                await context.SaveChangesAsync();
            }
        }

        private static string GenerateToken()
        {
            return Guid.NewGuid().ToString("N")[..16].ToUpper();
        }
    }
}
```

### 6. Configure Seeding in Program.cs
```csharp
var app = builder.Build();

// Seed database in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<VisitorTrackingContext>();
    await context.Database.MigrateAsync();
    await DbSeeder.SeedAsync(context);
}
```

## Performance Optimization

### Database Indexes
- Index on `VisitorToken` (unique) for fast visitor lookup
- Index on `Status` for filtering active visitors
- Index on `CreatedAt` for date-based queries
- Composite index on `Name`, `Company`, `CreatedAt` for duplicate detection

### Connection Pooling
Already configured by default in EF Core with SQLite.

## Testing Requirements

### Unit Tests
```csharp
[Test]
public async Task Visitor_ShouldCreateWithValidData()
{
    // Arrange
    var options = new DbContextOptionsBuilder<VisitorTrackingContext>()
        .UseInMemoryDatabase(databaseName: "TestDb")
        .Options;

    using var context = new VisitorTrackingContext(options);

    // Act
    var visitor = new Visitor
    {
        Id = Guid.NewGuid(),
        Name = "Test Visitor",
        Company = "Test Company",
        PlannedDuration = TimeSpan.FromHours(1),
        VisitorToken = "TEST123"
    };

    context.Visitors.Add(visitor);
    await context.SaveChangesAsync();

    // Assert
    Assert.AreEqual(1, await context.Visitors.CountAsync());
}
```

## Definition of Done
- [ ] All entities are properly configured with EF Core
- [ ] Database can be created and seeded with test data
- [ ] Migration system is working correctly (up and down migrations)
- [ ] Database relationships are properly established
- [ ] Performance indexes are in place and tested
- [ ] Entity validations work correctly
- [ ] Seed data provides good test scenarios
- [ ] DbContext is properly configured for dependency injection
- [ ] Unit tests for entity operations are passing

## Technical Notes
- Use Fluent API for complex configurations
- Ensure proper handling of UTC timestamps
- Configure cascade delete behavior appropriately (SetNull for CreatedByUser)
- Visitor tokens should be cryptographically secure and unique
- Consider implementing soft delete for audit requirements in future phases

## Dependencies for Next Tasks
- TASK-003 (Entra ID Authentication) requires User entity
- TASK-007 (Visitor Registration) requires Visitor entity
- All data-related tasks depend on this database foundation
