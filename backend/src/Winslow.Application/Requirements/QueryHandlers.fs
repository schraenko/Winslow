module Winslow.Application.Requirements.QueryHandlers

open System
open System.Threading.Tasks
open Winslow.Domain.Common.Types
open Winslow.Domain.Common.Errors
open Winslow.Domain.Requirements.Requirement
open Winslow.Domain.Requirements.RequirementTypes
open Winslow.Application.Common.Ports
open Winslow.Application.Requirements.Queries

let private formatTimestamp (ts: Timestamp) =
    (Timestamp.value ts).ToString("O")

// ── Mapping: Aggregat → Read Model ────────────────────────────────────────────

let private toReadModel (req: Requirement) : RequirementReadModel = {
    Id                 = req.Id     |> RequirementId.value |> string
    ProjectId          = req.ProjectId |> ProjectId.value  |> string
    Title              = req.Title  |> RequirementTitle.value
    Description        = req.Description
    Status             = string req.Status
    Priority           = string req.Priority
    Kind               = string req.Kind
    AcceptanceCriteria = req.AcceptanceCriteria |> AcceptanceCriteria.value
    AuthorId           = req.AuthorId |> UserId.value |> string
    CreatedAt          = formatTimestamp req.CreatedAt
    UpdatedAt          = formatTimestamp req.UpdatedAt
}

let private toListItem (req: Requirement) : RequirementListItem = {
    Id                 = req.Id     |> RequirementId.value |> string
    ProjectId          = req.ProjectId |> ProjectId.value  |> string
    Title              = req.Title  |> RequirementTitle.value
    Description        = req.Description
    Status             = string req.Status
    Priority           = string req.Priority
    Kind               = string req.Kind
    AcceptanceCriteria = req.AcceptanceCriteria |> AcceptanceCriteria.value
    AuthorId           = req.AuthorId |> UserId.value |> string
    CreatedAt          = formatTimestamp req.CreatedAt
    UpdatedAt          = formatTimestamp req.UpdatedAt
}

// ── Handler: Einzelne Anforderung ─────────────────────────────────────────────

let handleGetById
    (repo  : IRequirementRepository)
    (query : GetRequirementByIdQuery)
    : Task<Result<RequirementReadModel, AppError>> =
    task {
        let! result = repo.FindById query.RequirementId
        return result |> Result.map toReadModel
    }

// ── Handler: Liste nach Projekt ───────────────────────────────────────────────

let handleGetByProject
    (repo  : IRequirementRepository)
    (query : GetRequirementsByProjectQuery)
    : Task<Result<RequirementListItem list, AppError>> =
    task {
        let! result = repo.FindByProject query.ProjectId
        return result |> Result.map (fun reqs ->
            reqs
            |> List.filter (fun r ->
                query.StatusFilter   |> Option.forall ((=) r.Status)   &&
                query.PriorityFilter |> Option.forall ((=) r.Priority))
            |> List.map toListItem)
    }
