module Winslow.Application.Sprints.CommandHandlers

open System.Threading.Tasks
open Winslow.Domain.Common.Types
open Winslow.Domain.Common.Errors
open Winslow.Domain.Sprints.Sprint
open Winslow.Domain.Sprints.SprintTypes
open Winslow.Application.Common.Ports
open Winslow.Application.Sprints.Commands

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
    (repo : SprintRepository)
    (cmd  : CreateSprintCommand)
    : Task<Result<SprintId, AppError>> =
    taskResult {
        let input : CreateSprintInput = {
            ProjectId = cmd.ProjectId
            Name      = cmd.Name
            Goal      = cmd.Goal
            StartDate = cmd.StartDate
            EndDate   = cmd.EndDate
        }
        let! sprint, _event =
            create input
            |> Result.mapError Domain
            |> Task.FromResult
        do! repo.Save sprint
        return id sprint
    }

let handleTransition
    (repo : SprintRepository)
    (cmd  : TransitionSprintStatusCommand)
    : Task<Result<unit, AppError>> =
    taskResult {
        let! sprint = repo.FindById cmd.SprintId
        let! updated, _event =
            transitionStatus cmd.NewStatus sprint
            |> Result.mapError Domain
            |> Task.FromResult
        do! repo.Save updated
    }
