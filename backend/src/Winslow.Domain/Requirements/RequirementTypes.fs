module Winslow.Domain.Requirements.RequirementTypes

open Winslow.Domain.Common.Types
open Winslow.Domain.Common.Errors

// ── Kerntypen ─────────────────────────────────────────────────────────────────

type RequirementStatus =
    | Draft
    | UnderReview
    | Approved
    | Rejected
    | Implemented

type RequirementPriority =
    | MustHave
    | ShouldHave
    | CouldHave
    | WontHave   // MoSCoW

type RequirementKind =
    | Functional
    | NonFunctional
    | Constraint
    | BusinessRule

// ── Value Objects ─────────────────────────────────────────────────────────────

type RequirementTitle = private RequirementTitle of NonEmptyString

module RequirementTitle =
    let create s =
        NonEmptyString.create "Titel" s
        |> Result.map RequirementTitle

    let value (RequirementTitle t) = NonEmptyString.value t

type AcceptanceCriteria = AcceptanceCriteria of string list

module AcceptanceCriteria =
    let create (criteria: string list) =
        if criteria |> List.forall (fun s -> s.Trim() <> "") then
            Ok (AcceptanceCriteria criteria)
        else
            Error (ValidationError ("AcceptanceCriteria", "Leere Kriterien sind nicht erlaubt."))

    let value (AcceptanceCriteria c) = c

// ── Statusübergänge ───────────────────────────────────────────────────────────

module RequirementStatus =
    let allowedTransitions =
        Map.ofList [
            Draft,          [ UnderReview ]
            UnderReview,    [ Approved; Rejected; Draft ]
            Approved,       [ Implemented; UnderReview ]
            Rejected,       [ Draft ]
            Implemented,    []
        ]

    let canTransitionTo (from: RequirementStatus) (``to``: RequirementStatus) =
        allowedTransitions
        |> Map.tryFind from
        |> Option.map (List.contains ``to``)
        |> Option.defaultValue false

    let transition from ``to`` =
        if canTransitionTo from ``to`` then
            Ok ``to``
        else
            Error (InvalidTransition (string from, string ``to``))
