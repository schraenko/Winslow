module Winslow.Domain.Projects.Project

open Winslow.Domain.Common.Types
open Winslow.Domain.Common.Errors
open Winslow.Domain.Projects.ProjectTypes

type Project = {
    Id          : ProjectId
    Name        : NonEmptyString
    Description : string
    Status      : ProjectStatus
    Methodology : ProjectMethodology
    OwnerId     : UserId
    CreatedAt   : Timestamp
    UpdatedAt   : Timestamp
}

type CreateProjectInput = {
    Name        : string
    Description : string
    Methodology : ProjectMethodology
    OwnerId     : UserId
}

let create (input: CreateProjectInput) : Result<Project, DomainError> =
    result {
        let! name = NonEmptyString.create "Projektname" input.Name
        let now   = Timestamp.now ()
        return {
            Id          = ProjectId.create ()
            Name        = name
            Description = input.Description
            Status      = Planning
            Methodology = input.Methodology
            OwnerId     = input.OwnerId
            CreatedAt   = now
            UpdatedAt   = now
        }
    }
