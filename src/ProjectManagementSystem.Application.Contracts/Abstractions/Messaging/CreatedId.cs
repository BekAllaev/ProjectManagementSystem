using System.Runtime.Serialization;

namespace ProjectManagementSystem.Application.Abstractions.Messaging;

/// <summary>
/// Result of a command that creates something: the id of the new aggregate or inner entity. A gRPC response must be
/// a message, so a bare <see cref="Guid"/> is not enough.
/// </summary>
[DataContract]
public sealed record CreatedId([property: DataMember(Order = 1)] Guid Id)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private CreatedId() : this(Guid.Empty)
    {
    }
}
