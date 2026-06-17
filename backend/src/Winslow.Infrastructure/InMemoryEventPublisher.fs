module Winslow.Infrastructure.InMemoryEventPublisher

open System.Threading.Tasks
open Winslow.Domain.Requirements.RequirementEvents
open Winslow.Application.Common.Ports

let create () : EventPublisher =
    {   Publish = fun (event : RequirementEvent) ->
            task {
                let desc =
                    match event with
                    | RequirementCreated _       -> "RequirementCreated"
                    | RequirementStatusChanged _ -> "RequirementStatusChanged"
                    | RequirementUpdated _       -> "RequirementUpdated"
                    | RequirementDeleted _       -> "RequirementDeleted"
                printfn "[Event] %s" desc
            }
    }
