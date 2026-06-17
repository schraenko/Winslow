module Winslow.Application.BacklogItems.Commands

open Winslow.Domain.Common.Types
open Winslow.Domain.BacklogItems.BacklogItemTypes

type CreateBacklogItemCommand = {
    ProjectId     : ProjectId
    ParentId      : BacklogItemId option
    ItemType      : BacklogItemType
    Title         : string
    Description   : string
    Priority      : int
    StoryPoints   : int option
    Effort        : float option
    AuthorId      : UserId
}

type UpdateBacklogItemCommand = {
    BacklogItemId : BacklogItemId
    Title         : string option
    Description   : string option
    Priority      : int option
    StoryPoints   : int option
    Effort        : float option
}

type TransitionBacklogItemStatusCommand = {
    BacklogItemId : BacklogItemId
    NewStatus     : BacklogItemStatus
}

type AssignToSprintCommand = {
    BacklogItemId : BacklogItemId
    SprintId      : SprintId
}

type RemoveFromSprintCommand = {
    BacklogItemId : BacklogItemId
}
