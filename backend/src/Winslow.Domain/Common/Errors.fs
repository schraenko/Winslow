module Winslow.Domain.Common.Errors

// ── Domain-Fehler ─────────────────────────────────────────────────────────────

type DomainError =
    | NotFound          of entity: string * id: string
    | ValidationError   of field: string * message: string
    | InvalidTransition of from: string * ``to``: string
    | Unauthorized      of message: string
    | Conflict          of message: string

type AppError =
    | Domain       of DomainError
    | Persistence  of message: string
    | External     of service: string * message: string

