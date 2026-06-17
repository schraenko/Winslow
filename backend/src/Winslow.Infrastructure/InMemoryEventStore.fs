module Winslow.Infrastructure.InMemoryEventStore

open System
open System.Collections.Concurrent
open System.Threading.Tasks
open Winslow.Domain.Common.Types
open Winslow.Domain.Common.Errors
open Winslow.Domain.Requirements.RequirementTypes
open Winslow.Domain.Requirements.RequirementEvents
open Winslow.Application.Common.Ports

let create () : EventStore =
    let store = ConcurrentDictionary<Guid, EventEnvelope list>()

    {   Append = fun (reqId : RequirementId) (envelopes : EventEnvelope list) ->
            task {
                let id = reqId |> RequirementId.value
                store.AddOrUpdate(id, envelopes, fun _ existing -> existing @ envelopes) |> ignore
                return Ok ()
            }

        ReadStream = fun (reqId : RequirementId) ->
            task {
                let id = reqId |> RequirementId.value
                match store.TryGetValue id with
                | true, events -> return Ok (events |> List.sortBy (fun e -> e.Version))
                | false, _     -> return Ok []
            }
    }
