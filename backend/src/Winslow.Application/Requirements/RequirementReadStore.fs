module Winslow.Application.Requirements.RequirementReadStore

open System
open System.Threading.Tasks
open Winslow.Domain.Common.Types
open Winslow.Domain.Common.Errors
open Winslow.Domain.Requirements.Requirement
open Winslow.Domain.Requirements.RequirementTypes
open Winslow.Application.Requirements.Queries

// ── Projection ────────────────────────────────────────────────────────────────

let private formatTimestamp (ts: Timestamp) =
    (Timestamp.value ts).ToString("O")

let projectRequirement (req: Requirement) : RequirementReadModel = {
    Id                 = requirementId req |> RequirementId.value |> string
    ProjectId          = projectId req |> ProjectId.value |> string
    Title              = title req |> RequirementTitle.value
    Description        = description req
    Status             = string (status req)
    Priority           = string (priority req)
    Kind               = string (kind req)
    AcceptanceCriteria = acceptanceCriteria req |> AcceptanceCriteria.value
    AuthorId           = authorId req |> UserId.value |> string
    CreatedAt          = formatTimestamp (createdAt req)
    UpdatedAt          = formatTimestamp (updatedAt req)
}

// ── Read Store Port ───────────────────────────────────────────────────────────

type RequirementReadStore = {
    GetById      : RequirementId -> Task<Result<RequirementReadModel, AppError>>
    GetByProject : ProjectId -> RequirementStatus option -> RequirementPriority option -> Task<Result<RequirementReadModel list, AppError>>
    Upsert       : RequirementId -> RequirementReadModel -> Task<Result<unit, AppError>>
    Delete       : RequirementId -> Task<Result<unit, AppError>>
}
