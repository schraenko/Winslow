# Winslow Monorepo — Agent Guide

## Structure
- **Backend** (`backend/`): F# .NET 10, DDD with 4 layers — Domain, Application, Infrastructure, API
  - All 4 projects compile and the API is runnable via Falco
  - Solution `Winslow.sln` references all 4 projects
  - Tests dirs exist but are empty — no test projects or runners are set up
- **Frontend** (`frontend/`): Flutter, Riverpod (state), go_router (navigation), Dio (HTTP), plugin-based feature modules

## Commands

### Backend
- Restore: `dotnet restore Winslow.sln` (from `backend/`)
- Build: `dotnet build Winslow.sln`
- Run API: `dotnet run --project src/Winslow.API/Winslow.API.fsproj` (starts on `http://localhost:5000`)
- No test command exists (test project directories are empty placeholders)

### Frontend
- Get deps: `flutter pub get`
- Run app: `flutter run`
- Code generation (freezed, riverpod_generator, json_serializable): `dart run build_runner build` (or `watch`)
- Lint: `flutter analyze` (uses `flutter_lints: ^3.0.0`)
- No test command

### Docker
- `docker compose up` starts PostgreSQL 16 + API (auto-runs `backend/migrations/*.sql`)

## Architecture notes
- Backend uses **Railway-Oriented Programming**: `result { }` CE in Domain (custom `ResultBuilder` in `Common/Types.fs` — F# 10 liefert keinen built-in mehr), custom `TaskResultBuilder` in Application for async handlers
- `IRequirementRepository` and `IEventPublisher` are F# interface types (object expressions) in `Application/Common/Ports.fs`
- API uses **Falco 4.x** web framework with `Falco.Routing.get`/`post`/`patch` and `Falco.Request.getRoute`/`getJsonOptions`
- In-memory repository (`InMemoryRequirementRepository` in Infrastructure) seeded with 2 demo requirements — no PostgreSQL needed
- Response JSON uses camelCase (`System.Text.Json` with `JsonNamingPolicy.CamelCase`)
- Frontend `requirementsRepositoryProvider` throws by default — must be overridden in `main.dart` via `ProviderScope` overrides (done there with `API_BASE_URL` compile-time env var, default `http://localhost:5000`)
- Backend API uses project ID `00000000-0000-0000-0000-000000000001` as demo project
- Seed requirement IDs: `00000000-0000-0000-0000-000000000001` (approved, must-have, functional), `00000000-0000-0000-0000-000000000002` (draft, should-have, functional)

### API Endpoints
- `GET /projects/{projectId}/requirements` — list requirements (returns `RequirementListItem[]`)
- `GET /requirements/{id}` — get single requirement (returns `RequirementReadModel`)
- `POST /requirements` — create requirement (body: `CreateRequirementDto`)
- `PATCH /requirements/{id}/status` — transition status (body: `TransitionStatusDto` with `newStatus`)

### Status Transitions
- Draft → UnderReview, Draft → Rejected
- UnderReview → Approved, UnderReview → Rejected
- Approved → Implemented

## Caveats
- Infrastructure uses in-memory storage (no persistence between restarts)
- Docker compose mounts `backend/migrations/` as init scripts — schema is PostgreSQL-specific (UUID gen, JSONB, pgcrypto)
- No CI, no pre-commit hooks, no task runner config files
