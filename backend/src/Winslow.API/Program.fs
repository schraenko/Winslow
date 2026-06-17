module Winslow.Api.Program

open System
open System.Text.Json
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Falco
open Falco.Routing
open Winslow.Domain.Common.Types
open Winslow.Domain.Common.Errors
open Winslow.Domain.Requirements.Requirement
open Winslow.Domain.Requirements.RequirementTypes
open Winslow.Domain.BacklogItems.BacklogItemTypes
open Winslow.Domain.Sprints.SprintTypes
open Winslow.Application.Common.Ports
open Winslow.Application.Requirements.Commands
open Winslow.Application.Requirements.Queries
open Winslow.Application.Requirements.CommandHandlers
open Winslow.Application.Requirements.QueryHandlers
open Winslow.Application.BacklogItems.Commands
open Winslow.Application.BacklogItems.Queries
open Winslow.Application.Sprints.Commands
open Winslow.Application.Sprints.Queries
open Winslow.Infrastructure

// ── JSON ───────────────────────────────────────────────────────────────────────

let jsonOptions =
    JsonSerializerOptions(
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    )

let ofJson (data : 'a) : HttpHandler =
    fun ctx ->
        task {
            let json = JsonSerializer.Serialize(data, jsonOptions)
            ctx.Response.ContentType <- "application/json; charset=utf-8"
            return! ctx.Response.WriteAsync(json, System.Text.Encoding.UTF8)
        }

let bindJson<'a> (ctx : HttpContext) : Task<'a> =
    Request.getJsonOptions<'a> jsonOptions ctx

// ── Enum-Helfer ────────────────────────────────────────────────────────────────

let parsePriority (s : string) =
    match s.Replace(" ", "").ToLowerInvariant() with
    | "musthave"   -> Ok MustHave
    | "shouldhave" -> Ok ShouldHave
    | "couldhave"  -> Ok CouldHave
    | "wonthave"   -> Ok WontHave
    | _            -> Error $"Ungültige Priorität: {s}"

let parseKind (s : string) =
    match s.Replace(" ", "").ToLowerInvariant() with
    | "functional"    -> Ok Functional
    | "nonfunctional" -> Ok NonFunctional
    | "constraint"    -> Ok Constraint
    | "businessrule"  -> Ok BusinessRule
    | _               -> Error $"Ungültige Art: {s}"

let parseStatus (s : string) =
    match s.Replace(" ", "").ToLowerInvariant() with
    | "draft"        -> Ok Draft
    | "underreview"  -> Ok UnderReview
    | "approved"     -> Ok Approved
    | "rejected"     -> Ok Rejected
    | "implemented"  -> Ok Implemented
    | _              -> Error $"Ungültiger Status: {s}"

let parseBacklogItemType (s : string) =
    match s.Replace(" ", "").ToLowerInvariant() with
    | "epic"       -> Ok Epic
    | "feature"    -> Ok Feature
    | "pbi"        -> Ok PBI
    | "task"       -> Ok Task
    | "bug"        -> Ok Bug
    | "impediment" -> Ok Impediment
    | _            -> Error $"Ungültiger BacklogItem-Typ: {s}"

let parseBacklogItemStatus (s : string) =
    match s.Replace(" ", "").ToLowerInvariant() with
    | "open"       -> Ok Open
    | "inprogress" | "in_progress" -> Ok InProgress
    | "done"       -> Ok Done
    | "cancelled"  -> Ok Cancelled
    | _            -> Error $"Ungültiger BacklogItem-Status: {s}"

let parseSprintStatus (s : string) =
    match s.Replace(" ", "").ToLowerInvariant() with
    | "planned"  -> Ok Planned
    | "active"   -> Ok Active
    | "completed" -> Ok Completed
    | _          -> Error $"Ungültiger Sprint-Status: {s}"

// ── DTOs ───────────────────────────────────────────────────────────────────────

type CreateRequirementDto = {
    ProjectId          : string
    Title              : string
    Description        : string
    Priority           : string
    Kind               : string
    AcceptanceCriteria : string list
}

type TransitionStatusDto = { NewStatus : string }

// ── BacklogItem DTOs ────────────────────────────────────────────────────────────

type CreateBacklogItemDto = {
    ProjectId   : string
    ParentId    : string option
    ItemType    : string
    Title       : string
    Description : string
    Priority    : int
    StoryPoints : int option
    Effort      : float option
}

type AssignToSprintDto = {
    SprintId : string
}

// ── Sprint DTOs ─────────────────────────────────────────────────────────────────

type CreateSprintDto = {
    ProjectId : string
    Name      : string
    Goal      : string
    StartDate : string
    EndDate   : string
}

type TransitionSprintStatusDto = { NewStatus : string }

// ── Error-Handler ──────────────────────────────────────────────────────────────

let private handleError (appError : AppError) : HttpHandler =
    fun ctx ->
        task {
            let status, message =
                match appError with
                | Domain (NotFound (entity, id)) ->
                    404, $"{entity} {id} nicht gefunden"
                | Domain (ValidationError (field, msg)) ->
                    400, $"{field}: {msg}"
                | Domain (InvalidTransition (fromSt, toSt)) ->
                    400, $"Ungültiger Übergang von {fromSt} nach {toSt}"
                | _ -> 500, "Interner Serverfehler"

            ctx.Response.StatusCode <- status
            return! ofJson {| error = message |} ctx
        }

// ── Abhängigkeiten ─────────────────────────────────────────────────────────────

let repo = InMemoryRequirementRepository.create ()
let eventStore = InMemoryEventStore.create ()
let readStore = InMemoryRequirementReadStore.create ()
let publisher = InMemoryEventPublisher.create ()
let backlogItemRepo = InMemoryBacklogItemRepository.create ()
let sprintRepo = InMemorySprintRepository.create ()

// ── Aliases (vermeiden Namespace-Konflikte) ─────────────────────────────────────

module BliCmd = Winslow.Application.BacklogItems.CommandHandlers
module BliQry = Winslow.Application.BacklogItems.QueryHandlers
module SpCmd = Winslow.Application.Sprints.CommandHandlers
module SpQry = Winslow.Application.Sprints.QueryHandlers

// ── Handler: GET /projects/{projectId}/requirements ────────────────────────────

let apiGetProjectRequirements : HttpHandler =
    fun ctx ->
        task {
            let projectId = (Request.getRoute ctx).GetString "projectId"

            match ProjectId.parse projectId with
            | Error msg ->
                ctx.Response.StatusCode <- 400
                return! ofJson {| error = msg |} ctx
            | Ok pid ->
                let query =
                    { ProjectId = pid
                      StatusFilter = None
                      PriorityFilter = None }

                match! handleGetByProject readStore query with
                | Ok items -> return! ofJson items ctx
                | Error appErr -> return! handleError appErr ctx
        }

// ── Handler: GET /requirements/{id} ────────────────────────────────────────────

let apiGetRequirementById : HttpHandler =
    fun ctx ->
        task {
            let id = (Request.getRoute ctx).GetString "id"

            match RequirementId.parse id with
            | Error msg ->
                ctx.Response.StatusCode <- 400
                return! ofJson {| error = msg |} ctx
            | Ok rid ->
                let query = { RequirementId = rid }

                match! handleGetById readStore query with
                | Ok item -> return! ofJson item ctx
                | Error appErr -> return! handleError appErr ctx
        }

// ── Handler: POST /requirements ────────────────────────────────────────────────

let apiCreateRequirement : HttpHandler =
    fun ctx ->
        task {
            let! dto = bindJson<CreateRequirementDto> ctx

            match ProjectId.parse dto.ProjectId with
            | Error msg ->
                ctx.Response.StatusCode <- 400
                return! ofJson {| error = msg |} ctx
            | Ok pid ->

                match parsePriority dto.Priority, parseKind dto.Kind with
                | Error e, _
                | _, Error e ->
                    ctx.Response.StatusCode <- 400
                    return! ofJson {| error = e |} ctx
                | Ok priority, Ok kind ->
                    let cmd =
                        { ProjectId = pid
                          Title = dto.Title
                          Description = dto.Description
                          Priority = priority
                          Kind = kind
                          AcceptanceCriteria = dto.AcceptanceCriteria
                          AuthorId = UserId(Guid "00000000-0000-0000-0000-000000000099") }

                    match! handleCreate repo eventStore readStore publisher cmd with
                    | Ok reqId ->
                        ctx.Response.StatusCode <- 201
                        return! ofJson {| id = reqId |> RequirementId.value |> string |} ctx
                    | Error appErr -> return! handleError appErr ctx
        }

// ── Handler: PATCH /requirements/{id}/status ───────────────────────────────────

let apiTransitionStatus : HttpHandler =
    fun ctx ->
        task {
            let id = (Request.getRoute ctx).GetString "id"
            let! dto = bindJson<TransitionStatusDto> ctx

            match RequirementId.parse id, parseStatus dto.NewStatus with
            | Error msg, _ ->
                ctx.Response.StatusCode <- 400
                return! ofJson {| error = msg |} ctx
            | _, Error msg ->
                ctx.Response.StatusCode <- 400
                return! ofJson {| error = msg |} ctx
            | Ok rid, Ok newStatus ->
                let cmd = { RequirementId = rid; NewStatus = newStatus }

                match! handleTransition repo eventStore readStore publisher cmd with
                | Ok () -> return! ofJson {| status = "ok" |} ctx
                | Error appErr -> return! handleError appErr ctx
        }

// ── Handler: GET /projects/{projectId}/backlog ──────────────────────────────────

let apiGetProductBacklog : HttpHandler =
    fun ctx ->
        task {
            let projectId = (Request.getRoute ctx).GetString "projectId"

            match ProjectId.parse projectId with
            | Error msg ->
                ctx.Response.StatusCode <- 400
                return! ofJson {| error = msg |} ctx
            | Ok pid ->
                let query : GetProductBacklogQuery = { ProjectId = pid }
                match! BliQry.handleGetProductBacklog backlogItemRepo query with
                | Ok items -> return! ofJson items ctx
                | Error appErr -> return! handleError appErr ctx
        }

// ── Handler: GET /backlog-items/{id} ───────────────────────────────────────────

let apiGetBacklogItemById : HttpHandler =
    fun ctx ->
        task {
            let id = (Request.getRoute ctx).GetString "id"

            match BacklogItemId.parse id with
            | Error msg ->
                ctx.Response.StatusCode <- 400
                return! ofJson {| error = msg |} ctx
            | Ok bid ->
                let query : GetBacklogItemByIdQuery = { BacklogItemId = bid }
                match! BliQry.handleGetById backlogItemRepo query with
                | Ok item -> return! ofJson item ctx
                | Error appErr -> return! handleError appErr ctx
        }

// ── Handler: POST /backlog-items ────────────────────────────────────────────────

let apiCreateBacklogItem : HttpHandler =
    fun ctx ->
        task {
            let! dto = bindJson<CreateBacklogItemDto> ctx

            match ProjectId.parse dto.ProjectId, parseBacklogItemType dto.ItemType with
            | Error msg, _
            | _, Error msg ->
                ctx.Response.StatusCode <- 400
                return! ofJson {| error = msg |} ctx
            | Ok pid, Ok itemType ->

                let parentId = dto.ParentId |> Option.map BacklogItemId.parse |> Option.bind (
                    function Ok id -> Some id | _ -> None)

                let cmd : CreateBacklogItemCommand =
                    { ProjectId   = pid
                      ParentId    = parentId
                      ItemType    = itemType
                      Title       = dto.Title
                      Description = dto.Description
                      Priority    = dto.Priority
                      StoryPoints = dto.StoryPoints
                      Effort      = dto.Effort
                      AuthorId    = UserId(Guid "00000000-0000-0000-0000-000000000099") }

                match! BliCmd.handleCreate backlogItemRepo cmd with
                | Ok biId ->
                    ctx.Response.StatusCode <- 201
                    return! ofJson {| id = biId |> BacklogItemId.value |> string |} ctx
                | Error appErr -> return! handleError appErr ctx
        }

// ── Handler: PATCH /backlog-items/{id}/status ─────────────────────────────────

let apiTransitionBacklogItemStatus : HttpHandler =
    fun ctx ->
        task {
            let id = (Request.getRoute ctx).GetString "id"
            let! dto = bindJson<TransitionStatusDto> ctx

            match BacklogItemId.parse id, parseBacklogItemStatus dto.NewStatus with
            | Error msg, _
            | _, Error msg ->
                ctx.Response.StatusCode <- 400
                return! ofJson {| error = msg |} ctx
            | Ok bid, Ok newStatus ->
                let cmd : TransitionBacklogItemStatusCommand = { BacklogItemId = bid; NewStatus = newStatus }
                match! BliCmd.handleTransition backlogItemRepo cmd with
                | Ok () -> return! ofJson {| status = "ok" |} ctx
                | Error appErr -> return! handleError appErr ctx
        }

// ── Handler: PATCH /backlog-items/{id}/assign-to-sprint ─────────────────────────

let apiAssignToSprint : HttpHandler =
    fun ctx ->
        task {
            let id = (Request.getRoute ctx).GetString "id"
            let! dto = bindJson<AssignToSprintDto> ctx

            match BacklogItemId.parse id, SprintId.parse dto.SprintId with
            | Error msg, _
            | _, Error msg ->
                ctx.Response.StatusCode <- 400
                return! ofJson {| error = msg |} ctx
            | Ok bid, Ok sid ->
                let cmd : AssignToSprintCommand = { BacklogItemId = bid; SprintId = sid }
                match! BliCmd.handleAssignToSprint backlogItemRepo cmd with
                | Ok () -> return! ofJson {| status = "ok" |} ctx
                | Error appErr -> return! handleError appErr ctx
        }

// ── Handler: PATCH /backlog-items/{id}/remove-from-sprint ───────────────────────

let apiRemoveFromSprint : HttpHandler =
    fun ctx ->
        task {
            let id = (Request.getRoute ctx).GetString "id"

            match BacklogItemId.parse id with
            | Error msg ->
                ctx.Response.StatusCode <- 400
                return! ofJson {| error = msg |} ctx
            | Ok bid ->
                let cmd : RemoveFromSprintCommand = { BacklogItemId = bid }
                match! BliCmd.handleRemoveFromSprint backlogItemRepo cmd with
                | Ok () -> return! ofJson {| status = "ok" |} ctx
                | Error appErr -> return! handleError appErr ctx
        }

// ── Handler: GET /projects/{projectId}/sprints ─────────────────────────────────

let apiListSprints : HttpHandler =
    fun ctx ->
        task {
            let projectId = (Request.getRoute ctx).GetString "projectId"

            match ProjectId.parse projectId with
            | Error msg ->
                ctx.Response.StatusCode <- 400
                return! ofJson {| error = msg |} ctx
            | Ok pid ->
                let query : ListSprintsQuery = { ProjectId = pid }
                match! SpQry.handleList sprintRepo query with
                | Ok items -> return! ofJson items ctx
                | Error appErr -> return! handleError appErr ctx
        }

// ── Handler: GET /sprints/{id} ─────────────────────────────────────────────────

let apiGetSprintById : HttpHandler =
    fun ctx ->
        task {
            let id = (Request.getRoute ctx).GetString "id"

            match SprintId.parse id with
            | Error msg ->
                ctx.Response.StatusCode <- 400
                return! ofJson {| error = msg |} ctx
            | Ok sid ->
                let query : GetSprintByIdQuery = { SprintId = sid }
                match! SpQry.handleGetById sprintRepo query with
                | Ok sprint -> return! ofJson sprint ctx
                | Error appErr -> return! handleError appErr ctx
        }

// ── Handler: GET /sprints/{id}/backlog ─────────────────────────────────────────

let apiGetSprintBacklog : HttpHandler =
    fun ctx ->
        task {
            let id = (Request.getRoute ctx).GetString "id"

            match SprintId.parse id with
            | Error msg ->
                ctx.Response.StatusCode <- 400
                return! ofJson {| error = msg |} ctx
            | Ok sid ->
                let query : GetSprintBacklogQuery = { SprintId = sid }
                match! BliQry.handleGetSprintBacklog backlogItemRepo query with
                | Ok items -> return! ofJson items ctx
                | Error appErr -> return! handleError appErr ctx
        }

// ── Handler: POST /sprints ──────────────────────────────────────────────────────

let apiCreateSprint : HttpHandler =
    fun ctx ->
        task {
            let! dto = bindJson<CreateSprintDto> ctx

            match ProjectId.parse dto.ProjectId with
            | Error msg ->
                ctx.Response.StatusCode <- 400
                return! ofJson {| error = msg |} ctx
            | Ok pid ->

                let tryParseDate (s : string) =
                    match DateTime.TryParse s with
                    | true, d -> Ok d
                    | _       -> Error $"Ungültiges Datum: {s}"

                match tryParseDate dto.StartDate, tryParseDate dto.EndDate with
                | Error msg, _
                | _, Error msg ->
                    ctx.Response.StatusCode <- 400
                    return! ofJson {| error = msg |} ctx
                | Ok startDate, Ok endDate ->
                    let cmd : CreateSprintCommand =
                        { ProjectId = pid
                          Name      = dto.Name
                          Goal      = dto.Goal
                          StartDate = startDate
                          EndDate   = endDate }

                    match! SpCmd.handleCreate sprintRepo cmd with
                    | Ok sprintId ->
                        ctx.Response.StatusCode <- 201
                        return! ofJson {| id = sprintId |> SprintId.value |> string |} ctx
                    | Error appErr -> return! handleError appErr ctx
        }

// ── Handler: PATCH /sprints/{id}/status ─────────────────────────────────────────

let apiTransitionSprintStatus : HttpHandler =
    fun ctx ->
        task {
            let id = (Request.getRoute ctx).GetString "id"
            let! dto = bindJson<TransitionSprintStatusDto> ctx

            match SprintId.parse id, parseSprintStatus dto.NewStatus with
            | Error msg, _
            | _, Error msg ->
                ctx.Response.StatusCode <- 400
                return! ofJson {| error = msg |} ctx
            | Ok sid, Ok newStatus ->
                let cmd : TransitionSprintStatusCommand = { SprintId = sid; NewStatus = newStatus }
                match! SpCmd.handleTransition sprintRepo cmd with
                | Ok () -> return! ofJson {| status = "ok" |} ctx
                | Error appErr -> return! handleError appErr ctx
        }

// ── Einstiegspunkt ─────────────────────────────────────────────────────────────

[<EntryPoint>]
let main args =
    let routes =
        [ get "/projects/{projectId}/requirements" apiGetProjectRequirements
          get "/requirements/{id}" apiGetRequirementById
          post "/requirements" apiCreateRequirement
          patch "/requirements/{id}/status" apiTransitionStatus
          // BacklogItems
          get "/projects/{projectId}/backlog" apiGetProductBacklog
          get "/backlog-items/{id}" apiGetBacklogItemById
          post "/backlog-items" apiCreateBacklogItem
          patch "/backlog-items/{id}/status" apiTransitionBacklogItemStatus
          patch "/backlog-items/{id}/assign-to-sprint" apiAssignToSprint
          patch "/backlog-items/{id}/remove-from-sprint" apiRemoveFromSprint
          // Sprints
          get "/projects/{projectId}/sprints" apiListSprints
          get "/sprints/{id}" apiGetSprintById
          get "/sprints/{id}/backlog" apiGetSprintBacklog
          post "/sprints" apiCreateSprint
          patch "/sprints/{id}/status" apiTransitionSprintStatus ]

    let app = WebApplication.Create(args)
    app.UseFalco routes |> ignore
    app.Run()
    0
