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

// ── Non-empty string ──────────────────────────────────────────────────────────

type NonEmptyString = private NonEmptyString of string

module NonEmptyString =
    let create fieldName (s: string) =
        if String.IsNullOrWhiteSpace(s) then
            Error (ValidationError (fieldName, "darf nicht leer sein."))
        else
            Ok (NonEmptyString (s.Trim()))

    let value (NonEmptyString s) = s
