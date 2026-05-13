# EquipTrack — Codebase Reference

## Project Overview

EquipTrack is an ASP.NET Core 8 Web API for tracking physical equipment (assets) within an organisation. The system manages assets, employees, and the allocation of assets to employees. It enforces domain rules (e.g. an asset that is already in use cannot be re-assigned) and records the full allocation history with timestamps.

**Solution file:** `EquipTrack.slnx` (new `.slnx` format, Visual Studio 2022 17.10+)

---

## Architecture: Clean Architecture (4-layer)

The solution is organised into four projects with a strict inward dependency rule — outer layers depend on inner layers, never the reverse.

```
EquipTrack.API              (Presentation — outermost, depends on all layers)
    |
EquipTrack.Application      (Use-cases / application logic, depends on Domain)
    |
EquipTrack.Infrastructure   (EF Core, SQL Server, depends on Domain)
    |
EquipTrack.Domain           (Entities, interfaces — innermost, no dependencies)
```

### Layer responsibilities

| Project | Responsibility |
|---|---|
| `EquipTrack.Domain` | Pure C# entities, enums, and repository/UoW interfaces. No framework dependencies. |
| `EquipTrack.Application` | DTOs, service interfaces, and service implementations that orchestrate domain logic. |
| `EquipTrack.Infrastructure` | EF Core `AppDbContext`, concrete repository implementations, `UnitOfWork`, and migrations. |
| `EquipTrack.API` | ASP.NET Core controllers, DI registration, middleware pipeline (`Program.cs`), Swagger. |

**Key rule:** `EquipTrack.Application` and `EquipTrack.Domain` have zero knowledge of EF Core or SQL Server. Infrastructure concerns are injected via interfaces.

---

## Key Entities and Relationships

### Asset (`EquipTrack.Domain/Entities/Asset.cs`)

```csharp
public enum AssetStatus { Available, InUse, UnderMaintenance, Retired }

public class Asset
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string SerialNumber { get; set; }
    public AssetStatus Status { get; set; }  // default: Available

    public void Assign();  // domain guard: throws InvalidOperationException if not Available
}
```

The `Assign()` method is the only place where the status transition `Available -> InUse` is performed. This is intentional domain logic encapsulation — the Application layer calls it, not the controller.

### Employee (`EquipTrack.Domain/Entities/Employee.cs`)

```csharp
public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Department { get; set; }
    public string Email { get; set; }
}
```

### Allocation (`EquipTrack.Domain/Entities/Allocation.cs`)

The join/history record created each time an asset is assigned.

```csharp
public class Allocation
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public int EmployeeId { get; set; }
    public DateTime AssignedDate { get; set; }   // set to DateTime.UtcNow at assignment
    public DateTime? ReturnDate { get; set; }    // null until the asset is returned

    public Asset? Asset { get; set; }            // navigation
    public Employee? Employee { get; set; }      // navigation
}
```

**Relationships (configured in `AppDbContext.OnModelCreating`):**
- `Allocation` has one `Asset` (FK: `AssetId`), Asset has many Allocations.
- `Allocation` has one `Employee` (FK: `EmployeeId`), Employee has many Allocations.

---

## Patterns in Use

### Repository Pattern

Each aggregate root has an interface in `EquipTrack.Domain/Interfaces/` and a concrete implementation in `EquipTrack.Infrastructure/Repositories/`.

| Interface | Implementation |
|---|---|
| `IAssetRepository` | `AssetRepository` |
| `IEmployeeRepository` | `EmployeeRepository` |
| `IAllocationRepository` | `AllocationRepository` |

Repository interface shape (Asset as example):
```csharp
public interface IAssetRepository
{
    Task<Asset?> GetByIdAsync(int id);
    Task<IEnumerable<Asset>> GetAllAsync();
    Task AddAsync(Asset asset);
    void Update(Asset asset);   // synchronous — EF change tracking, no I/O
}
```

`IAllocationRepository` is currently write-only (`AddAsync` only) — querying allocations is not yet implemented.

### Unit of Work Pattern

`IUnitOfWork` has a single method:
```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync();
}
```

`UnitOfWork` wraps `AppDbContext.SaveChangesAsync()`. Services call repositories to stage changes, then call `IUnitOfWork.SaveChangesAsync()` once to commit the entire transaction. This keeps SaveChanges out of the repositories themselves.

### DTO Mapping (manual)

The Application layer maps between domain entities and DTOs by hand in service methods — no AutoMapper. Keep this consistent when adding new features.

- `AssetDto` / `CreateAssetDto` — in `EquipTrack.Application/DTOs/AssetDto.cs`
- `EmployeeDto` / `CreateEmployeeDto` — in `EquipTrack.Application/DTOs/EmployeeDto.cs`
- `EmployeeDto` deliberately omits `Email` (read model only exposes `Id`, `Name`, `Department`).

---

## Dependency Injection Registration (`Program.cs`)

All registrations use `AddScoped` (per HTTP request lifetime, which is correct for EF Core DbContext):

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IAllocationRepository, AllocationRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
```

Note: there is a commented-out duplicate `AddScoped<IAssetService, AssetService>()` line in `Program.cs` — this is dead code and can be removed.

---

## Database Configuration

**Provider:** Microsoft SQL Server (SQL Server 2022 Express Edition)  
**Connection string** (`appsettings.json`):
```
Server=.\SQLEXPRESS;Database=EquipTrackDb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;
```

**DbContext:** `EquipTrack.Infrastructure/Data/AppDbContext.cs`  
**DbSets:** `Assets`, `Employees`, `Allocations`

---

## Migrations

Migrations live in `EquipTrack.Infrastructure/Migrations/`.

The existing migration `20260327004642_InitialCreate` creates all three tables.

### Adding a new migration

EF Core tooling is in `EquipTrack.Infrastructure`; the startup project is `EquipTrack.API` (it has the connection string). Always specify both when running from the solution root:

```powershell
dotnet ef migrations add <MigrationName> `
  --project EquipTrack.Infrastructure `
  --startup-project EquipTrack.API
```

### Applying migrations to the database

```powershell
dotnet ef database update `
  --project EquipTrack.Infrastructure `
  --startup-project EquipTrack.API
```

---

## Build and Run

```powershell
# Restore and build
dotnet build EquipTrack.slnx

# Run the API
dotnet run --project EquipTrack.API

# Swagger UI (development mode)
# https://localhost:<port>/swagger
```

The Swagger UI is enabled only in the Development environment (`app.Environment.IsDevelopment()`).

---

## API Endpoints

### Assets — `AssetsController` (`/api/assets`)

| Method | Route | Description |
|---|---|---|
| GET | `/api/assets` | Returns all assets as `AssetDto[]` |
| POST | `/api/assets` | Creates a new asset from `CreateAssetDto`; returns `201 Created` with the new `id` |
| POST | `/api/assets/{id}/assign/{employeeId}` | Assigns an asset to an employee; returns `400` if the asset is not `Available` |

Domain exceptions (`InvalidOperationException`) from `Asset.Assign()` are caught in the controller and returned as `400 Bad Request` with the exception message.

### Employees — `EmployeesController` (`/api/employees`)

| Method | Route | Description |
|---|---|---|
| GET | `/api/employees` | Returns all employees as `EmployeeDto[]` |
| POST | `/api/employees` | Creates a new employee from `CreateEmployeeDto`; returns `201 Created` with the new `id` |

---

## Coding Conventions

- **File-scoped namespaces** throughout (`namespace Foo.Bar;` not `namespace Foo.Bar { }`).
- **Nullable reference types** enabled (`<Nullable>enable</Nullable>`) — use `?` annotations and null-guard where needed.
- **Implicit usings** enabled — common BCL namespaces are available without explicit `using` statements.
- **`string.Empty` default** — all string properties initialise to `string.Empty`, not `null`.
- **`async`/`await` throughout** — all repository and service methods that touch the database are async. `IUnitOfWork.SaveChangesAsync()` is the only place where changes are committed.
- **Expression-bodied members** used in repositories for simple pass-through methods.
- **Domain logic stays in entities** — status transitions live on `Asset`, not in services or controllers.
- **Services return DTOs, not entities** — the API layer never directly receives a domain entity from a service.
- **`Update()` is synchronous** — EF Core change tracking does not require I/O; `IAssetRepository.Update()` returns `void`.

---

## What Is Implemented

- [x] Domain entities: `Asset`, `Employee`, `Allocation`
- [x] Repository interfaces (`IAssetRepository`, `IEmployeeRepository`, `IAllocationRepository`)
- [x] `IUnitOfWork` interface and `UnitOfWork` implementation
- [x] `AssetService`: list all, get by id, create, assign to employee, return asset
- [x] `EmployeeService`: list all, get by id, create
- [x] `AllocationService`: list all, filter by asset, filter by employee
- [x] `AssetsController`: GET all, GET by id, POST create, POST assign, POST return
- [x] `EmployeesController`: GET all, GET by id, POST create
- [x] `AllocationsController`: GET all, GET by asset id, GET by employee id
- [x] Input validation — `[Required]`, `[MaxLength]`, `[EmailAddress]` on all create DTOs; `ModelState.IsValid` checked in all POST endpoints
- [x] `Asset.Return()` domain method — `InUse → Available` with guard
- [x] `Asset.Assign()` domain method — `Available → InUse` with guard
- [x] `IAllocationRepository` full CRUD — `AddAsync`, `GetAllAsync`, `GetByAssetIdAsync`, `GetByEmployeeIdAsync`, `GetActiveByAssetIdAsync`
- [x] Global exception handler middleware (`ExceptionHandlerMiddleware`) — maps `InvalidOperationException` → 400, unhandled → 500
- [x] `EmployeeDto` exposes `Email` field
- [x] EF Core SQL Server setup with `AppDbContext`
- [x] Initial migration (`InitialCreate`)
- [x] Swagger / OpenAPI in development
- [x] Unit tests — `EquipTrack.Tests` (xUnit + Moq): 19 tests covering `AssetService`, `EmployeeService`, and `AuthService`
- [x] JWT authentication — `POST /api/auth/register` and `POST /api/auth/login`
- [x] Role-based authorization — `Admin` and `User` roles via `[Authorize(Roles = "Admin")]`
- [x] `User` entity and `UserRole` enum (`User`, `Admin`) in Domain
- [x] `IUserRepository` with `GetByUsernameAsync` and `AnyAsync`
- [x] `IPasswordHasher` and `ITokenService` interfaces in Application/Services
- [x] `BcryptPasswordHasher` (BCrypt.Net-Next) and `JwtTokenService` in Infrastructure/Auth
- [x] `JwtSettings` POCO with Options binding (`Secret`, `Issuer`, `Audience`, `ExpiryMinutes`)
- [x] Admin seeding on first startup from `AdminSeed` config section (`admin` / `Admin@1234`)
- [x] `ExceptionHandlerMiddleware` extended: `UnauthorizedAccessException` → 401
- [x] Swagger configured with JWT Bearer security definition ("Authorize" button)
- [x] `AddUsersTable` EF Core migration (unique index on `Username`)

## What Is Not Yet Implemented

- [ ] **`DELETE` / `PUT` endpoints** for assets and employees
- [ ] **`AssetStatus` transitions** to `UnderMaintenance` or `Retired`
- [ ] **Integration tests** — no test project with real database yet
- [ ] **Pagination** on list endpoints
