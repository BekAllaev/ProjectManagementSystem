using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Projects;

namespace ProjectManagementSystem.Application.Projects.Queries.GetProjects;

/// <summary>
/// Projects for the list of projects, ordered by title.
/// </summary>
/// <param name="State">Only projects in this state; empty for all.</param>
/// <param name="Search">Only projects whose title contains this text (case-insensitive); empty for all.</param>
[DataContract]
public sealed record GetProjectsQuery(
    [property: DataMember(Order = 1)] ProjectState? State,
    [property: DataMember(Order = 2)] string? Search) : IQuery<ProjectList>
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private GetProjectsQuery() : this(default, default)
    {
    }
}
