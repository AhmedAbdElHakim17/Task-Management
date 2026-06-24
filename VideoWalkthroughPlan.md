# Video Walkthrough — Script Outline

Record your screen (IDE + Swagger/Browser). ~10–15 minutes total.

---

## 1. Project Structure (2 min)

**Show:** Solution Explorer in Visual Studio / Rider / VS Code

**Say:** "The project is a 4-layer DDD-inspired architecture:"

| Layer | Purpose |
|---|---|
| `TaskManagement.Domain` | Entities (`User`, `TaskItem`, `RefreshToken`), enums (`Role`, `TaskItemStatus`), domain interfaces. Pure C# — zero NuGet dependencies. |
| `TaskManagement.Application` | Service interfaces + implementations (`ITaskService` / `TaskService`, etc.), DTOs, Request models, `OperationResult<T>`. All business logic lives here. |
| `TaskManagement.Infrastructure` | EF Core `AppDbContext`, repositories, `JwtService`, `RedisCacheService`, in-memory channel queue, `TaskProcessingService` background worker. Implements all Domain interfaces. |
| `TaskManagement.API` | Thin controllers, middleware (`ExceptionMiddleware`), DI wiring in `Program.cs`. Controllers just delegate to services and return HTTP responses. |

**Highlight:** `TaskManagement.Tests` — xUnit tests that cover domain logic, auth service, and the full task service authorization matrix.

---

## 2. Architecture Approach (1.5 min)

**Show:** `TaskService.cs` and `DependencyInjection.cs`

**Say:** "We chose a Service Layer pattern over CQRS/MediatR:"

- **Domain** defines interfaces (e.g. `ITaskRepository`, `ICurrentUserService`).
- **Application** defines service contracts and implements business logic using those interfaces.
- **Infrastructure** provides concrete implementations (EF Core, Redis, JWT).
- **API** registers everything via `DependencyInjection.cs` extension methods.

Key points:
- Controllers are thin — zero business logic.
- `OperationResult<T>` returns success/failure consistently from every service method — no exceptions for expected failures.
- Dependency injection constructor — all services are primary constructors.

---

## 3. Authentication & Authorization Flow (2 min)

**Show:** `AuthController.cs`, then Swagger `POST /api/auth/login`

**Say:** "Authentication uses JWT bearer tokens with refresh token rotation:"

1. **Register** — `POST /api/auth/register` creates a new User-role account. Password is BCrypt-hashed.
2. **Login** — `POST /api/auth/login` validates credentials, returns access token (60min TTL) + refresh token (7 day TTL) + user profile.
3. **Refresh** — `POST /api/auth/refresh` accepts expired access token + valid refresh token → returns new pair. Old refresh token is revoked in the database.
4. **Authorize header** — All protected endpoints require `Authorization: Bearer <token>`.

**Show:** `AdminController.cs` — `[Authorize(Policy = "AdminOnly")]` attribute.

**Say:** "Two roles: `User` and `Admin`. Role-based authorization is enforced at the controller level via policies. The `CurrentUserService` reads the JWT claims to expose `UserId` and `Role` to downstream services."

---

## 4. Seeded Admin User (1 min)

**Show:** `DbSeeder.cs` in the IDE

**Say:** "On first startup, `Program.cs` runs migrations and then calls `DbSeeder.SeedAsync()`. It checks if any admin exists — if not, it creates one:"

```json
{
  "email": "admin@example.com",
  "password": "Admin@123"
}
```

**Show:** Swagger — login with admin credentials, copy token, click Authorize, paste token.

---

## 5. User & Admin APIs (2 min)

**Show:** Swagger — demonstrate each endpoint

**User APIs** (in `AuthController`):
- `POST /api/auth/register` — Register a new user
- `POST /api/auth/login` — Get JWT token
- `POST /api/auth/refresh` — Refresh tokens
- `GET /api/auth/me` — Get own profile

**Admin APIs** (in `AdminController` — requires Admin role):
- `POST /api/admin/users` — Create a user with any role
- `GET /api/admin/users` — List all active users
- `DELETE /api/admin/users/{id}` — Soft-delete a user

**Say:** "Admin endpoints are gated by `[Authorize(Policy = "AdminOnly")]`. Non-admin users receive 403 Forbidden. Note: an admin cannot soft-delete themselves — the delete is a soft-delete, setting `IsDeleted = true`, and a global EF Core query filter excludes deleted users from all queries."

---

## 6. Task APIs (3 min)

**Show:** Swagger — demonstrate with both a regular user and admin JWT tokens

**Endpoints:**
- `POST /api/tasks` — Create (enqueues background processing)
- `GET /api/tasks` — List (scoped by role)
- `GET /api/tasks/{id}` — Get by ID (Redis-cached on second call)
- `PATCH /api/tasks/{id}/status` — Update status

**Demonstrate the authorization matrix:**

| Scenario | Expected result |
|---|---|
| User gets own task | 200 OK |
| User gets another user's task | 403 Forbidden |
| Admin gets a regular user's task | 200 OK |
| Admin gets another admin's task | 403 Forbidden |
| Admin lists all tasks | Sees non-admin tasks only |

**Say** (pointing to `TaskService.cs`): "The `IsAdmin` property checks `currentUserService.Role`, and all three task methods branch on it:"
- `GetAllTasksAsync`: admin gets all users, filters out admin-owned tasks, returns the rest.
- `GetTaskByIdAsync` / `UpdateTaskStatusAsync`: if the task owner is not the current user AND the current user is an admin, we look up the owner's role — if they're also an admin, deny with 403.

---

## 7. Redis Caching (1.5 min)

**Show:** `RedisCacheService.cs` and application logs (console output)

**Say:** "Redis caches `GET /api/tasks/{id}` responses for 10 minutes:"

1. First request: `[CACHE MISS]` — fetches from SQL Server, stores in Redis.
2. Second request: `[CACHE HIT]` — returns from Redis immediately.
3. **Admin edge case**: If cache has another user's task, an admin can't trust the cache alone (no role data in cached DTO). Admin falls through to DB to verify the owner's role before returning.
4. On `PATCH .../status`: cache key is deleted (`[CACHE REMOVED]`) so next read fetches fresh data.

---

## 8. Background Processing (1.5 min)

**Show:** `TaskProcessingService.cs`, `InMemoryTaskQueue.cs`, and console logs

**Say:** "Background processing uses `System.Threading.Channels` — an in-memory producer/consumer queue:"

1. When `POST /api/tasks` is called, `TaskService.CreateTaskAsync()` calls `taskQueue.Enqueue(task.Id)`.
2. `InMemoryTaskQueue` wraps a `Channel<Guid>`.
3. `TaskProcessingService` (a `BackgroundService`) reads from the channel in a loop:
   - Dequeues a task ID.
   - Waits 2 seconds (simulated async work).
   - Fetches the task from DB.
   - If the task was deleted before processing → logs warning, skips.
   - Otherwise transitions status `Pending` → `InProgress`.
4. Exception safety: single DB failure doesn't crash the service — exception is caught and logged.

---

## 9. Business Logic & Testing (1 min)

**Show:** `TaskServiceTests.cs` in the IDE

**Say:** "Beyond standard CRUD, I added business logic:"

- **Duplicate title check**: Creating a task with the same title on the same calendar day for the same user returns `DUPLICATE_TASK_TITLE`.
- **Task sorting**: `GetAllTasksAsync` returns tasks sorted by priority (High > Medium > Low) then by creation date (oldest first).
- **Admin isolation**: admins cannot access other admins' tasks — this prevents privilege escalation between administrators.

"These rules are covered by **29 unit tests** including 20 that specifically validate the admin authorization matrix — cache hits, cache misses, 403 scenarios, cross-user access, and role isolation."

---

## 10. Wrap-up (30 sec)

**Show:** README.md

**Say:** "The README has full setup instructions — Docker Compose for one-command startup, or local setup with SQL Server + Redis. It documents all endpoints, the admin credentials, assumptions, and the architecture. Source code is in the repository along with the Notion test checklist."
