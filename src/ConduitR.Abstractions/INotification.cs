using System.Threading;
using System.Threading.Tasks;

namespace ConduitR.Abstractions;

/// <summary>Marker interface for a publish/subscribe notification.</summary>
public interface INotification {}

/// <summary>Handler for notifications.</summary>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
