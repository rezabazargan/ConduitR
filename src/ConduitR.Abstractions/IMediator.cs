using System.Threading;
using System.Threading.Tasks;

namespace ConduitR.Abstractions;

public interface IMediator
{
    ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification;
}
