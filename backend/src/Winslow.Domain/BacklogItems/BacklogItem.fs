module Winslow.Domain.BacklogItems.BacklogItem

open Winslow.Domain.Common.Types
open Winslow.Domain.Common.Errors
open Winslow.Domain.BacklogItems.BacklogItemTypes
open Winslow.Domain.BacklogItems.BacklogItemEvents

// ── Aggregat ──────────────────────────────────────────────────────────────────

type BacklogItem = private BacklogItem of BacklogItemData

and BacklogItemData = {
    Id            : BacklogItemId
    ProjectId     : ProjectId
    ParentId      : BacklogItemId option
    ItemType      : BacklogItemType
    Title         : NonEmptyString
    Description   : string
    Priority      : int
    StoryPoints   : int option
    Effort        : float option
    Status        : BacklogItemStatus
    BoardColumnId : BoardColumnId option
    SprintId      : SprintId option
    PhaseId       : PhaseId option
    AuthorId      : UserId
    CreatedAt     : Timestamp
    UpdatedAt     : Timestamp
}

// ── Accessor functions ────────────────────────────────────────────────────────

let id            (BacklogItem data) = data.Id
let projectId     (BacklogItem data) = data.ProjectId
let parentId      (BacklogItem data) = data.ParentId
let itemType      (BacklogItem data) = data.ItemType
let title         (BacklogItem data) = data.Title
let description   (BacklogItem data) = data.Description
let priority      (BacklogItem data) = data.Priority
let storyPoints   (BacklogItem data) = data.StoryPoints
let effort        (BacklogItem data) = data.Effort
let status        (BacklogItem data) = data.Status
let boardColumnId (BacklogItem data) = data.BoardColumnId
let sprintId      (BacklogItem data) = data.SprintId
let authorId      (BacklogItem data) = data.AuthorId
let createdAt     (BacklogItem data) = data.CreatedAt
let updatedAt     (BacklogItem data) = data.UpdatedAt

let hydrate
    (id            : BacklogItemId)
    (projectId     : ProjectId)
    (parentId      : BacklogItemId option)
    (itemType      : BacklogItemType)
    (title         : NonEmptyString)
    (description   : string)
    (priority      : int)
    (storyPoints   : int option)
    (effort        : float option)
    (status        : BacklogItemStatus)
    (boardColumnId : BoardColumnId option)
    (sprintId      : SprintId option)
    (phaseId       : PhaseId option)
    (authorId      : UserId)
    (createdAt     : Timestamp)
    (updatedAt     : Timestamp)
    : BacklogItem =
    BacklogItem {
        Id            = id
        ProjectId     = projectId
        ParentId      = parentId
        ItemType      = itemType
        Title         = title
        Description   = description
        Priority      = priority
        StoryPoints   = storyPoints
        Effort        = effort
        Status        = status
        BoardColumnId = boardColumnId
        SprintId      = sprintId
        PhaseId       = phaseId
        AuthorId      = authorId
        CreatedAt     = createdAt
        UpdatedAt     = updatedAt
    }

// ── Factory ───────────────────────────────────────────────────────────────────

type CreateBacklogItemInput = {
    ProjectId     : ProjectId
    ParentId      : BacklogItemId option
    ParentType    : BacklogItemType option  // resolved by application layer
    ItemType      : BacklogItemType
    Title         : string
    Description   : string
    Priority      : int
    StoryPoints   : int option
    Effort        : float option
    AuthorId      : UserId
}

let create
    (input : CreateBacklogItemInput)
    : Result<BacklogItem * BacklogItemEvent, DomainError> =
    result {
        do! validateHierarchy input.ItemType input.ParentType

        let! title = NonEmptyString.create "Title" input.Title
        let now    = Timestamp.now ()

        let data = {
            Id            = BacklogItemId.create ()
            ProjectId     = input.ProjectId
            ParentId      = input.ParentId
            ItemType      = input.ItemType
            Title         = title
            Description   = input.Description
            Priority      = input.Priority
            StoryPoints   = input.StoryPoints
            Effort        = input.Effort
            Status        = Open
            BoardColumnId = None
            SprintId      = None
            PhaseId       = None
            AuthorId      = input.AuthorId
            CreatedAt     = now
            UpdatedAt     = now
        }

        let event = BacklogItemCreated {
            BacklogItemId = data.Id
            ProjectId     = data.ProjectId
            ItemType      = data.ItemType
            ParentId      = data.ParentId
            Title         = NonEmptyString.value data.Title
            AuthorId      = data.AuthorId
            OccurredAt    = now
        }

        return BacklogItem data, event
    }

// ── Status transition ─────────────────────────────────────────────────────────

let transitionStatus
    (newStatus : BacklogItemStatus)
    (item      : BacklogItem)
    : Result<BacklogItem * BacklogItemEvent, DomainError> =
    let (BacklogItem data) = item
    if data.Status = newStatus then
        Error (ValidationError ("Status", "Item already has this status"))
    else
        let now = Timestamp.now ()
        let updatedData = { data with Status = newStatus; UpdatedAt = now }
        let event = BacklogItemStatusChanged {
            BacklogItemId = data.Id
            From          = data.Status
            To            = newStatus
            OccurredAt    = now
        }
        Ok (BacklogItem updatedData, event)

// ── Assign to sprint ─────────────────────────────────────────────────────────

let assignToSprint
    (sprintId : SprintId)
    (item     : BacklogItem)
    : Result<BacklogItem * BacklogItemEvent, DomainError> =
    let (BacklogItem data) = item
    match data.ItemType with
    | Epic | Feature -> Error (ValidationError ("SprintId", "Epics and Features cannot be assigned to sprints"))
    | _ ->
        let now = Timestamp.now ()
        let updatedData = { data with SprintId = Some sprintId; UpdatedAt = now }
        let event = BacklogItemAssignedToSprint {
            BacklogItemId = data.Id
            SprintId      = sprintId
            OccurredAt    = now
        }
        Ok (BacklogItem updatedData, event)

// ── Remove from sprint ────────────────────────────────────────────────────────

let removeFromSprint
    (item : BacklogItem)
    : Result<BacklogItem * BacklogItemEvent, DomainError> =
    let (BacklogItem data) = item
    let now = Timestamp.now ()
    let updatedData = { data with SprintId = None; UpdatedAt = now }
    let event = BacklogItemRemovedFromSprint (data.Id, now)
    Ok (BacklogItem updatedData, event)
