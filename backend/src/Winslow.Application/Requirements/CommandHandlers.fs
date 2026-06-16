module Winslow.Application.Requirements.CommandHandlers

open System.Threading.Tasks
open Winslow.Domain.Common.Types
open Winslow.Domain.Common.Errors
open Winslow.Domain.Requirements.Requirement
open Winslow.Domain.Requirements.RequirementEvents
open Winslow.Application.Common.Ports
open Winslow.Application.Requirements.Commands

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

// ── Handler: Anforderung erstellen ────────────────────────────────────────────

let handleCreate
    (repo      : IRequirementRepository)
    (publisher : IEventPublisher)
    (cmd       : CreateRequirementCommand)
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

        do! repo.Save req
        do! task {
            let! _ = publisher.Publish event
            return Ok ()
        }
        return req.Id
    }

// ── Handler: Status-Übergang ──────────────────────────────────────────────────

let handleTransition
    (repo      : IRequirementRepository)
    (publisher : IEventPublisher)
    (cmd       : TransitionStatusCommand)
    : Task<Result<unit, AppError>> =
    taskResult {
        let! req = repo.FindById cmd.RequirementId
        let! updated, event =
            transitionStatus cmd.NewStatus req
            |> Result.mapError Domain
            |> Task.FromResult
        do! repo.Save updated
        do! task {
            let! _ = publisher.Publish event
            return Ok ()
        }
    }

// ── Handler: Aktualisieren ────────────────────────────────────────────────────

let handleUpdate
    (repo : IRequirementRepository)
    (cmd  : UpdateRequirementCommand)
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
        do! repo.Save updated
    }

// ── Handler: Löschen ─────────────────────────────────────────────────────────

let handleDelete
    (repo      : IRequirementRepository)
    (publisher : IEventPublisher)
    (cmd       : DeleteRequirementCommand)
    : Task<Result<unit, AppError>> =
    taskResult {
        do! repo.Delete cmd.RequirementId
        let event = RequirementDeleted (cmd.RequirementId, Timestamp.now ())
        do! task {
            let! _ = publisher.Publish event
            return Ok ()
        }
    }
