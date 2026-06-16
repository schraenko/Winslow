module Winslow.Application.Common.Ports

open System.Threading.Tasks
open Winslow.Domain.Common.Types
open Winslow.Domain.Common.Errors
open Winslow.Domain.Requirements.Requirement
open Winslow.Domain.Requirements.RequirementTypes
open Winslow.Domain.Requirements.RequirementEvents

// ── Repository-Ports (Interfaces via object expressions) ─────────────────────

type IRequirementRepository =
    abstract member FindById      : RequirementId -> Task<Result<Requirement, AppError>>
    abstract member FindByProject : ProjectId -> Task<Result<Requirement list, AppError>>
    abstract member Save          : Requirement -> Task<Result<unit, AppError>>
    abstract member Delete        : RequirementId -> Task<Result<unit, AppError>>

type IEventPublisher =
    abstract member Publish : RequirementEvent -> Task<unit>
