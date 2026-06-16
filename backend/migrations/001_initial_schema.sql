-- Migration: 001_initial_schema.sql

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ── Projekte ──────────────────────────────────────────────────────────────────
CREATE TABLE projects (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name        VARCHAR(255) NOT NULL,
    description TEXT NOT NULL DEFAULT '',
    status      VARCHAR(50)  NOT NULL DEFAULT 'Planning',
    methodology VARCHAR(50)  NOT NULL DEFAULT 'Scrum',
    owner_id    UUID NOT NULL,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

-- ── Anforderungen ─────────────────────────────────────────────────────────────
CREATE TABLE requirements (
    id                   UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    project_id           UUID         NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    title                VARCHAR(500) NOT NULL,
    description          TEXT         NOT NULL DEFAULT '',
    status               VARCHAR(50)  NOT NULL DEFAULT 'Draft',
    priority             VARCHAR(50)  NOT NULL DEFAULT 'ShouldHave',
    kind                 VARCHAR(50)  NOT NULL DEFAULT 'Functional',
    acceptance_criteria  JSONB        NOT NULL DEFAULT '[]',
    author_id            UUID         NOT NULL,
    created_at           TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at           TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_requirements_project  ON requirements(project_id);
CREATE INDEX idx_requirements_status   ON requirements(status);
CREATE INDEX idx_requirements_priority ON requirements(priority);

-- ── Domain Events Log ─────────────────────────────────────────────────────────
CREATE TABLE domain_events (
    id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    aggregate_id UUID        NOT NULL,
    event_type   VARCHAR(100) NOT NULL,
    payload      JSONB        NOT NULL DEFAULT '{}',
    occurred_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_events_aggregate ON domain_events(aggregate_id);
CREATE INDEX idx_events_type      ON domain_events(event_type);

-- ── Demo-Daten ────────────────────────────────────────────────────────────────
INSERT INTO projects (id, name, description, methodology, owner_id)
VALUES (
    '00000000-0000-0000-0000-000000000001',
    'Demo-Projekt',
    'Beispielprojekt für die Winslow',
    'Scrum',
    '00000000-0000-0000-0000-000000000099'
);

INSERT INTO requirements (project_id, title, description, status, priority, kind, acceptance_criteria, author_id)
VALUES
    ('00000000-0000-0000-0000-000000000001',
     'Benutzeranmeldung',
     'Nutzer können sich mit E-Mail und Passwort anmelden.',
     'Approved', 'MustHave', 'Functional',
     '["Login-Formular vorhanden", "Fehlermeldung bei falschen Daten", "JWT wird zurückgegeben"]',
     '00000000-0000-0000-0000-000000000099'),
    ('00000000-0000-0000-0000-000000000001',
     'Anforderungsliste anzeigen',
     'Nutzer sehen alle Anforderungen eines Projekts.',
     'Draft', 'ShouldHave', 'Functional',
     '["Liste ist filterbar nach Status", "Paginierung bei > 50 Einträgen"]',
     '00000000-0000-0000-0000-000000000099');
