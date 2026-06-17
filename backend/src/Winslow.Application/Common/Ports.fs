module Winslow.Application.Common.Ports

open System
open System.Threading.Tasks
open Winslow.Domain.Common.Types
open Winslow.Domain.Common.Errors
open Winslow.Domain.Requirements.Requirement
open Winslow.Domain.Requirements.RequirementTypes
open Winslow.Domain.Requirements.RequirementEvents
open Winslow.Domain.BacklogItems.BacklogItem
open Winslow.Domain.BacklogItems.BacklogItemTypes
open Winslow.Domain.Sprints.Sprint
open Winslow.Domain.Sprints.SprintTypes

// ── Repository-Ports (Records of functions) ──────────────────────────────────

type RequirementRepository = {
    FindById      : RequirementId -> Task<Result<Requirement, AppError>>
    FindByProject : ProjectId -> Task<Result<Requirement list, AppError>>
    Save          : Requirement -> Task<Result<unit, AppError>>
    Delete        : RequirementId -> Task<Result<unit, AppError>>
}

type EventPublisher = {
    Publish : RequirementEvent -> Task<unit>
}

// ── Event Store ───────────────────────────────────────────────────────────────

type EventEnvelope = {
    AggregateId : Guid
    Version     : int64
    Event       : RequirementEvent
    OccurredAt  : Timestamp
}

type EventStore = {
    Append     : RequirementId -> EventEnvelope list -> Task<Result<unit, AppError>>
    ReadStream : RequirementId -> Task<Result<EventEnvelope list, AppError>>
}

// ── BacklogItem Repository ─────────────────────────────────────────────────────

type BacklogItemRepository = {
    FindById      : BacklogItemId -> Task<Result<BacklogItem, AppError>>
    FindByProject : ProjectId -> Task<Result<BacklogItem list, AppError>>
    FindBySprint  : SprintId -> Task<Result<BacklogItem list, AppError>>
    FindChildren  : BacklogItemId -> Task<Result<BacklogItem list, AppError>>
    Save          : BacklogItem -> Task<Result<unit, AppError>>
    Delete        : BacklogItemId -> Task<Result<unit, AppError>>
}

// ── Sprint Repository ──────────────────────────────────────────────────────────

type SprintRepository = {
    FindById      : SprintId -> Task<Result<Sprint, AppError>>
    FindByProject : ProjectId -> Task<Result<Sprint list, AppError>>
    Save          : Sprint -> Task<Result<unit, AppError>>
}
