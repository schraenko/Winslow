module Winslow.Domain.Requirements.RequirementEvents

open Winslow.Domain.Common.Types
open Winslow.Domain.Requirements.RequirementTypes

// ── Domain Events ─────────────────────────────────────────────────────────────

type RequirementCreatedData = {
    RequirementId : RequirementId
    ProjectId     : ProjectId
    Title         : string
    AuthorId      : UserId
    OccurredAt    : Timestamp
}

type RequirementStatusChangedData = {
    RequirementId : RequirementId
    From          : RequirementStatus
    To            : RequirementStatus
    OccurredAt    : Timestamp
}

type RequirementUpdatedData = {
    RequirementId : RequirementId
    OccurredAt    : Timestamp
}

type RequirementEvent =
    | RequirementCreated       of RequirementCreatedData
    | RequirementStatusChanged of RequirementStatusChangedData
    | RequirementUpdated       of RequirementUpdatedData
    | RequirementDeleted       of RequirementId * Timestamp

module RequirementEvent =
    let occurredAt = function
        | RequirementCreated e       -> Timestamp.value e.OccurredAt
        | RequirementStatusChanged e -> Timestamp.value e.OccurredAt
        | RequirementUpdated e       -> Timestamp.value e.OccurredAt
        | RequirementDeleted (_, t)  -> Timestamp.value t
