using System.Runtime.Serialization;

namespace ProjectManagementSystem.Application.Projects;

[DataContract]
public sealed record ProjectRef(
    [property: DataMember(Order = 1)] Guid Id,
    [property: DataMember(Order = 2)] string Title)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private ProjectRef() : this(default, default!)
    {
    }
}
