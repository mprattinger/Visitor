# PRD: Company Visitor Tracking System

## 1. Product overview

### 1.1 Document title and version

* PRD: Company Visitor Tracking System
* Version: 1.0

### 1.2 Product summary

The Company Visitor Tracking System is a .NET 10 Blazor Server application designed to streamline visitor management within the company premises. The system provides a dual-interface solution where visitors can self-register upon arrival or select from pre-registered visits, while employees can manage and oversee visitor activities through an authenticated portal.

The application features role-based access with Entra ID authentication for employees and guest access for visitors. It tracks visitor status throughout their journey from planned visits to departure, ensuring security compliance and operational visibility. The system uses SQLite database with Entity Framework Core for data persistence and is designed for internal deployment via Docker containers.

## 2. Goals

### 2.1 Business goals

* Improve security by maintaining accurate visitor records and real-time status tracking
* Streamline visitor registration process to reduce wait times and administrative overhead
* Provide comprehensive visitor oversight for security and compliance purposes
* Enable proactive visitor management through pre-registration capabilities
* Reduce manual paperwork and administrative tasks related to visitor management

### 2.2 User goals

* **Visitors**: Quick and easy registration process with minimal barriers to entry
* **Employees**: Efficient pre-registration of expected visitors to expedite their arrival
* **Admins**: Complete visibility and control over all visitor activities and data management
* **Security personnel**: Real-time awareness of visitor status and presence on premises

### 2.3 Non-goals

* Integration with external visitor management systems or third-party services
* Mobile application development (web-based responsive design only)
* Notification systems or automated alerts (future phase consideration)
* Badge printing or physical access control system integration
* Advanced reporting and analytics features

## 3. User personas

### 3.1 Key user types

* Company visitors (external guests)
* Company employees (authenticated users)
* System administrators (IT staff with full system access)
* Security personnel (monitoring and oversight)

### 3.2 Basic persona details

* **External Visitor**: Business partners, clients, contractors, or guests visiting company premises who need to register their presence and intended duration of stay
* **Company Employee**: Internal staff members who can pre-register expected visitors and access basic visitor information relevant to their work
* **System Administrator**: IT personnel responsible for managing the system, user permissions, and data maintenance with full CRUD operations

### 3.3 Role-based access

* **Guest User** (Unauthenticated): Visitor registration, status updates (check-in/check-out), view own visit information
* **Employee** (Entra ID Authenticated): Pre-register visitors, view visitor list, update own pre-registered visitor information
* **Admin** (Entra ID Authenticated + Admin Role): Full system access, delete visitor entries, manage all visitor records, system configuration

## 4. Functional requirements

* **Visitor Self-Registration** (Priority: High)
  * Guest users can register without authentication by providing name, company, and planned visit duration
  * System generates unique visitor record with "Planned" status
  * Visitors can update their own status to "Arrived" and later to "Left"

* **Employee Pre-Registration** (Priority: High)
  * Authenticated employees can create visitor entries in advance
  * Pre-registered visitors appear in selection list for quick check-in upon arrival
  * Employees can modify or cancel their own pre-registered visitor entries

* **Admin Visitor Management** (Priority: High)
  * Complete CRUD operations on all visitor records
  * Ability to delete visitor entries for data management and compliance
  * Administrative oversight dashboard with all visitor information and status

* **Entra ID Authentication** (Priority: High)
  * Integration with company Active Directory through Entra ID
  * Role-based access control distinguishing between employees and administrators
  * Automatic user provisioning based on Active Directory group membership

* **Status Tracking** (Priority: Medium)
  * Three-state visitor lifecycle: Planned → Arrived → Left
  * Real-time status updates reflecting visitor presence on premises
  * Historical record maintenance for audit and compliance purposes

## 5. User experience

### 5.1 Entry points & first-time user flow

* **Guest Access**: Direct web access to visitor registration without authentication barriers
* **Employee Access**: Entra ID authentication redirect, then access to employee dashboard
* **Admin Access**: Same authentication flow as employees with enhanced permissions based on role assignment

### 5.2 Core experience

* **Visitor Registration**: Simple, intuitive form with minimal required fields ensuring quick processing
  * Creates welcoming first impression while maintaining necessary security protocols

* **Employee Pre-Registration**: Streamlined workflow integrated into daily operations
  * Reduces visitor wait times and demonstrates proactive hospitality to business partners

* **Status Management**: Clear, straightforward status transitions with visual indicators
  * Provides immediate visibility into visitor presence without complexity

* **Administrative Control**: Comprehensive management interface with efficient data operations
  * Ensures system maintainability and compliance with data retention policies

### 5.3 Advanced features & edge cases

* Handling of duplicate visitor registrations or name conflicts
* Data validation and error handling for incomplete or invalid visitor information
* Session management for guest users to prevent unauthorized access to other visitor records
* Graceful degradation when Entra ID authentication services are temporarily unavailable

### 5.4 UI/UX highlights

* Responsive design ensuring usability across desktop and tablet devices
* Clean, professional interface reflecting company branding and security standards
* Intuitive navigation with role-appropriate menu structures and access controls
* Accessibility compliance ensuring usability for visitors with diverse needs

## 6. Narrative

A business partner arrives at the company for a scheduled meeting. Using their mobile device or a lobby kiosk, they quickly access the visitor registration system and either select their name from a pre-registered list (if their host employee added them beforehand) or complete a brief registration form. The system immediately updates their status to "Arrived," providing real-time visibility to security and the hosting employee. Upon departure, the visitor easily updates their status to "Left," completing their visit record. Meanwhile, employees proactively manage their upcoming meetings by pre-registering expected visitors, and administrators maintain system integrity through comprehensive oversight and data management capabilities, all while leveraging the company's existing Active Directory infrastructure for secure, seamless authentication.

## 7. Success metrics

### 7.1 User-centric metrics

* Average visitor registration time under 2 minutes
* Employee adoption rate for pre-registration features above 70%
* User satisfaction scores indicating ease of use and system reliability
* Reduction in visitor wait times compared to manual registration processes

### 7.2 Business metrics

* Improved visitor processing efficiency and reduced administrative overhead
* Enhanced security compliance through complete visitor record maintenance
* Increased professional impression on business partners and clients
* Cost savings from reduced manual processes and paperwork

### 7.3 Technical metrics

* System uptime above 99.5% during business hours
* Authentication response times under 3 seconds for Entra ID integration
* Database query performance supporting concurrent visitor registrations
* Successful Docker deployment and container orchestration reliability

## 8. Technical considerations

### 8.1 Integration points

* Entra ID authentication service integration for employee and admin access
* SQLite database with Entity Framework Core for data persistence and migrations
* Docker containerization for deployment to internal company infrastructure
* GitHub Enterprise integration for CI/CD pipeline and automated deployments

### 8.2 Data storage & privacy

* SQLite database file security and backup procedures for visitor information
* Data retention policies compliance for visitor records and audit trails
* Privacy protection for guest user sessions and visitor personal information
* GDPR considerations for visitor data collection, processing, and deletion rights

### 8.3 Scalability & performance

* SQLite performance optimization for concurrent visitor registration scenarios
* Blazor Server SignalR connection management for real-time status updates
* Caching strategies for Entra ID authentication and user role information
* Database indexing optimization for visitor search and retrieval operations

### 8.4 Potential challenges

* Entra ID integration complexity and authentication flow configuration
* SQLite concurrency limitations under high visitor registration volume
* Docker deployment networking and security configuration requirements
* Browser compatibility and responsive design across various devices and platforms

## 9. Milestones & sequencing

### 9.1 Project estimate

* **Medium**: 6-8 weeks development time

### 9.2 Team size & composition

* **3-4 developers**: 1 backend/.NET developer, 1 frontend/Blazor developer, 1 DevOps engineer, 1 project lead/full-stack developer

### 9.3 Suggested phases

* **Phase 1**: Core infrastructure and authentication (2-3 weeks)
  * Blazor Server project setup with .NET 10
  * SQLite database design and Entity Framework Core configuration
  * Entra ID authentication integration and role-based authorization

* **Phase 2**: Visitor management functionality (2-3 weeks)
  * Guest visitor registration and self-service status updates
  * Employee dashboard and pre-registration capabilities
  * Admin interface with full CRUD operations and visitor oversight

* **Phase 3**: Deployment and testing (1-2 weeks)
  * Docker containerization and deployment pipeline setup
  * GitHub Enterprise CI/CD integration and automated testing
  * User acceptance testing and security validation

## 10. User stories

### 10.1. Visitor self-registration

* **ID**: VT-001
* **Description**: As a company visitor, I want to register my visit by providing my name, company, and planned duration so that the company has a record of my presence.
* **Acceptance criteria**:
  * Visitor can access registration form without authentication
  * Form captures visitor name, company name, and planned visit duration
  * System generates visitor record with unique identifier and "Planned" status
  * Visitor receives confirmation of successful registration
  * Registration data is immediately available to authenticated users

### 10.2. Visitor status check-in

* **ID**: VT-002
* **Description**: As a visitor who has registered, I want to update my status to "Arrived" when I enter the company premises so that my presence is accurately tracked.
* **Acceptance criteria**:
  * Visitor can update their status from "Planned" to "Arrived"
  * Status change is reflected in real-time across the system
  * Only the visitor who created the record can update their own status
  * System prevents status changes to invalid states
  * Timestamp is recorded for the status change

### 10.3. Visitor status check-out

* **ID**: VT-003
* **Description**: As a departing visitor, I want to update my status to "Left" when leaving the company so that my visit is properly concluded.
* **Acceptance criteria**:
  * Visitor can update their status from "Arrived" to "Left"
  * Status change is immediately reflected throughout the system
  * Visitor cannot modify status after marking as "Left"
  * System records departure timestamp
  * Visit duration is automatically calculated and stored

### 10.4. Pre-registered visitor selection

* **ID**: VT-004
* **Description**: As a visitor whose visit was pre-registered by an employee, I want to select my name from a list instead of filling out the registration form so that I can check in quickly.
* **Acceptance criteria**:
  * System displays list of pre-registered visitors for current date
  * Visitor can select their name from the list
  * Selection automatically updates status to "Arrived" with timestamp
  * Pre-registered visitor information (company, duration) is preserved
  * System prevents duplicate selections of the same visitor record

### 10.5. Employee authentication

* **ID**: VT-005
* **Description**: As a company employee, I want to authenticate using my Entra ID credentials so that I can access the employee features securely.
* **Acceptance criteria**:
  * Employee is redirected to Entra ID authentication when accessing employee areas
  * Successful authentication grants access to employee dashboard
  * User role and permissions are determined from Active Directory group membership
  * Authentication session is maintained across browser sessions
  * Failed authentication provides appropriate error messaging

### 10.6. Employee visitor pre-registration

* **ID**: VT-006
* **Description**: As an authenticated employee, I want to pre-register expected visitors with their details so that they can quickly check in upon arrival.
* **Acceptance criteria**:
  * Employee can create visitor records with name, company, and planned duration
  * Pre-registered visitors have "Planned" status and are associated with the creating employee
  * Employee can view and modify their own pre-registered visitors
  * Pre-registered visitors appear in the visitor selection list for guest users
  * System prevents duplicate pre-registrations for the same visitor and date

### 10.7. Employee visitor overview

* **ID**: VT-007
* **Description**: As an authenticated employee, I want to view current visitors and their status so that I can be aware of who is on the premises.
* **Acceptance criteria**:
  * Employee dashboard displays list of current visitors with status information
  * List shows visitor name, company, status, and arrival time (if applicable)
  * Real-time updates when visitor status changes
  * Employee can filter or search visitor list
  * Employee can view details of visitors they pre-registered

### 10.8. Admin visitor management

* **ID**: VT-008
* **Description**: As a system administrator, I want full access to manage all visitor records including the ability to delete entries so that I can maintain system data integrity.
* **Acceptance criteria**:
  * Admin can view all visitor records regardless of who created them
  * Admin can delete any visitor record with confirmation prompt
  * Admin can modify visitor information and status for any record
  * Deletion operations are logged for audit purposes
  * Admin interface provides bulk operations for data management

### 10.9. Admin dashboard and oversight

* **ID**: VT-009
* **Description**: As a system administrator, I want a comprehensive dashboard showing all visitor activity and system statistics so that I can monitor system usage and security.
* **Acceptance criteria**:
  * Dashboard displays current visitor count and status distribution
  * Admin can view visitor history and search by various criteria
  * System provides visitor statistics and usage metrics
  * Admin can export visitor data for reporting purposes
  * Dashboard updates in real-time as visitor status changes

### 10.10. Role-based access control

* **ID**: VT-010
* **Description**: As a system user, I want access to features appropriate to my role so that system security and data integrity are maintained.
* **Acceptance criteria**:
  * Guest users can only access visitor registration and their own status updates
  * Employees can access pre-registration and view visitor lists
  * Admins have full system access including deletion capabilities
  * Role assignment is based on Entra ID group membership
  * Unauthorized access attempts are blocked with appropriate error messages