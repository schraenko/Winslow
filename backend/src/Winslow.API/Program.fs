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
open Winslow.Application.Common.Ports
open Winslow.Application.Requirements.Commands
open Winslow.Application.Requirements.Queries
open Winslow.Application.Requirements.CommandHandlers
open Winslow.Application.Requirements.QueryHandlers
open Winslow.Infrastructure.InMemoryRequirementRepository
open Winslow.Infrastructure.InMemoryEventPublisher

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

let repo : IRequirementRepository = InMemoryRequirementRepository()
let publisher : IEventPublisher = InMemoryEventPublisher()

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

                match! handleGetByProject repo query with
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

                match! handleGetById repo query with
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

                    match! handleCreate repo publisher cmd with
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

                match! handleTransition repo publisher cmd with
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
          patch "/requirements/{id}/status" apiTransitionStatus ]

    let app = WebApplication.Create(args)
    app.UseFalco routes |> ignore
    app.Run()
    0
