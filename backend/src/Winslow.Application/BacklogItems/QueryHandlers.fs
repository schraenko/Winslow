module Winslow.Application.BacklogItems.QueryHandlers

open System.Threading.Tasks
open Winslow.Domain.Common.Types
open Winslow.Domain.Common.Errors
open Winslow.Domain.BacklogItems.BacklogItem
open Winslow.Domain.BacklogItems.BacklogItemTypes
open Winslow.Application.Common.Ports
open Winslow.Application.BacklogItems.Queries

let private toReadModel (item : BacklogItem) : BacklogItemReadModel = {
    Id          = id item |> BacklogItemId.value |> string
    ProjectId   = projectId item |> ProjectId.value |> string
    ParentId    = parentId item |> Option.map (fun pid -> pid |> BacklogItemId.value |> string)
    ItemType    = string (itemType item)
    Title       = title item |> NonEmptyString.value
    Description = description item
    Priority    = priority item
    StoryPoints = storyPoints item
    Effort      = effort item
    Status      = string (status item)
    SprintId    = sprintId item |> Option.map (fun sid -> sid |> SprintId.value |> string)
    Children    = []
}

let handleGetById
    (repo  : BacklogItemRepository)
    (query : GetBacklogItemByIdQuery)
    : Task<Result<BacklogItemReadModel, AppError>> =
    task {
        let! result = repo.FindById query.BacklogItemId
        return result |> Result.map toReadModel
    }

let rec private buildTree (all : BacklogItem list) (parent : BacklogItem) : BacklogItemReadModel =
    let children = all |> List.filter (fun i -> parentId i = Some (id parent))
    let model = toReadModel parent
    { model with Children = children |> List.map (buildTree all) }

let handleGetProductBacklog
    (repo  : BacklogItemRepository)
    (query : GetProductBacklogQuery)
    : Task<Result<BacklogItemReadModel list, AppError>> =
    task {
        let! result = repo.FindByProject query.ProjectId
        match result with
        | Ok items ->
            let withoutSprint = items |> List.filter (fun i -> sprintId i |> Option.isNone)
            let epics = withoutSprint |> List.filter (fun i -> itemType i = Epic)
            return Ok (epics |> List.map (buildTree withoutSprint))
        | Error e -> return Error e
    }

let handleGetSprintBacklog
    (repo  : BacklogItemRepository)
    (query : GetSprintBacklogQuery)
    : Task<Result<BacklogItemReadModel list, AppError>> =
    task {
        let! result = repo.FindBySprint query.SprintId
        match result with
        | Ok items ->
            let topLevel =
                items
                |> List.filter (fun i ->
                    itemType i = PBI || itemType i = Bug || itemType i = Impediment)
            return Ok (topLevel |> List.map (buildTree items))
        | Error e -> return Error e
    }
