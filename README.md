# Winslow Monorepo

Projektsuite für agiles und klassisches Projekt- und Produktmanagement.

## Struktur

```
winslow/
├── backend/        # F# Backend (.NET 8)
│   └── src/
│       ├── Winslow.Domain/         # Domänenmodell, Typen, reine Logik
│       ├── Winslow.Application/    # Use Cases, CQRS, Commands/Queries
│       ├── Winslow.Infrastructure/ # DB, Repos, externe Adapter
│       └── Winslow.API/            # Falco HTTP-Layer, Routing, DTOs
├── frontend/       # Flutter App
│   └── lib/
│       ├── core/                        # Plugin-System, API-Client, Theme
│       └── features/
│           └── requirements/            # Anforderungsmanagement-Modul
└── docs/           # Architekturentscheidungen, API-Spec
```

## Quickstart

### Backend
```bash
cd backend
dotnet restore
dotnet run --project src/Winslow.API
```

### Frontend
```bash
cd frontend
flutter pub get
flutter run
```

## Architektur

- **Backend**: F# mit Falco (HTTP), Dapper (DB), PostgreSQL
- **Frontend**: Flutter mit Riverpod (State), go_router (Navigation)
- **API**: REST + OpenAPI
- **DDD**: Discriminated Unions, Railway-Oriented Programming, CQRS
