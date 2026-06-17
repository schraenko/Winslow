module Winslow.Domain.Sprints.Sprint

open System
open Winslow.Domain.Common.Types
open Winslow.Domain.Common.Errors
open Winslow.Domain.Sprints.SprintTypes
open Winslow.Domain.Sprints.SprintEvents

// ── Aggregat ──────────────────────────────────────────────────────────────────

type Sprint = private Sprint of SprintData

and SprintData = {
    Id         : SprintId
    ProjectId  : ProjectId
    Name       : NonEmptyString
    Goal       : string
    StartDate  : DateTime
    EndDate    : DateTime
    Status     : SprintStatus
    CreatedAt  : Timestamp
    UpdatedAt  : Timestamp
}

// ── Accessor functions ────────────────────────────────────────────────────────

let id        (Sprint data) = data.Id
let projectId (Sprint data) = data.ProjectId
let name      (Sprint data) = data.Name
let goal      (Sprint data) = data.Goal
let startDate (Sprint data) = data.StartDate
let endDate   (Sprint data) = data.EndDate
let status    (Sprint data) = data.Status
let createdAt (Sprint data) = data.CreatedAt
let updatedAt (Sprint data) = data.UpdatedAt

let hydrate
    (id        : SprintId)
    (projectId : ProjectId)
    (name      : NonEmptyString)
    (goal      : string)
    (startDate : DateTime)
    (endDate   : DateTime)
    (status    : SprintStatus)
    (createdAt : Timestamp)
    (updatedAt : Timestamp)
    : Sprint =
    Sprint {
        Id         = id
        ProjectId  = projectId
        Name       = name
        Goal       = goal
        StartDate  = startDate
        EndDate    = endDate
        Status     = status
        CreatedAt  = createdAt
        UpdatedAt  = updatedAt
    }

// ── Factory ───────────────────────────────────────────────────────────────────

type CreateSprintInput = {
    ProjectId : ProjectId
    Name      : string
    Goal      : string
    StartDate : DateTime
    EndDate   : DateTime
}

let create (input : CreateSprintInput) : Result<Sprint * SprintEvent, DomainError> =
    result {
        let! name = NonEmptyString.create "Name" input.Name

        if input.StartDate >= input.EndDate then
            return! Error (ValidationError ("EndDate", "End date must be after start date"))
        else
            let now = Timestamp.now ()
            let data = {
                Id         = SprintId.create ()
                ProjectId  = input.ProjectId
                Name       = name
                Goal       = input.Goal
                StartDate  = input.StartDate
                EndDate    = input.EndDate
                Status     = Planned
                CreatedAt  = now
                UpdatedAt  = now
            }
            let event = SprintCreated {
                SprintId   = data.Id
                ProjectId  = data.ProjectId
                Name       = NonEmptyString.value data.Name
                Goal       = data.Goal
                StartDate  = data.StartDate
                EndDate    = data.EndDate
                OccurredAt = now
            }
            return Sprint data, event
    }

// ── State machine ─────────────────────────────────────────────────────────────

let transitionStatus
    (newStatus : SprintStatus)
    (sprint    : Sprint)
    : Result<Sprint * SprintEvent, DomainError> =
    let (Sprint data) = sprint
    result {
        let! _ = SprintStatus.transition data.Status newStatus
        let now = Timestamp.now ()
        let updatedData = { data with Status = newStatus; UpdatedAt = now }
        let event = SprintStatusChanged {
            SprintId   = data.Id
            From       = string data.Status
            To         = string newStatus
            OccurredAt = now
        }
        return Sprint updatedData, event
    }
