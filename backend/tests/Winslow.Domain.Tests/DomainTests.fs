module Winslow.Domain.Tests

open System
open Xunit
open Winslow.Domain.Common.Types
open Winslow.Domain.Common.Errors
open Winslow.Domain.BacklogItems.BacklogItemTypes
open Winslow.Domain.BacklogItems.BacklogItemEvents
open Winslow.Domain.Sprints.SprintTypes
open Winslow.Domain.Sprints.SprintEvents

module Bli = Winslow.Domain.BacklogItems.BacklogItem
module Sp  = Winslow.Domain.Sprints.Sprint

// ── Helpers ────────────────────────────────────────────────────────────────────

let demoProjectId = ProjectId (Guid "00000000-0000-0000-0000-000000000001")
let demoAuthorId  = UserId (Guid "00000000-0000-0000-0000-000000000099")
let demoSprintId  = SprintId (Guid "00000000-0000-0000-0000-000000000072")

let mkCreateInput itemType title parentId : Bli.CreateBacklogItemInput = {
    ProjectId     = demoProjectId
    ParentId      = parentId
    ParentType    = None
    ItemType      = itemType
    Title         = title
    Description   = "test"
    Priority      = 50
    StoryPoints   = None
    Effort        = None
    AuthorId      = demoAuthorId
}

let OkResult (r : Result<'a, _>) = match r with Ok v -> v | Error e -> failwithf "Expected Ok but got Error: %A" e

// ── Hierarchy Validation ───────────────────────────────────────────────────────

[<Fact>]
let ``Epic can be created without parent`` () =
    Assert.True(validateHierarchy Epic None |> Result.isOk)

[<Fact>]
let ``Epic cannot have a parent`` () =
    Assert.True(validateHierarchy Epic (Some Feature) |> Result.isError)

[<Fact>]
let ``Feature requires Epic parent`` () =
    Assert.True(validateHierarchy Feature (Some Epic) |> Result.isOk)
    Assert.True(validateHierarchy Feature None |> Result.isError)
    Assert.True(validateHierarchy Feature (Some Feature) |> Result.isError)

[<Fact>]
let ``PBI requires Feature parent`` () =
    Assert.True(validateHierarchy PBI (Some Feature) |> Result.isOk)
    Assert.True(validateHierarchy PBI None |> Result.isError)

[<Fact>]
let ``Task requires PBI, Bug or Impediment parent`` () =
    Assert.True(validateHierarchy Task (Some PBI) |> Result.isOk)
    Assert.True(validateHierarchy Task (Some Bug) |> Result.isOk)
    Assert.True(validateHierarchy Task (Some Impediment) |> Result.isOk)
    Assert.True(validateHierarchy Task (Some Feature) |> Result.isError)
    Assert.True(validateHierarchy Task None |> Result.isError)

[<Fact>]
let ``Bug requires Feature parent`` () =
    Assert.True(validateHierarchy Bug (Some Feature) |> Result.isOk)
    Assert.True(validateHierarchy Bug None |> Result.isError)

[<Fact>]
let ``Impediment requires Feature parent`` () =
    Assert.True(validateHierarchy Impediment (Some Feature) |> Result.isOk)
    Assert.True(validateHierarchy Impediment None |> Result.isError)

// ── BacklogItem create ─────────────────────────────────────────────────────────

[<Fact>]
let ``Create Epic succeeds`` () =
    let input = mkCreateInput Epic "Big Epic" None
    match Bli.create input with
    | Ok (item, event) ->
        Assert.Equal("Big Epic", Bli.title item |> NonEmptyString.value)
        Assert.Equal(Epic, Bli.itemType item)
        Assert.Equal(Open, Bli.status item)
        Assert.True(Option.isNone (Bli.parentId item))
        match event with
        | BacklogItemCreated e ->
            Assert.Equal("Big Epic", e.Title)
            Assert.Equal(Epic, e.ItemType)
        | _ -> Assert.Fail("Expected BacklogItemCreated event")
    | Error e -> Assert.Fail($"Expected Ok but got Error: {e}")

[<Fact>]
let ``Create Feature under Epic succeeds`` () =
    let epic = Bli.create (mkCreateInput Epic "Epic" None) |> OkResult |> fst
    let input = { mkCreateInput Feature "A Feature" (Some (Bli.id epic)) with ParentType = Some Epic }
    match Bli.create input with
    | Ok (item, _) ->
        Assert.Equal(Feature, Bli.itemType item)
        Assert.Equal(Some (Bli.id epic), Bli.parentId item)
    | Error e -> Assert.Fail($"Expected Ok but got Error: {e}")

[<Fact>]
let ``Create rejects empty title`` () =
    match Bli.create (mkCreateInput Epic "" None) with
    | Error (ValidationError (field, _)) -> Assert.Equal("Title", field)
    | Error e -> Assert.Fail($"Expected ValidationError for Title but got: {e}")
    | Ok _ -> Assert.Fail("Expected Error but got Ok")

[<Fact>]
let ``Create PBI without parent is rejected`` () =
    Assert.True(Bli.create (mkCreateInput PBI "Orphan PBI" None) |> Result.isError)

[<Fact>]
let ``Create Task under PBI succeeds`` () =
    let epic = Bli.create (mkCreateInput Epic "E" None) |> OkResult |> fst
    let feature = Bli.create { mkCreateInput Feature "F" (Some (Bli.id epic)) with ParentType = Some Epic } |> OkResult |> fst
    let pbi = Bli.create { mkCreateInput PBI "My PBI" (Some (Bli.id feature)) with ParentType = Some Feature } |> OkResult |> fst
    let input = { mkCreateInput Task "A Task" (Some (Bli.id pbi)) with ParentType = Some PBI }
    match Bli.create input with
    | Ok (item, _) ->
        Assert.Equal(Task, Bli.itemType item)
        Assert.Equal(Some (Bli.id pbi), Bli.parentId item)
    | Error e -> Assert.Fail($"Expected Ok but got Error: {e}")

// ── Status transitions (BacklogItem) ───────────────────────────────────────────

[<Fact>]
let ``Transition to different status succeeds`` () =
    let epic = Bli.create (mkCreateInput Epic "E" None) |> OkResult |> fst
    match Bli.transitionStatus InProgress epic with
    | Ok (updated, event) ->
        Assert.Equal(InProgress, Bli.status updated)
        match event with
        | BacklogItemStatusChanged e ->
            Assert.Equal(Open, e.From)
            Assert.Equal(InProgress, e.To)
        | _ -> Assert.Fail("Expected BacklogItemStatusChanged")
    | Error e -> Assert.Fail($"Expected Ok: {e}")

[<Fact>]
let ``Transition to same status is rejected`` () =
    let epic = Bli.create (mkCreateInput Epic "E" None) |> OkResult |> fst
    match Bli.transitionStatus Open epic with
    | Error (ValidationError (field, _)) -> Assert.Equal("Status", field)
    | _ -> Assert.Fail("Expected ValidationError")

[<Fact>]
let ``Chain transitions: Open -> InProgress -> Done -> Cancelled`` () =
    let epic = Bli.create (mkCreateInput Epic "E" None) |> OkResult |> fst
    let step1 = Bli.transitionStatus InProgress epic |> OkResult |> fst
    Assert.Equal(InProgress, Bli.status step1)
    let step2 = Bli.transitionStatus Done step1 |> OkResult |> fst
    Assert.Equal(Done, Bli.status step2)
    let step3 = Bli.transitionStatus Cancelled step2 |> OkResult |> fst
    Assert.Equal(Cancelled, Bli.status step3)

// ── Assign to sprint ───────────────────────────────────────────────────────────

[<Fact>]
let ``PBI can be assigned to sprint`` () =
    let epic = Bli.create (mkCreateInput Epic "E" None) |> OkResult |> fst
    let feature = Bli.create { mkCreateInput Feature "F" (Some (Bli.id epic)) with ParentType = Some Epic } |> OkResult |> fst
    let pbi = Bli.create { mkCreateInput PBI "P" (Some (Bli.id feature)) with ParentType = Some Feature } |> OkResult |> fst
    match Bli.assignToSprint demoSprintId pbi with
    | Ok (updated, event) ->
        Assert.Equal(Some demoSprintId, Bli.sprintId updated)
        match event with
        | BacklogItemAssignedToSprint e -> Assert.Equal(demoSprintId, e.SprintId)
        | _ -> Assert.Fail("Expected BacklogItemAssignedToSprint")
    | Error e -> Assert.Fail($"Expected Ok: {e}")

[<Fact>]
let ``Epic cannot be assigned to sprint`` () =
    let epic = Bli.create (mkCreateInput Epic "E" None) |> OkResult |> fst
    Assert.True(Bli.assignToSprint demoSprintId epic |> Result.isError)

[<Fact>]
let ``Feature cannot be assigned to sprint`` () =
    let epic = Bli.create (mkCreateInput Epic "E" None) |> OkResult |> fst
    let feature = Bli.create { mkCreateInput Feature "F" (Some (Bli.id epic)) with ParentType = Some Epic } |> OkResult |> fst
    Assert.True(Bli.assignToSprint demoSprintId feature |> Result.isError)

[<Fact>]
let ``Remove from sprint clears sprintId`` () =
    let epic = Bli.create (mkCreateInput Epic "E" None) |> OkResult |> fst
    let feature = Bli.create { mkCreateInput Feature "F" (Some (Bli.id epic)) with ParentType = Some Epic } |> OkResult |> fst
    let pbi = Bli.create { mkCreateInput PBI "P" (Some (Bli.id feature)) with ParentType = Some Feature } |> OkResult |> fst
    let assigned = Bli.assignToSprint demoSprintId pbi |> OkResult |> fst
    match Bli.removeFromSprint assigned with
    | Ok (updated, event) ->
        Assert.True(Option.isNone (Bli.sprintId updated))
        match event with
        | BacklogItemRemovedFromSprint _ -> ()
        | _ -> Assert.Fail("Expected BacklogItemRemovedFromSprint")
    | Error e -> Assert.Fail($"Expected Ok: {e}")

// ── Sprint create ──────────────────────────────────────────────────────────────

let mkSprintInput name goal startDate endDate : Sp.CreateSprintInput = {
    ProjectId = demoProjectId
    Name      = name
    Goal      = goal
    StartDate = startDate
    EndDate   = endDate
}

[<Fact>]
let ``Create sprint with valid dates succeeds`` () =
    let input = mkSprintInput "Sprint 1" "Deliver MVP" (DateTime(2025, 6, 1)) (DateTime(2025, 6, 14))
    match Sp.create input with
    | Ok (s, event) ->
        Assert.Equal("Sprint 1", Sp.name s |> NonEmptyString.value)
        Assert.Equal(Planned, Sp.status s)
        match event with
        | SprintCreated e -> Assert.Equal("Sprint 1", e.Name)
        | _ -> Assert.Fail("Expected SprintCreated")
    | Error e -> Assert.Fail($"Expected Ok: {e}")

[<Fact>]
let ``Create sprint with end before start is rejected`` () =
    let input = mkSprintInput "Bad" "" (DateTime(2025, 6, 14)) (DateTime(2025, 6, 1))
    match Sp.create input with
    | Error (ValidationError (field, _)) -> Assert.Equal("EndDate", field)
    | _ -> Assert.Fail("Expected ValidationError")

[<Fact>]
let ``Create sprint with equal dates is rejected`` () =
    Assert.True(Sp.create (mkSprintInput "Bad" "" (DateTime(2025, 6, 1)) (DateTime(2025, 6, 1))) |> Result.isError)

[<Fact>]
let ``Create sprint rejects empty name`` () =
    Assert.True(Sp.create (mkSprintInput "" "" (DateTime(2025, 6, 1)) (DateTime(2025, 6, 14))) |> Result.isError)

// ── Sprint transitions ─────────────────────────────────────────────────────────

[<Fact>]
let ``Sprint transitions: Planned -> Active -> Completed`` () =
    let s = Sp.create (mkSprintInput "S" "" (DateTime(2025, 6, 1)) (DateTime(2025, 6, 14))) |> OkResult |> fst
    let active = Sp.transitionStatus Active s |> OkResult |> fst
    Assert.Equal(Active, Sp.status active)
    let completed = Sp.transitionStatus Completed active |> OkResult |> fst
    Assert.Equal(Completed, Sp.status completed)

[<Fact>]
let ``Sprint cannot skip from Planned to Completed`` () =
    let s = Sp.create (mkSprintInput "S" "" (DateTime(2025, 6, 1)) (DateTime(2025, 6, 14))) |> OkResult |> fst
    Assert.True(Sp.transitionStatus Completed s |> Result.isError)

[<Fact>]
let ``Completed sprint cannot transition`` () =
    let s = Sp.create (mkSprintInput "S" "" (DateTime(2025, 6, 1)) (DateTime(2025, 6, 14))) |> OkResult |> fst
    let completed = s |> Sp.transitionStatus Active |> OkResult |> fst |> Sp.transitionStatus Completed |> OkResult |> fst
    Assert.True(Sp.transitionStatus Active completed |> Result.isError)
    Assert.True(Sp.transitionStatus Planned completed |> Result.isError)
