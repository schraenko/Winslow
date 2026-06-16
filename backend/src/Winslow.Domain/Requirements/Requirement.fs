module Winslow.Domain.Requirements.Requirement

open Winslow.Domain.Common.Types
open Winslow.Domain.Common.Errors
open Winslow.Domain.Requirements.RequirementTypes
open Winslow.Domain.Requirements.RequirementEvents

// ── Aggregat ──────────────────────────────────────────────────────────────────

type Requirement = {
    Id                 : RequirementId
    ProjectId          : ProjectId
    Title              : RequirementTitle
    Description        : string
    Status             : RequirementStatus
    Priority           : RequirementPriority
    Kind               : RequirementKind
    AcceptanceCriteria : AcceptanceCriteria
    AuthorId           : UserId
    CreatedAt          : Timestamp
    UpdatedAt          : Timestamp
}

// ── Factory ───────────────────────────────────────────────────────────────────

type CreateRequirementInput = {
    ProjectId          : ProjectId
    Title              : string
    Description        : string
    Priority           : RequirementPriority
    Kind               : RequirementKind
    AcceptanceCriteria : string list
    AuthorId           : UserId
}

let create (input: CreateRequirementInput) : Result<Requirement * RequirementEvent, DomainError> =
    result {
        let! title    = RequirementTitle.create input.Title
        let! criteria = AcceptanceCriteria.create input.AcceptanceCriteria
        let now       = Timestamp.now ()
        let req = {
            Id                 = RequirementId.create ()
            ProjectId          = input.ProjectId
            Title              = title
            Description        = input.Description
            Status             = Draft
            Priority           = input.Priority
            Kind               = input.Kind
            AcceptanceCriteria = criteria
            AuthorId           = input.AuthorId
            CreatedAt          = now
            UpdatedAt          = now
        }
        let event = RequirementCreated {
            RequirementId = req.Id
            ProjectId     = req.ProjectId
            Title         = RequirementTitle.value req.Title
            AuthorId      = req.AuthorId
            OccurredAt    = now
        }
        return req, event
    }

// ── Statusübergang ────────────────────────────────────────────────────────────

let transitionStatus
    (newStatus : RequirementStatus)
    (req       : Requirement)
    : Result<Requirement * RequirementEvent, DomainError> =
    result {
        let! _  = RequirementStatus.transition req.Status newStatus
        let now = Timestamp.now ()
        let updated = { req with Status = newStatus; UpdatedAt = now }
        let event = RequirementStatusChanged {
            RequirementId = req.Id
            From          = req.Status
            To            = newStatus
            OccurredAt    = now
        }
        return updated, event
    }

// ── Update ────────────────────────────────────────────────────────────────────

type UpdateRequirementInput = {
    Title              : string option
    Description        : string option
    Priority           : RequirementPriority option
    AcceptanceCriteria : string list option
}

let update
    (input : UpdateRequirementInput)
    (req   : Requirement)
    : Result<Requirement, DomainError> =
    result {
        let! title =
            match input.Title with
            | Some t -> RequirementTitle.create t |> Result.map Some
            | None   -> Ok None

        let! criteria =
            match input.AcceptanceCriteria with
            | Some c -> AcceptanceCriteria.create c |> Result.map Some
            | None   -> Ok None

        let now = Timestamp.now ()
        return {
            req with
                Title              = title              |> Option.defaultValue req.Title
                Description        = input.Description  |> Option.defaultValue req.Description
                Priority           = input.Priority     |> Option.defaultValue req.Priority
                AcceptanceCriteria = criteria           |> Option.defaultValue req.AcceptanceCriteria
                UpdatedAt          = now
        }
    }
