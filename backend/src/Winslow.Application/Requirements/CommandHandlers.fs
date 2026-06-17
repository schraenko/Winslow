module Winslow.Application.Requirements.CommandHandlers

open System
open System.Threading.Tasks
open Winslow.Domain.Common.Types
open Winslow.Domain.Common.Errors
open Winslow.Domain.Requirements.Requirement
open Winslow.Domain.Requirements.RequirementEvents
open Winslow.Domain.Requirements.RequirementTypes
open Winslow.Application.Common.Ports
open Winslow.Application.Requirements.Commands
open Winslow.Application.Requirements.RequirementReadStore

// ── Railway-Oriented Computation Expression ───────────────────────────────────

type TaskResultBuilder() =
    member _.Return(x)           = Task.FromResult(Ok x)
    member _.ReturnFrom(x)       = x
    member _.Bind(m, f) : Task<Result<'b, 'e>> =
        task {
            let! r = m
            match r with
            | Ok v    -> return! f v
            | Error e -> return Error e
        }

let taskResult = TaskResultBuilder()

let (>>=) (m : Task<Result<'a, 'e>>) (f : 'a -> Task<Result<'b, 'e>>) =
    task {
        let! r = m
        match r with
        | Ok v    -> return! f v
        | Error e -> return Error e
    }

// ── Handler: Anforderung erstellen ────────────────────────────────────────────

let handleCreate
    (repo       : RequirementRepository)
    (eventStore : EventStore)
    (readStore  : RequirementReadStore)
    (publisher  : EventPublisher)
    (cmd        : CreateRequirementCommand)
    : Task<Result<RequirementId, AppError>> =
    taskResult {
        let input : CreateRequirementInput = {
            ProjectId          = cmd.ProjectId
            Title              = cmd.Title
            Description        = cmd.Description
            Priority           = cmd.Priority
            Kind               = cmd.Kind
            AcceptanceCriteria = cmd.AcceptanceCriteria
            AuthorId           = cmd.AuthorId
        }
        let! req, event =
            create input
            |> Result.mapError Domain
            |> Task.FromResult

        let envelope = {
            AggregateId = requirementId req |> RequirementId.value
            Version     = 1L
            Event       = event
            OccurredAt  = Timestamp.now ()
        }

        let model = projectRequirement req

        do! repo.Save req
        do! eventStore.Append (requirementId req) [ envelope ]
        do! readStore.Upsert (requirementId req) model
        do! task {
            let! _ = publisher.Publish event
            return Ok ()
        }
        return requirementId req
    }

// ── Handler: Status-Übergang ──────────────────────────────────────────────────

let handleTransition
    (repo       : RequirementRepository)
    (eventStore : EventStore)
    (readStore  : RequirementReadStore)
    (publisher  : EventPublisher)
    (cmd        : TransitionStatusCommand)
    : Task<Result<unit, AppError>> =
    taskResult {
        let! req = repo.FindById cmd.RequirementId
        let! updated, event =
            transitionStatus cmd.NewStatus req
            |> Result.mapError Domain
            |> Task.FromResult

        let! existingEvents = eventStore.ReadStream cmd.RequirementId
        let version = (List.length existingEvents |> int64) + 1L

        let envelope = {
            AggregateId = cmd.RequirementId |> RequirementId.value
            Version     = version
            Event       = event
            OccurredAt  = Timestamp.now ()
        }

        let model = projectRequirement updated

        do! repo.Save updated
        do! eventStore.Append cmd.RequirementId [ envelope ]
        do! readStore.Upsert cmd.RequirementId model
        do! task {
            let! _ = publisher.Publish event
            return Ok ()
        }
    }

// ── Handler: Aktualisieren ────────────────────────────────────────────────────

let handleUpdate
    (repo       : RequirementRepository)
    (eventStore : EventStore)
    (readStore  : RequirementReadStore)
    (cmd        : UpdateRequirementCommand)
    : Task<Result<unit, AppError>> =
    taskResult {
        let! req = repo.FindById cmd.RequirementId
        let input : UpdateRequirementInput = {
            Title              = cmd.Title
            Description        = cmd.Description
            Priority           = cmd.Priority
            AcceptanceCriteria = cmd.AcceptanceCriteria
        }
        let! updated =
            update input req
            |> Result.mapError Domain
            |> Task.FromResult

        let! existingEvents = eventStore.ReadStream cmd.RequirementId
        let version = (List.length existingEvents |> int64) + 1L
        let event = RequirementUpdated { RequirementId = cmd.RequirementId; OccurredAt = Timestamp.now () }

        let envelope = {
            AggregateId = cmd.RequirementId |> RequirementId.value
            Version     = version
            Event       = event
            OccurredAt  = Timestamp.now ()
        }

        let model = projectRequirement updated

        do! repo.Save updated
        do! eventStore.Append cmd.RequirementId [ envelope ]
        do! readStore.Upsert cmd.RequirementId model
    }

// ── Handler: Löschen ─────────────────────────────────────────────────────────

let handleDelete
    (repo       : RequirementRepository)
    (eventStore : EventStore)
    (readStore  : RequirementReadStore)
    (publisher  : EventPublisher)
    (cmd        : DeleteRequirementCommand)
    : Task<Result<unit, AppError>> =
    taskResult {
        do! repo.Delete cmd.RequirementId

        let! existingEvents = eventStore.ReadStream cmd.RequirementId
        let version = (List.length existingEvents |> int64) + 1L
        let event = RequirementDeleted (cmd.RequirementId, Timestamp.now ())

        let envelope = {
            AggregateId = cmd.RequirementId |> RequirementId.value
            Version     = version
            Event       = event
            OccurredAt  = Timestamp.now ()
        }

        do! eventStore.Append cmd.RequirementId [ envelope ]
        do! readStore.Delete cmd.RequirementId
        do! task {
            let! _ = publisher.Publish event
            return Ok ()
        }
    }
