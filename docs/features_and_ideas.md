# Winslow — Features and Ideas

## Planned Features

### P-1: Ideation Plugin

A plugin for brainstorming, collecting, and voting on feature ideas before
they become formal requirements.

- Registered in `frontend/lib/main.dart:19` (currently commented out)
- Referenced in `docs/architecture.md:197` as `IdeationPlugin (planned)`

### P-2: Project Management Plugin

A plugin for managing epics, sprints, milestones, and project methodology
(agile/waterfall).

- Registered in `frontend/lib/main.dart:20` (currently commented out)
- Referenced in `docs/architecture.md:198` as `ProjectManagementPlugin (planned)`

### P-3: PostgreSQL Repository Adapter

Replace the in-memory repository with a real PostgreSQL implementation.

- Docker Compose (`docker-compose.yml`) and migrations (`backend/migrations/001_initial_schema.sql`)
  are ready and tested
- Database schema includes tables for `projects`, `requirements`, and `domain_events`
- Requires implementing `IRequirementRepository` against Npgsql
- Referenced in `docs/architecture.md:406`

### P-4: JWT Authentication

Add user authentication with JWT tokens.

- TODO at `frontend/lib/core/api/api_client.dart:25`:
  `// TODO: JWT-Token aus SecureStorage lesen`
- TODO at `frontend/lib/core/api/api_client.dart:34`:
  `// TODO: Token-Refresh-Flow`
- Currently no auth middleware on the API side

---

## Improvements & Technical Debt

### I-1: Backend Test Suite

Set up test projects and write tests.

- `backend/tests/Winslow.Application.Tests/` — empty directory
- `backend/tests/Winslow.Domain.Tests/` — empty directory
- No test runner or CI pipeline configured

### I-2: Frontend Feature Tests

Expand beyond the single smoke test.

- `frontend/test/widget_test.dart` — only checks that the app renders
- No unit tests for notifiers, repositories, or domain models

### I-3: Freezed / Code Generation

Adopt code generation for models and JSON serialization.

- Referenced in `docs/architecture.md:543`:
  "toolchain is ready for future code-gen adoption"
- Dependencies `freezed`, `freezed_annotation`, `json_serializable`,
  `json_annotation` are already in `pubspec.yaml:29,38-40`

### I-4: Create Requirement UI

Implement the actual form/modal for creating requirements.

- TODO at `frontend/lib/features/requirements/presentation/pages/requirements_page.dart:81`:
  `// TODO: CreateRequirementSheet`
- Currently shows a "coming soon" snackbar

### I-5: Frontend Tests

Add unit and widget tests for the requirements feature.
Referenced in `AGENTS.md:23`: "No test command"

### I-6: Backend Tests

Add unit tests for domain logic and application handlers.
Referenced in `AGENTS.md:16`: "Test project directories are empty placeholders"

### I-7: CI Pipeline

Set up continuous integration.

- Referenced in `AGENTS.md:52`: "No CI, no pre-commit hooks, no task runner config files"

### I-8: Android Release Configuration

Configure production signing for Android builds.

- TODO at `frontend/android/app/build.gradle.kts:23`: unique Application ID
- TODO at `frontend/android/app/build.gradle.kts:35`: release signing config

---

## Open Questions

- Should the API support batch status transitions?
- Should requirements support file attachments?
- Should there be a notification system for status changes?
- Should the frontend support offline mode with local caching?
