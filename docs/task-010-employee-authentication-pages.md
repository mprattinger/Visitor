# TASK-010: Employee Authentication Pages (VT-005)

## Overview
**Priority**: High  
**Dependencies**: TASK-003, TASK-004  
**Estimated Effort**: 1-2 days  
**Phase**: Core Functionality  
**User Story**: VT-005

## Description
Create employee authentication flow and dashboard access with proper Entra ID integration.

## User Story
**As a** company employee  
**I want to** authenticate using my Entra ID credentials  
**So that** I can access the employee features securely

## Acceptance Criteria
- [ ] Employee login page with Entra ID authentication
- [ ] Proper authentication redirect flow
- [ ] Employee dashboard with appropriate navigation
- [ ] Authentication failures handled with clear error messages
- [ ] Authentication session maintained across browser sessions
- [ ] User roles determined from Active Directory group membership
- [ ] Logout functionality implemented

## Implementation

### Employee Dashboard
**File: `Components/Pages/Employee/Dashboard.razor`**
```razor
@page "/employee/dashboard"
@using VisitorTracking.Constants
@attribute [Authorize(Policy = AuthorizationPolicies.EmployeePolicy)]
@inject IUserService UserService

<PageTitle>Employee Dashboard</PageTitle>

<AuthorizeView Policy="@AuthorizationPolicies.EmployeePolicy">
    <Authorized>
        <h3>Welcome, @context.User.Identity?.Name!</h3>
        
        <div class="row mt-4">
            <div class="col-md-6">
                <div class="card">
                    <div class="card-header bg-primary text-white">
                        <h5>My Pre-Registered Visitors</h5>
                    </div>
                    <div class="card-body">
                        <a href="/employee/visitors/preregister" class="btn btn-success">
                            Pre-Register New Visitor
                        </a>
                        <a href="/employee/visitors" class="btn btn-primary mt-2">
                            View All Visitors
                        </a>
                    </div>
                </div>
            </div>

            <AuthorizeView Roles="Admin">
                <div class="col-md-6">
                    <div class="card">
                        <div class="card-header bg-danger text-white">
                            <h5>Admin Functions</h5>
                        </div>
                        <div class="card-body">
                            <a href="/admin/panel" class="btn btn-danger">
                                Admin Panel
                            </a>
                        </div>
                    </div>
                </div>
            </AuthorizeView>
        </div>
    </Authorized>
    <NotAuthorized>
        <p>Please <a href="MicrosoftIdentity/Account/SignIn">sign in</a> to access this page.</p>
    </NotAuthorized>
</AuthorizeView>
```

## Definition of Done
- [ ] Employees can authenticate with Entra ID
- [ ] Dashboard displays user-specific content
- [ ] Role-based content renders correctly
- [ ] Session management works properly
- [ ] Logout functionality works
- [ ] Error handling provides clear feedback
