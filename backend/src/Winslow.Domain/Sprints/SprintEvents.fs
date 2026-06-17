module Winslow.Domain.Sprints.SprintEvents

open System
open Winslow.Domain.Common.Types

type SprintCreatedData = {
    SprintId    : SprintId
    ProjectId   : ProjectId
    Name        : string
    Goal        : string
    StartDate   : DateTime
    EndDate     : DateTime
    OccurredAt  : Timestamp
}

type SprintStatusChangedData = {
    SprintId    : SprintId
    From        : string
    To          : string
    OccurredAt  : Timestamp
}

type SprintEvent =
    | SprintCreated       of SprintCreatedData
    | SprintStatusChanged of SprintStatusChangedData
