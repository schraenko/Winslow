module Winslow.Infrastructure.InMemoryRequirementRepository

open System
open System.Collections.Concurrent
open System.Threading.Tasks
open Winslow.Domain.Common.Types
open Winslow.Domain.Common.Errors
open Winslow.Domain.Requirements.Requirement
open Winslow.Domain.Requirements.RequirementTypes
open Winslow.Application.Common.Ports

type InMemoryRequirementRepository() =
    let store = ConcurrentDictionary<Guid, Requirement>()

    do
        let now = Timestamp.now ()
        let demoProjectId = ProjectId (Guid.Parse "00000000-0000-0000-0000-000000000001")
        let demoAuthorId = UserId (Guid.Parse "00000000-0000-0000-0000-000000000099")

        let seed (id : string) title desc status priority kind criteria =
            match RequirementTitle.create title, AcceptanceCriteria.create criteria with
            | Ok t, Ok c ->
                { Id                 = RequirementId (Guid.Parse id)
                  ProjectId          = demoProjectId
                  Title              = t
                  Description        = desc
                  Status             = status
                  Priority           = priority
                  Kind               = kind
                  AcceptanceCriteria = c
                  AuthorId           = demoAuthorId
                  CreatedAt          = now
                  UpdatedAt          = now }
            | _ -> failwith "invalid seed data"

        store.TryAdd(Guid "00000000-0000-0000-0000-000000000001",
            seed "00000000-0000-0000-0000-000000000001" "Benutzeranmeldung"
                "Nutzer können sich mit E-Mail und Passwort anmelden."
                Approved MustHave Functional
                [ "Login-Formular vorhanden"; "Fehlermeldung bei falschen Daten"; "JWT wird zurückgegeben" ])
        |> ignore

        store.TryAdd(Guid "00000000-0000-0000-0000-000000000002",
            seed "00000000-0000-0000-0000-000000000002" "Anforderungsliste anzeigen"
                "Nutzer sehen alle Anforderungen eines Projekts."
                Draft ShouldHave Functional
                [ "Liste ist filterbar nach Status"; "Paginierung bei > 50 Einträgen" ])
        |> ignore

    interface IRequirementRepository with
        member _.FindById (reqId : RequirementId) =
            task {
                let id = reqId |> RequirementId.value
                match store.TryGetValue id with
                | true, req -> return Ok req
                | false, _  -> return Error (Domain (NotFound ("Requirement", string id)))
            }

        member _.FindByProject (projId : ProjectId) =
            task {
                let pid = projId |> ProjectId.value
                let results =
                    store.Values
                    |> Seq.filter (fun r -> r.ProjectId |> ProjectId.value = pid)
                    |> Seq.toList
                return Ok results
            }

        member _.Save (req : Requirement) =
            task {
                let id = req.Id |> RequirementId.value
                store.AddOrUpdate(id, req, fun _ _ -> req) |> ignore
                return Ok ()
            }

        member _.Delete (reqId : RequirementId) =
            task {
                let id = reqId |> RequirementId.value
                match store.TryRemove id with
                | true, _  -> return Ok ()
                | false, _ -> return Error (Domain (NotFound ("Requirement", string id)))
            }

