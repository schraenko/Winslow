module Winslow.Application.BacklogItems.CommandHandlers

open System.Threading.Tasks
open Winslow.Domain.Common.Types
open Winslow.Domain.Common.Errors
open Winslow.Domain.BacklogItems.BacklogItem
open Winslow.Domain.BacklogItems.BacklogItemTypes
open Winslow.Application.Common.Ports
open Winslow.Application.BacklogItems.Commands

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

let handleCreate
    (repo  : BacklogItemRepository)
    (cmd   : CreateBacklogItemCommand)
    : Task<Result<BacklogItemId, AppError>> =
    taskResult {
        let! parentType =
            match cmd.ParentId with
            | Some pid ->
                taskResult {
                    let! parent = repo.FindById pid
                    return Some (itemType parent)
                }
            | None -> Task.FromResult(Ok None)

        let input : CreateBacklogItemInput = {
            ProjectId     = cmd.ProjectId
            ParentId      = cmd.ParentId
            ParentType    = parentType
            ItemType      = cmd.ItemType
            Title         = cmd.Title
            Description   = cmd.Description
            Priority      = cmd.Priority
            StoryPoints   = cmd.StoryPoints
            Effort        = cmd.Effort
            AuthorId      = cmd.AuthorId
        }

        let! item, _event =
            create input
            |> Result.mapError Domain
            |> Task.FromResult

        do! repo.Save item
        return id item
    }

let handleTransition
    (repo : BacklogItemRepository)
    (cmd  : TransitionBacklogItemStatusCommand)
    : Task<Result<unit, AppError>> =
    taskResult {
        let! item = repo.FindById cmd.BacklogItemId
        let! updated, _event =
            transitionStatus cmd.NewStatus item
            |> Result.mapError Domain
            |> Task.FromResult
        do! repo.Save updated
    }

let handleAssignToSprint
    (repo : BacklogItemRepository)
    (cmd  : AssignToSprintCommand)
    : Task<Result<unit, AppError>> =
    taskResult {
        let! item = repo.FindById cmd.BacklogItemId
        let! updated, _event =
            assignToSprint cmd.SprintId item
            |> Result.mapError Domain
            |> Task.FromResult
        do! repo.Save updated
    }

let handleRemoveFromSprint
    (repo : BacklogItemRepository)
    (cmd  : RemoveFromSprintCommand)
    : Task<Result<unit, AppError>> =
    taskResult {
        let! item = repo.FindById cmd.BacklogItemId
        let! updated, _event =
            removeFromSprint item
            |> Result.mapError Domain
            |> Task.FromResult
        do! repo.Save updated
    }
