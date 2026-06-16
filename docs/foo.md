mermaid
    graph TD
        subgraph "Frontend (Flutter / Dart)"
            App["Winslow App - main.dart"]
            PS["Plugin System"]
            RR["Requirements Feature"]
            ApiClient["ApiClient - Dio Wrapper"]
        end

        subgraph "Backend (F# / .NET 10)"
            API["Winslow.API - Falco 4.x"]
            APP["Winslow.Application"]
            DOM["Winslow.Domain"]
            INFRA["Winslow.Infrastructure"]
        end

        subgraph "Data Layer"
            PG[("PostgreSQL 16")]
            IM[("InMemory Store - dev mode")]
        end

        App --> ApiClient
        ApiClient -->|HTTP JSON| API
        API --> APP
        APP --> DOM
        APP --> INFRA
        INFRA --> IM
        INFRA -.->|via DSN| PG