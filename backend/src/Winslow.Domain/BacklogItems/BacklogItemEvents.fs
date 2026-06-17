module Winslow.Domain.BacklogItems.BacklogItemEvents

open Winslow.Domain.Common.Types
open Winslow.Domain.BacklogItems.BacklogItemTypes

type BacklogItemCreatedData = {
    BacklogItemId : BacklogItemId
    ProjectId     : ProjectId
    ItemType      : BacklogItemType
    ParentId      : BacklogItemId option
    Title         : string
    AuthorId      : UserId
    OccurredAt    : Timestamp
}

type BacklogItemStatusChangedData = {
    BacklogItemId : BacklogItemId
    From          : BacklogItemStatus
    To            : BacklogItemStatus
    OccurredAt    : Timestamp
}

type BacklogItemUpdatedData = {
    BacklogItemId : BacklogItemId
    OccurredAt    : Timestamp
}

type BacklogItemAssignedToSprintData = {
    BacklogItemId : BacklogItemId
    SprintId      : SprintId
    OccurredAt    : Timestamp
}

type BacklogItemEvent =
    | BacklogItemCreated          of BacklogItemCreatedData
    | BacklogItemStatusChanged    of BacklogItemStatusChangedData
    | BacklogItemUpdated          of BacklogItemUpdatedData
    | BacklogItemAssignedToSprint of BacklogItemAssignedToSprintData
    | BacklogItemRemovedFromSprint of BacklogItemId * Timestamp
