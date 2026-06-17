module Winslow.Application.BacklogItems.Queries

open Winslow.Domain.Common.Types
open Winslow.Domain.BacklogItems.BacklogItemTypes

type GetBacklogItemByIdQuery = {
    BacklogItemId : BacklogItemId
}

type GetProductBacklogQuery = {
    ProjectId : ProjectId
}

type GetSprintBacklogQuery = {
    SprintId : SprintId
}

// ── Read Model ────────────────────────────────────────────────────────────────

type BacklogItemReadModel = {
    Id            : string
    ProjectId     : string
    ParentId      : string option
    ItemType      : string
    Title         : string
    Description   : string
    Priority      : int
    StoryPoints   : int option
    Effort        : float option
    Status        : string
    SprintId      : string option
    Children      : BacklogItemReadModel list
}
