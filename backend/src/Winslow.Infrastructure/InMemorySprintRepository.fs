module Winslow.Infrastructure.InMemorySprintRepository

open System
open System.Collections.Concurrent
open System.Threading.Tasks
open Winslow.Domain.Common.Types
open Winslow.Domain.Common.Errors
open Winslow.Domain.Sprints.Sprint
open Winslow.Domain.Sprints.SprintTypes
open Winslow.Application.Common.Ports

let create () : SprintRepository =
    let store = ConcurrentDictionary<Guid, Sprint>()

    let now = Timestamp.now ()
    let demoProjectId = ProjectId (Guid.Parse "00000000-0000-0000-0000-000000000001")

    let seed
        (id        : string)
        (name      : string)
        (goal      : string)
        (startDate : DateTime)
        (endDate   : DateTime)
        (status    : SprintStatus)
        : Sprint =
        match NonEmptyString.create "Name" name with
        | Ok n ->
            hydrate
                (SprintId (Guid.Parse id))
                demoProjectId
                n
                goal
                startDate
                endDate
                status
                now
                now
        | _ -> failwith "invalid seed data"

    // 2 demo sprints
    store.TryAdd(Guid "00000000-0000-0000-0000-000000000071",
        seed "00000000-0000-0000-0000-000000000071" "Sprint 1" "MVP der Benutzerverwaltung"
            (DateTime(2025, 6, 1)) (DateTime(2025, 6, 14)) Completed)
    |> ignore

    store.TryAdd(Guid "00000000-0000-0000-0000-000000000072",
        seed "00000000-0000-0000-0000-000000000072" "Sprint 2" "Anmeldung und Profile"
            (DateTime(2025, 6, 15)) (DateTime(2025, 6, 28)) Active)
    |> ignore

    {   FindById = fun (id : SprintId) ->
            task {
                let g = id |> SprintId.value
                match store.TryGetValue g with
                | true, sprint -> return Ok sprint
                | false, _     -> return Error (Domain (NotFound ("Sprint", string g)))
            }

        FindByProject = fun (projId : ProjectId) ->
            task {
                let pid = projId |> ProjectId.value
                let sprints =
                    store.Values
                    |> Seq.filter (fun s -> (projectId s |> ProjectId.value) = pid)
                    |> Seq.sortByDescending (fun s -> startDate s)
                    |> Seq.toList
                return Ok sprints
            }

        Save = fun (sprint : Sprint) ->
            task {
                let g = id sprint |> SprintId.value
                store.AddOrUpdate(g, sprint, fun _ _ -> sprint) |> ignore
                return Ok ()
            }
    }
