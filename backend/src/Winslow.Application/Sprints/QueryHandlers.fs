module Winslow.Application.Sprints.QueryHandlers

open System.Threading.Tasks
open Winslow.Domain.Common.Types
open Winslow.Domain.Common.Errors
open Winslow.Domain.Sprints.Sprint
open Winslow.Domain.Sprints.SprintTypes
open Winslow.Application.Common.Ports
open Winslow.Application.Sprints.Queries

let private toReadModel (sprint : Sprint) : SprintReadModel = {
    Id        = id sprint |> SprintId.value |> string
    ProjectId = projectId sprint |> ProjectId.value |> string
    Name      = name sprint |> NonEmptyString.value
    Goal      = goal sprint
    StartDate = (startDate sprint).ToString("O")
    EndDate   = (endDate sprint).ToString("O")
    Status    = string (status sprint)
}

let handleGetById
    (repo  : SprintRepository)
    (query : GetSprintByIdQuery)
    : Task<Result<SprintReadModel, AppError>> =
    task {
        let! result = repo.FindById query.SprintId
        return result |> Result.map toReadModel
    }

let handleList
    (repo  : SprintRepository)
    (query : ListSprintsQuery)
    : Task<Result<SprintReadModel list, AppError>> =
    task {
        let! result = repo.FindByProject query.ProjectId
        return result |> Result.map (List.map toReadModel)
    }
