module Winslow.Application.Sprints.Queries

open System
open Winslow.Domain.Common.Types
open Winslow.Domain.Sprints.SprintTypes

type GetSprintByIdQuery = {
    SprintId : SprintId
}

type ListSprintsQuery = {
    ProjectId : ProjectId
}

// ── Read Model ────────────────────────────────────────────────────────────────

type SprintReadModel = {
    Id        : string
    ProjectId : string
    Name      : string
    Goal      : string
    StartDate : string
    EndDate   : string
    Status    : string
}
