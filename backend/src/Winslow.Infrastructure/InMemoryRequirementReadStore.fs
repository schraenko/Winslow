module Winslow.Infrastructure.InMemoryRequirementReadStore

open System
open System.Collections.Concurrent
open System.Threading.Tasks
open Winslow.Domain.Common.Types
open Winslow.Domain.Common.Errors
open Winslow.Domain.Requirements.RequirementTypes
open Winslow.Application.Requirements.Queries
open Winslow.Application.Requirements.RequirementReadStore

let create () : RequirementReadStore =
    let store = ConcurrentDictionary<Guid, RequirementReadModel>()

    {   GetById = fun (reqId : RequirementId) ->
            task {
                let id = reqId |> RequirementId.value
                match store.TryGetValue id with
                | true, model -> return Ok model
                | false, _    -> return Error (Domain (NotFound ("Requirement", string id)))
            }

        GetByProject = fun (projId : ProjectId) (statusFilter : RequirementStatus option) (priorityFilter : RequirementPriority option) ->
            task {
                let pid = projId |> ProjectId.value |> string
                let results =
                    store.Values
                    |> Seq.filter (fun m -> m.ProjectId = pid)
                    |> Seq.filter (fun m ->
                        statusFilter   |> Option.forall (fun s -> m.Status = string s)   &&
                        priorityFilter |> Option.forall (fun p -> m.Priority = string p))
                    |> Seq.toList
                return Ok results
            }

        Upsert = fun (reqId : RequirementId) (model : RequirementReadModel) ->
            task {
                let id = reqId |> RequirementId.value
                store.AddOrUpdate(id, model, fun _ _ -> model) |> ignore
                return Ok ()
            }

        Delete = fun (reqId : RequirementId) ->
            task {
                let id = reqId |> RequirementId.value
                store.TryRemove id |> ignore
                return Ok ()
            }
    }
