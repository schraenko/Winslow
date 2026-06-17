module Winslow.Domain.Requirements.Requirement

open Winslow.Domain.Common.Types
open Winslow.Domain.Common.Errors
open Winslow.Domain.Requirements.RequirementTypes
open Winslow.Domain.Requirements.RequirementEvents

// ── Aggregat (DU with private constructor) ───────────────────────────────────

type Requirement = private Requirement of RequirementData

and RequirementData = {
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

// ── Public accessor functions ─────────────────────────────────────────────────

let requirementId      (Requirement data) = data.Id
let projectId          (Requirement data) = data.ProjectId
let title              (Requirement data) = data.Title
let description        (Requirement data) = data.Description
let status             (Requirement data) = data.Status
let priority           (Requirement data) = data.Priority
let kind               (Requirement data) = data.Kind
let acceptanceCriteria (Requirement data) = data.AcceptanceCriteria
let authorId           (Requirement data) = data.AuthorId
let createdAt          (Requirement data) = data.CreatedAt
let updatedAt          (Requirement data) = data.UpdatedAt

/// Hydrate a Requirement from raw values (for repository / seed data only).
/// Does not validate invariants — caller is responsible for ensuring consistency.
let hydrate
    (id                 : RequirementId)
    (projectId          : ProjectId)
    (title              : RequirementTitle)
    (description        : string)
    (status             : RequirementStatus)
    (priority           : RequirementPriority)
    (kind               : RequirementKind)
    (acceptanceCriteria : AcceptanceCriteria)
    (authorId           : UserId)
    (createdAt          : Timestamp)
    (updatedAt          : Timestamp)
    : Requirement =
    Requirement {
        Id                 = id
        ProjectId          = projectId
        Title              = title
        Description        = description
        Status             = status
        Priority           = priority
        Kind               = kind
        AcceptanceCriteria = acceptanceCriteria
        AuthorId           = authorId
        CreatedAt          = createdAt
        UpdatedAt          = updatedAt
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
        let data = {
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
            RequirementId = data.Id
            ProjectId     = data.ProjectId
            Title         = RequirementTitle.value data.Title
            AuthorId      = data.AuthorId
            OccurredAt    = now
        }
        return Requirement data, event
    }

// ── Statusübergang ────────────────────────────────────────────────────────────

let transitionStatus
    (newStatus : RequirementStatus)
    (req       : Requirement)
    : Result<Requirement * RequirementEvent, DomainError> =
    result {
        let (Requirement data) = req
        let! _  = RequirementStatus.transition data.Status newStatus
        let now = Timestamp.now ()
        let updatedData = { data with Status = newStatus; UpdatedAt = now }
        let event = RequirementStatusChanged {
            RequirementId = data.Id
            From          = data.Status
            To            = newStatus
            OccurredAt    = now
        }
        return Requirement updatedData, event
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
        let (Requirement data) = req

        let! title =
            match input.Title with
            | Some t -> RequirementTitle.create t |> Result.map Some
            | None   -> Ok None

        let! criteria =
            match input.AcceptanceCriteria with
            | Some c -> AcceptanceCriteria.create c |> Result.map Some
            | None   -> Ok None

        let now = Timestamp.now ()
        return Requirement {
            data with
                Title              = title              |> Option.defaultValue data.Title
                Description        = input.Description  |> Option.defaultValue data.Description
                Priority           = input.Priority     |> Option.defaultValue data.Priority
                AcceptanceCriteria = criteria           |> Option.defaultValue data.AcceptanceCriteria
                UpdatedAt          = now
        }
    }
