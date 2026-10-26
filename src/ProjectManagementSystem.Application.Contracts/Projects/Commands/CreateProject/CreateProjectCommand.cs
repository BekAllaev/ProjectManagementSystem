using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Projects.Commands.CreateProject;

/// <param name="Currency">ISO 4217 code of the currency of all money of the project.</param>
/// <returns>Id of the new project.</returns>
[DataContract]
public sealed record CreateProjectCommand(
    [property: DataMember(Order = 1)] string Title,
    [property: DataMember(Order = 2)] string? Description,
    [property: DataMember(Order = 3)] string Currency) : ICommand<CreatedId>
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private CreateProjectCommand() : this(default!, default, default!)
    {
    }
}
