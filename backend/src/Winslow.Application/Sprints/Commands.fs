module Winslow.Application.Sprints.Commands

open System
open Winslow.Domain.Common.Types
open Winslow.Domain.Sprints.SprintTypes

type CreateSprintCommand = {
    ProjectId : ProjectId
    Name      : string
    Goal      : string
    StartDate : DateTime
    EndDate   : DateTime
}

type TransitionSprintStatusCommand = {
    SprintId  : SprintId
    NewStatus : SprintStatus
}
