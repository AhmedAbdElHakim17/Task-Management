# Task Management API

A RESTful Task Management API built with **.NET 8**, **ASP.NET Core**, **EF Core**, **SQL Server**, **Redis**, and **JWT** authentication. The architecture follows a **4-layer DDD-inspired design** with a **Service Layer** pattern.

---

## Table of Contents

- [Prerequisites](#prerequisites)
- [Quick Start with Docker](#quick-start-with-docker)
- [Local Setup (Without Docker)](#local-setup-without-docker)
- [Default Admin Credentials](#default-admin-credentials)
- [Architecture Overview](#architecture-overview)
- [API Endpoints](#api-endpoints)
- [Authorization Rules](#authorization-rules)
- [Caching](#caching)
- [Background Processing](#background-processing)
- [Project Structure](#project-structure)
- [Assumptions Made](#assumptions-made)

---

## Prerequisites

| Tool | Version | Purpose |
|---|---|---|
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | ≥ 24.x | Run all services containerised |
| [Docker Compose](https://docs.docker.com/compose/) | ≥ 2.x (bundled with Docker Desktop) | Orchestrate multi-container setup |
| [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | 8.0.x | Required for local development only |
| SQL Server 2022 | — | Required for local development only |
| Redis | ≥ 7.x | Required for local development only |

---

## Quick Start with Docker

This is the **recommended** way to run the full stack with a single command.

```bash
# 1. Clone the repository (if you haven't already)
git clone <your-repo-url>
cd TaskManagement

# 2. Build and start all services (SQL Server, Redis, API)
docker-compose up --build

# 3. Open Swagger UI
#    Navigate to: http://localhost:5000/swagger
```

> **What happens on first run:**
> - SQL Server and Redis containers start and become healthy.
> - The API container starts, runs EF Core migrations automatically, and seeds the default admin user.
> - Swagger UI is available at `http://localhost:5000/swagger`.

To stop all services:

```bash
docker-compose down
```

To stop and **remove all data volumes** (clean slate):

```bash
docker-compose down -v
```

---

## Local Setup (Without Docker)

Use this approach if you want to run the API directly on your machine.

### 1. Install Dependencies

- **SQL Server** — Install [SQL Server 2022 Developer Edition](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) or use an existing instance.
- **Redis** — Install [Redis for Windows](https://github.com/microsoftarchive/redis/releases) or run via WSL/Docker (`docker run -p 6379:6379 redis:alpine`).

### 2. Configure Connection Strings

Edit `TaskManagement.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=TaskManagementDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Jwt": {
    "Key": "your-256-bit-secret-key-here-make-it-long",
    "Issuer": "TaskManagement",
    "Audience": "TaskManagementUsers",
    "ExpiryMinutes": 60
  }
}
```

### 3. Apply EF Core Migrations

Run from the **solution root**:

```bash
dotnet ef database update \
  --project TaskManagement.Infrastructure \
  --startup-project TaskManagement.API
```

### 4. Run the API

```bash
dotnet run --project TaskManagement.API
```

The API will be available at `https://localhost:7xxx` / `http://localhost:5xxx` (port shown in terminal).  
Swagger UI: `https://localhost:<port>/swagger`

> **Note:** The first run also seeds the default admin user automatically via `DbSeeder`.

---

## Default Admin Credentials

A default admin account is seeded automatically on first startup.

| Field    | Value               |
|----------|---------------------|
| Email    | `admin@example.com` |
| Password | `Admin@123`         |

Use these credentials with the `POST /api/auth/login` endpoint to obtain a JWT token, then click **Authorize** in Swagger to authenticate subsequent requests.

---

## Architecture Overview

The solution follows a **4-layer DDD-inspired** architecture:

```
TaskManagement.sln
├── TaskManagement.Domain          # Entities, enums, domain interfaces — zero framework dependencies
├── TaskManagement.Application     # Service interfaces, DTOs, request models, business logic — references Domain only
├── TaskManagement.Infrastructure  # EF Core, SQL Server, Redis, JWT services — references Domain + Application
└── TaskManagement.API             # ASP.NET Core controllers, middleware, DI wiring — references all layers
```

**Key design decisions:**

- **Domain layer** is pure C# with no NuGet dependencies, making it fully portable and testable in isolation.
- **Application layer** owns all business logic in `Service` classes (`IAuthService`, `IUserService`, `ITaskService`). Each service depends on Domain interfaces and uses DTOs/Request models for data transfer.
- **Infrastructure layer** implements all domain interfaces (repositories via EF Core, caching via StackExchange.Redis, JWT handling, background queue via `System.Threading.Channels`).
- **API layer** contains thin controllers that receive requests, delegate to Application services, and return HTTP responses — no business logic in controllers.
- **JWT authentication** with role-based authorization (`Admin` / `User` roles) is applied at the API boundary via the `[Authorize]` attribute.
- **Redis** is used for response caching of frequently read task data with a configurable TTL.
- **Background processing** uses an in-memory `Channel<Guid>` queue and a `BackgroundService` that transitions tasks from `Pending` to `InProgress` after a simulated 2-second delay.
- **Soft-delete** pattern is used on the `User` entity via a global EF Core query filter.
- **Refresh token rotation** — each time a token pair is refreshed, the old refresh token is revoked and a new one issued.

---

## API Endpoints

### Auth — `POST /api/auth/register`
Register a new account with `User` role.

### Auth — `POST /api/auth/login`
Authenticate and receive JWT + refresh token.

### Auth — `POST /api/auth/refresh`
Refresh expired JWT using a valid refresh token (token rotation).

### Auth — `GET /api/auth/me`
Get profile of the currently authenticated user.

### Tasks — `POST /api/tasks`
Create a new task. Task is enqueued for background processing.

### Tasks — `GET /api/tasks`
List all accessible tasks (scoped by role — see authorization rules).

### Tasks — `GET /api/tasks/{id}`
Get a single task by ID (scoped by role). Second request returns from Redis cache.

### Tasks — `PATCH /api/tasks/{id}/status`
Update the status of a task (`Pending` / `InProgress` / `Done`). Invalidates Redis cache.

### Admin — `POST /api/admin/users`
Create a user with any role (Admin only).

### Admin — `GET /api/admin/users`
List all active users (Admin only).

### Admin — `DELETE /api/admin/users/{id}`
Soft-delete a user (Admin only).

---

## Authorization Rules

| Role | Can do |
|---|---|
| **User** | Register, login, refresh tokens. CRUD on **own tasks only**. View own profile. |
| **Admin** | All User permissions. Task CRUD on **any non-admin user's tasks**. Admins **cannot** access or modify another admin's tasks. Full user management (create, list, soft-delete users). |

### Task access matrix

| Action | User → own task | User → other's task | Admin → user's task | Admin → admin's task |
|---|---|---|---|---|
| GET /api/tasks | 200 OK | 403 Forbidden | 200 OK | 403 Forbidden |
| GET /api/tasks/{id} | 200 OK | 403 Forbidden | 200 OK | 403 Forbidden |
| PATCH .../status | 204 No Content | 403 Forbidden | 204 No Content | 403 Forbidden |

---

## Caching

- **Redis** caches the result of `GET /api/tasks/{id}` for 10 minutes (configurable via `Redis:ExpiryMinutes`).
- On cache hit where the cached task belongs to the requesting user, the response is served directly from Redis with a `[CACHE HIT]` log entry.
- On cache hit where the cached task belongs to another user:
  - A regular user receives **403 Forbidden** immediately.
  - An admin falls through to the database to verify the task owner's role (cannot trust cache alone for cross-user access).
- On cache miss, the task is fetched from SQL Server and then stored in Redis.
- On status update (`PATCH .../status`), the cache key for that task is removed to ensure fresh data on the next read.

---

## Background Processing

- When a task is created via `POST /api/tasks`, its `Id` is enqueued into an in-memory `Channel<Guid>`.
- A `BackgroundService` (`TaskProcessingService`) continuously reads from the channel.
- For each task ID:
  1. Waits 2 seconds (simulating async work).
  2. Fetches the task from the database.
  3. If the task was deleted before processing, logs a warning and skips.
  4. Otherwise, transitions its status from `Pending` to `InProgress` and saves.
- Exceptions are caught and logged — a single failed task does not crash the service.

---

## Project Structure

```
TaskManagement/
├── TaskManagement.sln
├── docker-compose.yml
├── README.md
├── TaskManagement.Domain/
│   ├── Entities/           # User, TaskItem, RefreshToken
│   ├── Enums/              # Role, TaskItemStatus, TaskItemPriority
│   ├── Interfaces/         # IUserRepository, ITaskRepository, ICurrentUserService, ...
│   └── Constants/          # CONST_MESSAGE_CODES, API_STATUS_CODES
├── TaskManagement.Application/
│   ├── Services/           # ITaskService, IUserService, IAuthService + implementations
│   ├── DTOs/               # TaskDto, UserDto, AuthResponseDto, AuthResultDto
│   ├── Requests/           # CreateTaskRequest, UpdateTaskStatusRequest, RegisterRequest, ...
│   ├── Results/            # OperationResult<T> (success/failure wrapper)
│   ├── Mappings/           # EntityMapper (entity ↔ DTO mapping)
│   ├── Options/            # JwtOptions, RefreshTokenOptions, RedisOptions
│   └── Interfaces/         # ICacheService, ITaskQueue, IJwtService
├── TaskManagement.Infrastructure/
│   ├── Persistence/
│   │   ├── AppDbContext.cs
│   │   ├── Repositories/   # UserRepository, TaskRepository, RefreshTokenRepository
│   │   ├── Migrations/     # EF Core migrations
│   │   └── Seeding/        # DbSeeder (creates default admin)
│   ├── Identity/           # JwtService
│   ├── Caching/            # RedisCacheService
│   ├── Queue/              # InMemoryTaskQueue
│   ├── BackgroundServices/ # TaskProcessingService
│   └── DependencyInjection.cs
├── TaskManagement.API/
│   ├── Controllers/        # AuthController, TasksController, AdminController
│   ├── Middleware/          # ExceptionMiddleware
│   ├── Extensions/         # Service registration extensions
│   ├── Program.cs
│   └── appsettings.json
└── TaskManagement.Tests/
    ├── AuthServiceTests.cs
    ├── TaskServiceTests.cs
    ├── DomainEntityTests.cs
    └── TaskBusinessLogicTests.cs
```

---

## Assumptions Made

1. **Single-tenant** — The system does not support multi-tenancy; all data is scoped per user via `UserId` on task records.
2. **Password hashing** — Passwords are stored as BCrypt hashes. The plain-text admin password `Admin@123` is only used for the initial seed; it is hashed before being persisted.
3. **JWT secret** — The default `Jwt:Key` in `appsettings.json` is a placeholder. **You must replace it** with a cryptographically secure value (≥ 256 bits) before deploying to any environment beyond local development.
4. **SQL Server trust certificate** — `TrustServerCertificate=True` is set in the local connection string for developer convenience. Remove it or configure a proper certificate in production.
5. **No HTTPS in Docker** — The Docker setup exposes port `8080` (HTTP only) inside the container and maps it to host port `5000`. HTTPS termination should be handled by a reverse proxy (e.g., nginx, Traefik) in production.
6. **EF Core migrations** — The API applies pending migrations automatically on startup via `db.Database.MigrateAsync()`. This is intentional for simplicity; in a CI/CD pipeline you may prefer to run migrations as a separate deployment step.
7. **Redis as required dependency** — If Redis is unreachable, the application throws on startup. For a production-grade setup, consider graceful degradation with a circuit breaker.
8. **Task ownership scope** — Regular users can only view and update their own tasks. Admins can view and update any **non-admin** user's tasks, but **cannot access other admins' tasks**.
9. **Background processing is in-memory** — The `Channel<Guid>` queue is not persisted. If the application restarts, any pending tasks in the queue are lost without reprocessing.
