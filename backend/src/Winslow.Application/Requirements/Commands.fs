module Winslow.Application.Requirements.Commands

open Winslow.Domain.Common.Types
open Winslow.Domain.Requirements.RequirementTypes

// ── Commands ──────────────────────────────────────────────────────────────────

type CreateRequirementCommand = {
    ProjectId          : ProjectId
    Title              : string
    Description        : string
    Priority           : RequirementPriority
    Kind               : RequirementKind
    AcceptanceCriteria : string list
    AuthorId           : UserId
}

type UpdateRequirementCommand = {
    RequirementId      : RequirementId
    Title              : string option
    Description        : string option
    Priority           : RequirementPriority option
    AcceptanceCriteria : string list option
}

type TransitionStatusCommand = {
    RequirementId : RequirementId
    NewStatus     : RequirementStatus
}

type DeleteRequirementCommand = {
    RequirementId : RequirementId
}
