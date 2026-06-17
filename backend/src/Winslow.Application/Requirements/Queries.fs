module Winslow.Application.Requirements.Queries

open Winslow.Domain.Common.Types
open Winslow.Domain.Requirements.RequirementTypes

// ── Queries ───────────────────────────────────────────────────────────────────

type GetRequirementByIdQuery = {
    RequirementId : RequirementId
}

type GetRequirementsByProjectQuery = {
    ProjectId        : ProjectId
    StatusFilter     : RequirementStatus option
    PriorityFilter   : RequirementPriority option
}

// ── Read Models (DTOs für die API) ────────────────────────────────────────────

type RequirementReadModel = {
    Id                 : string
    ProjectId          : string
    Title              : string
    Description        : string
    Status             : string
    Priority           : string
    Kind               : string
    AcceptanceCriteria : string list
    AuthorId           : string
    CreatedAt          : string
    UpdatedAt          : string
}
