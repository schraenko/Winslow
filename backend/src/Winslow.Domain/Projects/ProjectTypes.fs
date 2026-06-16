module Winslow.Domain.Projects.ProjectTypes

type ProjectStatus =
    | Planning
    | Active
    | OnHold
    | Completed
    | Cancelled

type ProjectMethodology =
    | Scrum
    | Kanban
    | Waterfall
    | Hybrid
