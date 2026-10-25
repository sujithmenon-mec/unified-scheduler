# Unified Vehicle Service Scheduler

A .NET 10 web API that books vehicle service appointments at a dealership. It uses PostgreSQL for persistence, includes unit and integration tests, and provides observability (Serilog, OpenTelemetry, health checks) and request correlation.

## What it does

- Accepts POST `/api/appointments` to create a new appointment.
- Validates input, checks dealership hours, vehicle existence, and service‑bay/technician availability.
- Returns 200 Created with a new appointment.
- Supports GET `/api/appointments/{id}` to retrieve an appointment.
- Provides availability slots via GET `/api/availability`.
- Includes health endpoints: `/health` (liveness) and `/health/ready` (database readiness).
- Logs booking requests, confirmations, and rejections; records metrics (appointments confirmed, conflicts, duration); traces requests and database calls.

## Prerequisites

- .NET 10 SDK installed locally.
- PostgreSQL 16+ with a `scheduler` database and a `scheduler_test` database.
- `dotnet-ef` tool for migrations.

## Setup

1. **Create databases**

   ```bash
   psql -U postgres -d postgres -f db/01_create_role_and_databases.sql
   ```

2. **Configure database connections**

   Configure `ConnectionStrings:SchedulerDb` and `ConnectionStrings:SchedulerTestDb` in your local `ServiceScheduler.Api/appsettings.Development.json` (or equivalent environment variables). Keep local credentials out of source control.

3. **Apply migrations**

   ```bash
   dotnet ef database update --project ServiceScheduler.Api
   ```

4. **Seed development data**

   ```bash
   psql -h localhost -U scheduler -d scheduler -f db/02_seed.sql
   ```

5. **Run the API**

   ```bash
   dotnet run --project ServiceScheduler.Api
   ```

   Swagger UI is available at `http://localhost:5267/swagger`.

6. **Run tests**

   ```bash
   dotnet test
   ```

   Tests require the local PostgreSQL server and `scheduler_test`. The integration-test fixture applies migrations and seed data to that database automatically.

## cURL examples

```bash
# Happy path – create an appointment for Vehicle 1 (Oil Change) at 09:00 UTC
# The example date must be in the future; change it if it has passed.

curl -i -sS -X POST 'http://localhost:5267/api/appointments' \
  -H 'Content-Type: application/json' \
  -d '{"vehicleId":"20000000-0000-0000-0000-000000000001","dealershipId":"10000000-0000-0000-0000-000000000001","serviceTypeId":"30000000-0000-0000-0000-000000000001","startTime":"2026-11-02T09:00:00Z"}'

# Get the created appointment (use the id from the POST response)
curl -i -sS 'http://localhost:5267/api/appointments/{appointment-id}'

# Availability query for Oil Change on 2026-11-02
curl -i -sS 'http://localhost:5267/api/availability?dealershipId=10000000-0000-0000-0000-000000000001&serviceTypeId=30000000-0000-0000-0000-000000000001&date=2026-11-02'

# Health checks
curl -i -sS 'http://localhost:5267/health'
curl -i -sS 'http://localhost:5267/health/ready'

# Correlation ID example
curl -i -sS -H 'X-Correlation-Id: my-request-123' 'http://localhost:5267/api/appointments' -X POST -H 'Content-Type: application/json' -d '{}'
```

## Assumptions

- The dealership operates in UTC (`TimeZoneId = 'UTC'`).
- Working hours are 08:00–17:00 local time.
- Service bays and technicians are seeded with IDs starting at `60000000-...` and `70000000-...`.
- Vehicle IDs `20000000-...` correspond to seeded customers.
- The API uses `IsolationLevel.Serializable` transactions and retries on serialization failures.
- The `BookingService` includes a vehicle‑already‑booked check to prevent overlapping appointments for the same vehicle.
- The `AvailabilityService` returns slots only when both a free bay and a free technician are available.
- The `CorrelationIdMiddleware` reads `X-Correlation-Id` from the request, generates one if missing, and echoes it in the response header.
- The `SchedulerMetrics` records appointments confirmed, booking conflicts (with reason tag), and booking duration.
- The `GlobalExceptionHandler` logs unhandled exceptions and returns a consistent `ProblemDetails` with code `INTERNAL_SERVER_ERROR`.
- The `BookingRules` class extracts working‑hours and start‑in‑past checks for easier testing.
- Integration tests use WebApplicationFactory with the dedicated PostgreSQL database `scheduler_test`. The fixture applies migrations and seed data; each test clears existing appointments before running. The concurrency test also removes the temporary vehicles and appointments it creates.
- The unit tests cover `TimeWindow` overlap logic, constructor validation, working‑hours enforcement, start‑in‑past checks with an injected `TimeProvider`, metrics recording, and correlation‑ID middleware behavior.

## Project structure

```
ServiceScheduler.Api/
  Application/          # BookingService, AvailabilityService, BookingResult, BookingRequest, BookingRules
  Controllers/          # AppointmentsController, AvailabilityController, ApiProblem, AppointmentResponse
  Domain/               # Appointment, AppointmentStatus, Customer, Dealership, ServiceBay, ServiceType, Skill, Technician, TimeWindow, Vehicle
  Infrastructure/       # Configurations, GlobalExceptionHandler, CorrelationIdMiddleware, SchedulerDbContext, SchedulerMetrics
  Program.cs            # Host, services, middleware, OpenTelemetry, Serilog, health checks

ServiceScheduler.Tests/
  UnitTest1.cs         # TimeWindow overlap, constructor, working‑hours, start‑in‑past, metrics, correlation ID
  AppointmentIntegrationTests.cs # Integration tests against scheduler_test
  PostgreSqlTestCollection.cs # Collection fixture for integration tests
  ServiceScheduler.Tests.csproj

db/                     # SQL scripts for role/database creation and seed data
```

## AI Collaboration Narrative

I used AI assistance to accelerate development and ensure high‑quality code:

- **API contract design**: Defined request/response models, validation rules, and error handling patterns.
- **Business logic implementation**: Built the booking service with serializable transactions, availability queries, and conflict detection.
- **Observability setup**: Configured Serilog, OpenTelemetry, metrics, and correlation IDs.
- **Test coverage**: Created unit tests for domain logic and integration tests that exercise the full stack against a local PostgreSQL instance.

My strategy was to guide the AI with clear requirements, verify each output against the original specifications, and iteratively refine the code based on test failures and code‑review feedback. I ensured final quality by:

1. **Running the full test suite** after each major change to catch regressions.
2. **Reviewing generated code** for adherence to project conventions (e.g., record usage, naming, error handling).
3. **Checking observability** (logs, metrics, traces) to confirm they were correctly integrated.
4. **Validating integration behavior** with the seeded database to ensure happy‑path and edge‑case scenarios worked as expected.
