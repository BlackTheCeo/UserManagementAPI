# User Management API (Built with Microsoft Copilot)

A lightweight ASP.NET Core Minimal API for managing user records, developed, debugged, and enhanced with Microsoft Copilot.

## Features & Rubric Alignment

### 1. CRUD Endpoints
- `GET /users` — Retrieves all registered users.
- `GET /users/{id}` — Retrieves a specific user by ID (`404 Not Found` if missing).
- `POST /users` — Creates a new user (`201 Created`).
- `PUT /users/{id}` — Updates an existing user (`200 OK` or `404 Not Found`).
- `DELETE /users/{id}` — Deletes a user (`204 No Content` or `404 Not Found`).

### 2. Input Validation
- Validates that `Name`, `Email`, and `Role` are non-empty, enforces valid email formatting, trims whitespace, and prevents duplicate email registrations (`400 Bad Request` / `409 Conflict`).

### 3. Middleware Pipeline
- **Global Exception-Handling Middleware:** Catches unhandled runtime errors and returns standardized JSON `500` responses.
- **Request/Response Logging Middleware:** Logs HTTP method, route path, status code, and execution duration in milliseconds.
- **Authentication Middleware:** Validates the `X-Api-Key` header on state-changing requests (`POST`, `PUT`, `DELETE`).

### 4. Debugging Improvements Made with Copilot
- Replaced non-thread-safe `List<User>` storage with `ConcurrentDictionary<int, User>` and `Interlocked.Increment` to prevent ID collisions under concurrent requests.
- Added explicit `404 Not Found` checks before updating or deleting records to avoid unhandled exceptions.
- Added global exception middleware at the start of the pipeline so unexpected errors return clean JSON responses instead of stack traces.
