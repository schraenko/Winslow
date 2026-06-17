module Winslow.Domain.Common.Types

open System
open Winslow.Domain.Common.Errors

// ── Strongly-typed IDs ────────────────────────────────────────────────────────

type RequirementId = RequirementId of Guid
type ProjectId     = ProjectId of Guid
type UserId        = UserId of Guid

module RequirementId =
    let create ()    = RequirementId (Guid.NewGuid())
    let value (RequirementId id) = id
    let parse (s: string) =
        match Guid.TryParse(s) with
        | true, g  -> Ok (RequirementId g)
        | false, _ -> Error $"Ungültige RequirementId: {s}"

module ProjectId =
    let create ()  = ProjectId (Guid.NewGuid())
    let value (ProjectId id) = id
    let parse (s: string) =
        match Guid.TryParse(s) with
        | true, g  -> Ok (ProjectId g)
        | false, _ -> Error $"Ungültige ProjectId: {s}"

module UserId =
    let create ()  = UserId (Guid.NewGuid())
    let value (UserId id) = id

type BacklogItemId = BacklogItemId of Guid
type SprintId      = SprintId of Guid
type PhaseId       = PhaseId of Guid
type BoardColumnId = BoardColumnId of Guid
type MilestoneId   = MilestoneId of Guid

module BacklogItemId =
    let create () = BacklogItemId (Guid.NewGuid())
    let value (BacklogItemId id) = id
    let parse (s: string) =
        match Guid.TryParse(s) with
        | true, g  -> Ok (BacklogItemId g)
        | false, _ -> Error $"Ungültige BacklogItemId: {s}"

module SprintId =
    let create () = SprintId (Guid.NewGuid())
    let value (SprintId id) = id
    let parse (s: string) =
        match Guid.TryParse(s) with
        | true, g  -> Ok (SprintId g)
        | false, _ -> Error $"Ungültige SprintId: {s}"

module PhaseId =
    let create () = PhaseId (Guid.NewGuid())
    let value (PhaseId id) = id

module BoardColumnId =
    let create () = BoardColumnId (Guid.NewGuid())
    let value (BoardColumnId id) = id

module MilestoneId =
    let create () = MilestoneId (Guid.NewGuid())
    let value (MilestoneId id) = id

// ── Timestamps ────────────────────────────────────────────────────────────────

type Timestamp = Timestamp of DateTimeOffset

module Timestamp =
    let now ()              = Timestamp DateTimeOffset.UtcNow
    let value (Timestamp t) = t

// ── Railway: result { } computation expression ───────────────────────────────

type ResultBuilder() =
    member _.Bind(m, f)       = Result.bind f m
    member _.Return(x)        = Ok x
    member _.ReturnFrom(x)    = x
    member _.Zero()           = Ok ()

let result = ResultBuilder()

// ── Bind operator (CON-10) ────────────────────────────────────────────────────

let (>>=) = Result.bind

// ── Non-empty string ──────────────────────────────────────────────────────────

type NonEmptyString = private NonEmptyString of string

module NonEmptyString =
    let create fieldName (s: string) =
        if String.IsNullOrWhiteSpace(s) then
            Error (ValidationError (fieldName, "darf nicht leer sein."))
        else
            Ok (NonEmptyString (s.Trim()))

    let value (NonEmptyString s) = s
