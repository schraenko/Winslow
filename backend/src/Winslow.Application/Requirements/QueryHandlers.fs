module Winslow.Application.Requirements.QueryHandlers

open System.Threading.Tasks
open Winslow.Domain.Common.Types
open Winslow.Domain.Common.Errors
open Winslow.Application.Requirements.Queries
open Winslow.Application.Requirements.RequirementReadStore

// ── Handler: Einzelne Anforderung ─────────────────────────────────────────────

let handleGetById
    (readStore : RequirementReadStore)
    (query     : GetRequirementByIdQuery)
    : Task<Result<RequirementReadModel, AppError>> =
    readStore.GetById query.RequirementId

// ── Handler: Liste nach Projekt ───────────────────────────────────────────────

let handleGetByProject
    (readStore : RequirementReadStore)
    (query     : GetRequirementsByProjectQuery)
    : Task<Result<RequirementReadModel list, AppError>> =
    readStore.GetByProject query.ProjectId query.StatusFilter query.PriorityFilter
