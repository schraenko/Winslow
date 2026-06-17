module Winslow.Domain.Sprints.SprintTypes

open Winslow.Domain.Common.Errors

type SprintStatus =
    | Planned
    | Active
    | Completed

module SprintStatus =
    let allowedTransitions =
        Map.ofList [
            Planned,   [ Active ]
            Active,    [ Completed ]
            Completed, []
        ]

    let canTransitionTo (fromStatus : SprintStatus) (toStatus : SprintStatus) =
        allowedTransitions
        |> Map.tryFind fromStatus
        |> Option.map (List.contains toStatus)
        |> Option.defaultValue false

    let transition (fromStatus : SprintStatus) (toStatus : SprintStatus) =
        if canTransitionTo fromStatus toStatus then
            Ok toStatus
        else
            Error (InvalidTransition (string fromStatus, string toStatus))
