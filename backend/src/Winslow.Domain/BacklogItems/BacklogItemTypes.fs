module Winslow.Domain.BacklogItems.BacklogItemTypes

open Winslow.Domain.Common.Types
open Winslow.Domain.Common.Errors

// ── Kerntypen ─────────────────────────────────────────────────────────────────

type BacklogItemType =
    | Epic
    | Feature
    | PBI
    | Task
    | Bug
    | Impediment

type BacklogItemStatus =
    | Open
    | InProgress
    | Done
    | Cancelled

// ── Hierarchy validation ──────────────────────────────────────────────────────

let allowedParentTypes (itemType : BacklogItemType) : BacklogItemType list =
    match itemType with
    | Epic       -> []
    | Feature    -> [ Epic ]
    | PBI        -> [ Feature ]
    | Task       -> [ PBI; Bug; Impediment ]
    | Bug        -> [ Feature ]
    | Impediment -> [ Feature ]

let isValidParent (childType : BacklogItemType) (parentType : BacklogItemType) =
    allowedParentTypes childType |> List.contains parentType

let validateHierarchy
    (childType  : BacklogItemType)
    (parentType : BacklogItemType option)
    : Result<unit, DomainError> =
    match parentType with
    | None ->
        if allowedParentTypes childType |> List.isEmpty then Ok ()
        else Error (ValidationError ("ParentId", $"{childType} requires a parent"))
    | Some pt ->
        if isValidParent childType pt then Ok ()
        else Error (ValidationError ("ParentId", $"{childType} cannot have parent of type {pt}"))
