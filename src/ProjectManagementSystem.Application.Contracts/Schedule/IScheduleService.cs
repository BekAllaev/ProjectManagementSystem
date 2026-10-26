using System.ServiceModel;
using ProtoBuf.Grpc;
using ProjectManagementSystem.Application.Schedule.Commands.AddDependency;
using ProjectManagementSystem.Application.Schedule.Commands.RemoveDependency;

namespace ProjectManagementSystem.Application.Schedule;

/// <summary>
/// gRPC service of the ScheduleNetwork aggregate: one method per command. The host implements every
/// method by sending the command through MediatR; the client shares this interface for typed calls.
/// </summary>
[ServiceContract(Name = "ProjectManagementSystem.ScheduleService")]
public interface IScheduleService
{
    Task AddDependency(AddDependencyCommand command, CallContext context = default);

    Task RemoveDependency(RemoveDependencyCommand command, CallContext context = default);
}
