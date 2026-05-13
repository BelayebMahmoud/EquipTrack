# EquipTrack

A RESTful Web API for tracking physical equipment (assets) within an organisation. EquipTrack manages assets, employees, and the full lifecycle of asset allocations — including assignment, return, and history — secured with JWT Bearer authentication and role-based access control.

---

## Table of Contents

- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Project Structure](#project-structure)
- [Domain Model](#domain-model)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [API Reference](#api-reference)
- [Authentication & Authorization](#authentication--authorization)
- [Error Handling](#error-handling)
- [Unit Tests](#unit-tests)
- [Postman Collection](#postman-collection)
- [Database Migrations](#database-migrations)

---

## Tech Stack

| Concern | Technology |
|---|---|
| Runtime | .NET 8 |
| Web framework | ASP.NET Core 8 Web API |
| ORM | Entity Framework Core 8 |
| Database | Microsoft SQL Server (Express 2022+) |
| Authentication | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer` 8.0.0) |
| Password hashing | BCrypt.Net-Next 4.0.3 |
| JWT generation | `System.IdentityModel.Tokens.Jwt` 7.5.2 |
| API docs | Swashbuckle / Swagger UI 6.6.2 |
| Testing | xUnit 2.7.0 + Moq 4.20.70 |

---

## Architecture

EquipTrack follows **Clean Architecture** with four projects and a strict inward dependency rule — outer layers depend on inner layers, never the reverse.

```
┌─────────────────────────────────────────────────┐
│              EquipTrack.API                     │
│   Controllers · Program.cs · Middleware         │
│   (depends on Application + Infrastructure)     │
├─────────────────────────────────────────────────┤
│           EquipTrack.Application                │
│   Services · DTOs · Service interfaces          │
│   (depends on Domain only)                      │
├─────────────────────────────────────────────────┤
│          EquipTrack.Infrastructure              │
│   AppDbContext · Repositories · Auth            │
│   (depends on Domain + Application)             │
├─────────────────────────────────────────────────┤
│             EquipTrack.Domain                   │
│   Entities · Enums · Repository interfaces      │
│   (no external dependencies)                    │
└─────────────────────────────────────────────────┘
```

**Key rules enforced by the layer model:**

- `Domain` and `Application` have zero knowledge of EF Core or SQL Server.
- Infrastructure implements Application interfaces (`IPasswordHasher`, `ITokenService`) — this is the correct direction (Infrastructure is the outermost data layer).
- Services return **DTOs**, never raw domain entities.
- All database writes go through `IUnitOfWork.SaveChangesAsync()` — repositories only stage changes via EF change tracking.
- Domain logic (status transitions, business guards) lives in entity methods, not in services or controllers.

---

## Project Structure

```
EquipTrack/
├── EquipTrack.slnx                         # Solution file (VS 2022 17.10+)
├── EquipTrack.postman_collection.json      # Postman collection (24 requests)
│
├── EquipTrack.Domain/
│   ├── Entities/
│   │   ├── Asset.cs                        # Asset entity + Assign() / Return() domain methods
│   │   ├── Employee.cs
│   │   ├── Allocation.cs                   # Join/history record
│   │   ├── User.cs                         # Auth user entity
│   │   └── UserRole.cs                     # Enum: User, Admin
│   └── Interfaces/
│       ├── IAssetRepository.cs
│       ├── IEmployeeRepository.cs
│       ├── IAllocationRepository.cs
│       ├── IUserRepository.cs
│       └── IUnitOfWork.cs
│
├── EquipTrack.Application/
│   ├── DTOs/
│   │   ├── AssetDto.cs                     # AssetDto, CreateAssetDto
│   │   ├── EmployeeDto.cs                  # EmployeeDto, CreateEmployeeDto
│   │   ├── AllocationDto.cs
│   │   └── AuthDto.cs                      # RegisterDto, LoginDto, AuthResponseDto
│   └── Services/
│       ├── IAssetService.cs
│       ├── IEmployeeService.cs
│       ├── IAllocationService.cs
│       ├── IAuthService.cs
│       ├── IPasswordHasher.cs              # BCrypt contract (impl in Infrastructure)
│       ├── ITokenService.cs                # JWT contract (impl in Infrastructure)
│       ├── AssetService.cs
│       ├── EmployeeService.cs
│       ├── AllocationService.cs
│       └── AuthService.cs
│
├── EquipTrack.Infrastructure/
│   ├── Auth/
│   │   ├── JwtSettings.cs                  # Options POCO (bound from appsettings)
│   │   ├── BcryptPasswordHasher.cs         # BCrypt.Net-Next implementation
│   │   └── JwtTokenService.cs              # JWT generation
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   └── UnitOfWork.cs
│   ├── Repositories/
│   │   ├── AssetRepository.cs
│   │   ├── EmployeeRepository.cs
│   │   ├── AllocationRepository.cs
│   │   └── UserRepository.cs
│   └── Migrations/
│       ├── 20260327004642_InitialCreate.cs
│       └── 20260513181135_AddUsersTable.cs
│
├── EquipTrack.API/
│   ├── Controllers/
│   │   ├── AuthController.cs               # POST register / POST login
│   │   ├── AssetsController.cs
│   │   ├── EmployeesController.cs
│   │   └── AllocationsController.cs
│   ├── Middleware/
│   │   └── ExceptionHandlerMiddleware.cs   # Global error → HTTP status mapping
│   ├── Program.cs
│   └── appsettings.json
│
└── EquipTrack.Tests/
    └── Services/
        ├── AssetServiceTests.cs            # 11 tests
        ├── EmployeeServiceTests.cs         # 4 tests
        └── AuthServiceTests.cs             # 4 tests
```

---

## Domain Model

### Asset

```csharp
public enum AssetStatus { Available, InUse, UnderMaintenance, Retired }

public class Asset
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string SerialNumber { get; set; }
    public AssetStatus Status { get; set; }  // default: Available

    public void Assign();   // Available → InUse  (throws if not Available)
    public void Return();   // InUse → Available   (throws if not InUse)
}
```

Status transitions are enforced by domain methods — the service layer calls `Assign()` / `Return()`, never sets `Status` directly.

### Employee

```csharp
public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Department { get; set; }
    public string Email { get; set; }
}
```

### Allocation

Created each time an asset is assigned; `ReturnDate` is set on return.

```csharp
public class Allocation
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public int EmployeeId { get; set; }
    public DateTime AssignedDate { get; set; }
    public DateTime? ReturnDate { get; set; }   // null while active
    public Asset? Asset { get; set; }
    public Employee? Employee { get; set; }
}
```

### User

```csharp
public enum UserRole { User, Admin }

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public UserRole Role { get; set; }   // default: User
}
```

Admins can only be created via the startup seeder — `POST /api/auth/register` always creates a `User`-role account.

### Entity Relationships (EF Core)

```
Asset ──< Allocation >── Employee
```

- One Asset → many Allocations (FK: `AssetId`)
- One Employee → many Allocations (FK: `EmployeeId`)
- `Users` table is standalone (unique index on `Username`)

---

## Prerequisites

| Requirement | Version |
|---|---|
| .NET SDK | 8.0+ |
| SQL Server | 2019+ (Express edition is fine) |
| EF Core CLI tools | 8.0.0 (`dotnet tool install --global dotnet-ef --version 8.0.0`) |
| Visual Studio / Rider / VS Code | Any recent version |

---

## Getting Started

### 1. Clone the repository

```bash
git clone <repository-url>
cd EquipTrack
```

### 2. Configure the connection string

Open `EquipTrack.API/appsettings.json` and update the connection string to match your SQL Server instance:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=EquipTrackDb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;"
}
```

### 3. Apply database migrations

```powershell
dotnet ef database update `
  --project EquipTrack.Infrastructure `
  --startup-project EquipTrack.API
```

This creates the `EquipTrackDb` database with tables: `Assets`, `Employees`, `Allocations`, `Users` (unique index on `Username`).

### 4. Run the API

```powershell
dotnet run --project EquipTrack.API
```

The API starts on:
- HTTPS: `https://localhost:7049`
- HTTP: `http://localhost:5126`

### 5. Open Swagger UI

Navigate to `https://localhost:7049/swagger` (available in Development mode only).

Click the **Authorize** button, paste a JWT token (obtained from `POST /api/auth/login`), and explore all endpoints.

### 6. Default admin account

On first startup, the API seeds an admin user automatically if the `Users` table is empty:

| Field | Value |
|---|---|
| Username | `admin` |
| Password | `Admin@1234` |

> Change the seed credentials in `appsettings.json` under `AdminSeed` before deploying to production.

---

## Configuration

`EquipTrack.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=EquipTrackDb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;"
  },
  "JwtSettings": {
    "Secret": "EquipTrack-Super-Secret-Key-Change-In-Production-2026!",
    "Issuer": "EquipTrack.API",
    "Audience": "EquipTrack.Client",
    "ExpiryMinutes": 60
  },
  "AdminSeed": {
    "Username": "admin",
    "Password": "Admin@1234"
  }
}
```

| Key | Description |
|---|---|
| `JwtSettings:Secret` | HMAC-SHA256 signing key. Must be ≥ 32 characters. **Change before production.** |
| `JwtSettings:ExpiryMinutes` | Token lifetime in minutes (default: 60). `ClockSkew = TimeSpan.Zero` — tokens expire precisely at `ExpiresAt`. |
| `AdminSeed:Username` / `Password` | Credentials for the seeded admin. Only used when `Users` table is empty on startup. |

---

## API Reference

### Authentication

All endpoints except Auth require a valid JWT Bearer token in the `Authorization` header:

```
Authorization: Bearer <token>
```

#### Role matrix

| Controller | GET (read) | POST / mutating |
|---|---|---|
| Auth | Public (`[AllowAnonymous]`) | Public |
| Assets | `[Authorize]` — any role | `[Authorize(Roles = "Admin")]` |
| Employees | `[Authorize]` — any role | `[Authorize(Roles = "Admin")]` |
| Allocations | `[Authorize]` — any role (class-level) | — |

---

### Auth — `POST /api/auth/register`

Creates a new `User`-role account and returns a JWT.

**Request body:**
```json
{
  "username": "john",
  "email": "john@example.com",
  "password": "MyPassword1"
}
```

| Field | Constraint |
|---|---|
| `username` | Required, max 100 chars, must be unique |
| `email` | Required, valid email format, max 256 chars |
| `password` | Required, min 8 chars, max 100 chars |

**Response `200 OK`:**
```json
{
  "token": "eyJhbGci...",
  "expiresAt": "2026-05-13T19:00:00Z"
}
```

**Errors:** `400` — validation failure or username already taken.

---

### Auth — `POST /api/auth/login`

Authenticates a user and returns a JWT.

**Request body:**
```json
{
  "username": "admin",
  "password": "Admin@1234"
}
```

**Response `200 OK`:**
```json
{
  "token": "eyJhbGci...",
  "expiresAt": "2026-05-13T19:00:00Z"
}
```

**Errors:** `401` — invalid username or password (same message for both cases to prevent username enumeration).

---

### Assets — `GET /api/assets`

Returns all assets. Requires any valid JWT.

**Response `200 OK`:**
```json
[
  { "id": 1, "name": "MacBook Pro 16", "serialNumber": "SN-2024-001", "status": "Available" },
  { "id": 2, "name": "Dell Monitor",   "serialNumber": "SN-2024-002", "status": "InUse" }
]
```

---

### Assets — `GET /api/assets/{id}`

Returns a single asset. Requires any valid JWT.

**Response `200 OK`:**
```json
{ "id": 1, "name": "MacBook Pro 16", "serialNumber": "SN-2024-001", "status": "Available" }
```

**Errors:** `404` — asset not found.

---

### Assets — `POST /api/assets`

Creates a new asset with status `Available`. Requires **Admin** role.

**Request body:**
```json
{
  "name": "MacBook Pro 16",
  "serialNumber": "SN-2024-001"
}
```

| Field | Constraint |
|---|---|
| `name` | Required, max 200 chars |
| `serialNumber` | Required, max 100 chars |

**Response `201 Created`:** the new asset `id` (integer).

**Errors:** `400` — validation failure. `403` — non-Admin token.

---

### Assets — `POST /api/assets/{id}/assign/{employeeId}`

Assigns an asset to an employee. Records an `Allocation` with `AssignedDate = UtcNow`. Requires **Admin** role.

**Response `200 OK`:** `"Asset assigned successfully."`

**Errors:**
- `400` — asset is not `Available` (domain guard from `Asset.Assign()`)
- `404` — asset or employee not found
- `403` — non-Admin token

---

### Assets — `POST /api/assets/{id}/return`

Returns an asset. Sets `ReturnDate` on the active allocation and transitions the asset back to `Available`. Requires **Admin** role.

**Response `200 OK`:** `"Asset returned successfully."`

**Errors:**
- `400` — asset is not `InUse`
- `404` — asset or active allocation not found
- `403` — non-Admin token

---

### Employees — `GET /api/employees`

Returns all employees. Requires any valid JWT.

**Response `200 OK`:**
```json
[
  { "id": 1, "name": "Jane Doe", "department": "Engineering", "email": "jane@company.com" }
]
```

---

### Employees — `GET /api/employees/{id}`

Returns a single employee. Requires any valid JWT.

**Errors:** `404` — employee not found.

---

### Employees — `POST /api/employees`

Creates a new employee. Requires **Admin** role.

**Request body:**
```json
{
  "name": "Jane Doe",
  "department": "Engineering",
  "email": "jane@company.com"
}
```

| Field | Constraint |
|---|---|
| `name` | Required, max 200 chars |
| `department` | Required, max 100 chars |
| `email` | Required, valid email format, max 256 chars |

**Response `201 Created`:** the new employee `id` (integer).

**Errors:** `400` — validation failure. `403` — non-Admin token.

---

### Allocations — `GET /api/allocations`

Returns the full allocation history (active and closed). Requires any valid JWT.

**Response `200 OK`:**
```json
[
  {
    "id": 1,
    "assetId": 1,
    "assetName": "MacBook Pro 16",
    "employeeId": 1,
    "employeeName": "Jane Doe",
    "assignedDate": "2026-05-13T18:00:00Z",
    "returnDate": null
  }
]
```

`returnDate` is `null` while the allocation is active.

---

### Allocations — `GET /api/allocations/by-asset/{assetId}`

Returns all allocations for a specific asset. Requires any valid JWT.

---

### Allocations — `GET /api/allocations/by-employee/{employeeId}`

Returns all allocations for a specific employee. Requires any valid JWT.

---

## Authentication & Authorization

### JWT structure

Tokens are signed with HMAC-SHA256. Claims included:

| Claim | Value |
|---|---|
| `sub` | User ID (integer) |
| `unique_name` | Username |
| `email` | Email address |
| `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` | `"User"` or `"Admin"` |
| `jti` | Random GUID (unique per token) |

The `ClaimTypes.Role` claim is required for ASP.NET Core's `[Authorize(Roles = "Admin")]` evaluation.

### Token validation

Validated on every request to a protected endpoint:
- Issuer and Audience match `JwtSettings`
- Signature verifies against the secret key
- Token has not expired (`ClockSkew = TimeSpan.Zero` — no grace period)

### Password hashing

Passwords are hashed with BCrypt (work factor 11, default for BCrypt.Net-Next). Plain-text passwords are never stored or logged.

---

## Error Handling

`ExceptionHandlerMiddleware` wraps the entire pipeline and maps exceptions to HTTP responses:

| Exception | HTTP Status | Response body |
|---|---|---|
| `InvalidOperationException` | `400 Bad Request` | `{ "error": "<message>" }` |
| `UnauthorizedAccessException` | `401 Unauthorized` | `{ "error": "<message>" }` |
| Any other unhandled exception | `500 Internal Server Error` | `{ "error": "An unexpected error occurred." }` |

Domain rule violations (e.g. assigning an already-assigned asset) throw `InvalidOperationException` inside `Asset.Assign()`, which bubbles up through the service layer and is caught here — no try/catch in controllers.

---

## Unit Tests

```powershell
dotnet test EquipTrack.Tests
```

**19 tests total** — all passing.

| Test class | Tests | What is covered |
|---|---|---|
| `AssetServiceTests` | 11 | GetAll, GetById (found/not found), Create, Assign (success/not found/already in use), Return (success/not found/no active allocation) |
| `EmployeeServiceTests` | 4 | GetAll, GetById (found/not found), Create |
| `AuthServiceTests` | 4 | Register (success/duplicate username), Login (correct credentials/wrong password) |

Tests use **Moq** to mock all repository and infrastructure dependencies. No EF Core or database involved — pure unit tests against service logic.

Example — verifying domain invariant via service test:

```csharp
[Fact]
public async Task AssignAssetAsync_WhenAssetAlreadyInUse_ThrowsInvalidOperationException()
{
    var asset = new Asset { Id = 1, Status = AssetStatus.InUse };
    _assetRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(asset);
    _employeeRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Employee { Id = 2 });

    await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AssignAssetAsync(1, 2));
    _uow.Verify(u => u.SaveChangesAsync(), Times.Never);
}
```

---

## Postman Collection

`EquipTrack.postman_collection.json` at the repository root contains **24 pre-configured requests** across 4 folders.

### Import

1. Open Postman → **Import** → select `EquipTrack.postman_collection.json`
2. The collection uses these variables (auto-populated by test scripts):

| Variable | Initial value | Set by |
|---|---|---|
| `baseUrl` | `https://localhost:7049` | Pre-configured |
| `adminToken` | _(empty)_ | Admin Login request |
| `userToken` | _(empty)_ | User Login request |
| `assetId` | `1` | Create Asset request |
| `employeeId` | `1` | Create Employee request |

### Recommended run order

1. **Authentication** → Admin Login (captures `adminToken`)
2. **Authentication** → Register New User
3. **Authentication** → User Login (captures `userToken`)
4. **Employees** → Create Employee (captures `employeeId`)
5. **Assets** → Create Asset (captures `assetId`)
6. **Assets** → Assign Asset to Employee
7. **Allocations** → Get All Allocations
8. **Assets** → Return Asset
9. Run remaining error-case requests (`[400]`, `[401]`, `[403]`, `[404]`)

Every request has Postman test scripts asserting status codes and response field presence.

---

## Database Migrations

### Existing migrations

| Migration | Description |
|---|---|
| `20260327004642_InitialCreate` | Creates `Assets`, `Employees`, `Allocations` tables |
| `20260513181135_AddUsersTable` | Creates `Users` table with unique index on `Username` |

### Add a new migration

```powershell
dotnet ef migrations add <MigrationName> `
  --project EquipTrack.Infrastructure `
  --startup-project EquipTrack.API
```

### Apply migrations

```powershell
dotnet ef database update `
  --project EquipTrack.Infrastructure `
  --startup-project EquipTrack.API
```

Always specify both `--project` and `--startup-project` when running from the solution root — the EF Core tooling is in Infrastructure, the connection string is in the API project.
