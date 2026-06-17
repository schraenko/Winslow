module Winslow.Infrastructure.InMemoryBacklogItemRepository

open System
open System.Collections.Concurrent
open System.Threading.Tasks
open Winslow.Domain.Common.Types
open Winslow.Domain.Common.Errors
open Winslow.Domain.BacklogItems.BacklogItem
open Winslow.Domain.BacklogItems.BacklogItemTypes
open Winslow.Application.Common.Ports

let create () : BacklogItemRepository =
    let store = ConcurrentDictionary<Guid, BacklogItem>()

    let now = Timestamp.now ()
    let demoProjectId = ProjectId (Guid.Parse "00000000-0000-0000-0000-000000000001")
    let demoAuthorId  = UserId (Guid.Parse "00000000-0000-0000-0000-000000000099")

    let seed
        (id            : string)
        (parentId      : string option)
        (itemType      : BacklogItemType)
        (title         : string)
        (description   : string)
        (priority      : int)
        (storyPoints   : int option)
        (effort        : float option)
        (status        : BacklogItemStatus)
        (sprintId      : string option)
        : BacklogItem =
        match NonEmptyString.create "Title" title with
        | Ok t ->
            let pid     = parentId |> Option.map (fun p -> BacklogItemId (Guid.Parse p))
            let sprint  = sprintId |> Option.map (fun s -> SprintId (Guid.Parse s))
            hydrate
                (BacklogItemId (Guid.Parse id))
                demoProjectId
                pid
                itemType
                t
                description
                priority
                storyPoints
                effort
                status
                None
                sprint
                None
                demoAuthorId
                now
                now
        | _ -> failwith "invalid seed data"

    // Epic
    store.TryAdd(Guid "00000000-0000-0000-0000-000000000011",
        seed "00000000-0000-0000-0000-000000000011" None Epic
            "Benutzerverwaltung" "Benutzerkonten, Anmeldung und Profile verwalten."
            100 None None Open None)
    |> ignore

    // Features under Epic
    store.TryAdd(Guid "00000000-0000-0000-0000-000000000021",
        seed "00000000-0000-0000-0000-000000000021"
            (Some "00000000-0000-0000-0000-000000000011")
            Feature "Registrierung" "Neue Benutzer registrieren."
            100 None None Open None)
    |> ignore

    store.TryAdd(Guid "00000000-0000-0000-0000-000000000022",
        seed "00000000-0000-0000-0000-000000000022"
            (Some "00000000-0000-0000-0000-000000000011")
            Feature "Anmeldung" "Benutzer anmelden und abmelden."
            90 None None Open None)
    |> ignore

    // PBIs under Feature "Registrierung"
    store.TryAdd(Guid "00000000-0000-0000-0000-000000000031",
        seed "00000000-0000-0000-0000-000000000031"
            (Some "00000000-0000-0000-0000-000000000021")
            PBI "Registrierungsformular" "E-Mail, Passwort, Name eingeben."
            100 (Some 5) None Open None)
    |> ignore

    store.TryAdd(Guid "00000000-0000-0000-0000-000000000032",
        seed "00000000-0000-0000-0000-000000000032"
            (Some "00000000-0000-0000-0000-000000000021")
            PBI "E-Mail-Bestätigung" "Bestätigungs-E-Mail senden und verifizieren."
            80 (Some 3) None Open None)
    |> ignore

    // Tasks under PBI "Registrierungsformular"
    store.TryAdd(Guid "00000000-0000-0000-0000-000000000041",
        seed "00000000-0000-0000-0000-000000000041"
            (Some "00000000-0000-0000-0000-000000000031")
            Task "UI: Formular erstellen" "HTML-Formular mit Validierung."
            100 None (Some 4.0) Open None)
    |> ignore

    store.TryAdd(Guid "00000000-0000-0000-0000-000000000042",
        seed "00000000-0000-0000-0000-000000000042"
            (Some "00000000-0000-0000-0000-000000000031")
            Task "API: Register-Endpunkt" "POST /api/register mit Validierung."
            100 None (Some 8.0) Open None)
    |> ignore

    // Bug under Feature "Anmeldung"
    store.TryAdd(Guid "00000000-0000-0000-0000-000000000051",
        seed "00000000-0000-0000-0000-000000000051"
            (Some "00000000-0000-0000-0000-000000000022")
            Bug "Passwort-Reset-Token abgelaufen" "Token läuft schon nach 1 Minute statt 15."
            90 None None Open None)
    |> ignore

    // Impediment under Feature "Anmeldung"
    store.TryAdd(Guid "00000000-0000-0000-0000-000000000061",
        seed "00000000-0000-0000-0000-000000000061"
            (Some "00000000-0000-0000-0000-000000000022")
            Impediment "SMTP-Server nicht verfügbar" "Der Mailserver ist seit 2 Tagen down."
            100 None None Open None)
    |> ignore

    {   FindById = fun (id : BacklogItemId) ->
            task {
                let g = id |> BacklogItemId.value
                match store.TryGetValue g with
                | true, item -> return Ok item
                | false, _   -> return Error (Domain (NotFound ("BacklogItem", string g)))
            }

        FindByProject = fun (projId : ProjectId) ->
            task {
                let pid = projId |> ProjectId.value
                let items =
                    store.Values
                    |> Seq.filter (fun i -> (projectId i |> ProjectId.value) = pid)
                    |> Seq.toList
                return Ok items
            }

        FindBySprint = fun (sid : SprintId) ->
            task {
                let target = sid |> SprintId.value
                let items =
                    store.Values
                    |> Seq.filter (fun i ->
                        match sprintId i with
                        | Some s -> (s |> SprintId.value) = target
                        | None   -> false)
                    |> Seq.toList
                return Ok items
            }

        FindChildren = fun (pid : BacklogItemId) ->
            task {
                let target = pid |> BacklogItemId.value
                let items =
                    store.Values
                    |> Seq.filter (fun i ->
                        match parentId i with
                        | Some p -> (p |> BacklogItemId.value) = target
                        | None   -> false)
                    |> Seq.toList
                return Ok items
            }

        Save = fun (item : BacklogItem) ->
            task {
                let g = id item |> BacklogItemId.value
                store.AddOrUpdate(g, item, fun _ _ -> item) |> ignore
                return Ok ()
            }

        Delete = fun (itemId : BacklogItemId) ->
            task {
                let g = itemId |> BacklogItemId.value
                match store.TryRemove g with
                | true, _  -> return Ok ()
                | false, _ -> return Error (Domain (NotFound ("BacklogItem", string g)))
            }
    }
