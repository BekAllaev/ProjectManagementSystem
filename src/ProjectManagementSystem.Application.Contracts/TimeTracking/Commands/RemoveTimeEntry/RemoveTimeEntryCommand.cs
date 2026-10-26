using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.TimeTracking.Commands.RemoveTimeEntry;

[DataContract]
public sealed record RemoveTimeEntryCommand(
    [property: DataMember(Order = 1)] Guid TaskId,
    [property: DataMember(Order = 2)] Guid EntryId) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private RemoveTimeEntryCommand() : this(default, default)
    {
    }
}
