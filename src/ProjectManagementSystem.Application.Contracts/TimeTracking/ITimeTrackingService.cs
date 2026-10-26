using System.ServiceModel;
using ProtoBuf.Grpc;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.TimeTracking.Commands.LogTime;
using ProjectManagementSystem.Application.TimeTracking.Commands.RemoveTimeEntry;

namespace ProjectManagementSystem.Application.TimeTracking;

/// <summary>
/// gRPC service of the TaskTimeLog aggregate: one method per command. The host implements every
/// method by sending the command through MediatR; the client shares this interface for typed calls.
/// </summary>
[ServiceContract(Name = "ProjectManagementSystem.TimeTrackingService")]
public interface ITimeTrackingService
{
    Task<CreatedId> LogTime(LogTimeCommand command, CallContext context = default);

    Task RemoveTimeEntry(RemoveTimeEntryCommand command, CallContext context = default);
}
