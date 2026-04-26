using System.Diagnostics;
using ConduitR;
using ConduitR.Abstractions;
using Xunit;

public sealed class TelemetryTests
{
    [Fact]
    public async Task Send_activity_stays_open_until_handler_completes()
    {
        using var listener = CreateListener("conduitr.request_type", typeof(SlowRequest).FullName!, out var stopped);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var mediator = new Mediator(type =>
            type == typeof(IRequestHandler<SlowRequest, string>)
                ? new object[] { new SlowRequestHandler(gate) }
                : Array.Empty<object>());

        var sendTask = mediator.Send(new SlowRequest()).AsTask();
        await Task.Yield();

        Assert.DoesNotContain("Mediator.Send", stopped);

        gate.SetResult();
        await sendTask;

        Assert.Contains("Mediator.Send", stopped);
    }

    [Fact]
    public async Task Publish_activity_stays_open_until_handlers_complete()
    {
        using var listener = CreateListener("conduitr.notification_type", typeof(SlowNotification).FullName!, out var stopped);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handlers = new object[] { new SlowNotificationHandler(gate) };
        var mediator = new Mediator(type =>
            type == typeof(INotificationHandler<SlowNotification>) ||
            type == typeof(IEnumerable<INotificationHandler<SlowNotification>>)
                ? handlers
                : Array.Empty<object>());

        var publishTask = mediator.Publish(new SlowNotification());
        await Task.Yield();

        Assert.DoesNotContain("Mediator.Publish", stopped);

        gate.SetResult();
        await publishTask;

        Assert.Contains("Mediator.Publish", stopped);
    }

    [Fact]
    public async Task Stream_activity_stays_open_until_enumeration_completes()
    {
        using var listener = CreateListener("conduitr.request_type", typeof(SlowStreamRequest).FullName!, out var stopped);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var mediator = new Mediator(type =>
            type == typeof(IStreamRequestHandler<SlowStreamRequest, int>)
                ? new object[] { new SlowStreamHandler(gate) }
                : Array.Empty<object>());

        var stream = mediator.CreateStream(new SlowStreamRequest());
        await Task.Yield();

        Assert.DoesNotContain("Mediator.Stream", stopped);

        var results = new List<int>();
        await foreach (var item in stream)
        {
            results.Add(item);
            Assert.DoesNotContain("Mediator.Stream", stopped);
            gate.SetResult();
        }

        Assert.Equal(new[] { 1 }, results);
        Assert.Contains("Mediator.Stream", stopped);
    }

    private static ActivityListener CreateListener(string tagName, string tagValue, out List<string> stopped)
    {
        stopped = new List<string>();
        var captured = stopped;
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ConduitRTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                foreach (var tag in activity.TagObjects)
                {
                    if (tag.Key == tagName && string.Equals(tag.Value?.ToString(), tagValue, StringComparison.Ordinal))
                    {
                        captured.Add(activity.OperationName);
                        return;
                    }
                }
            }
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    public sealed record SlowRequest() : IRequest<string>;

    public sealed class SlowRequestHandler : IRequestHandler<SlowRequest, string>
    {
        private readonly TaskCompletionSource _gate;

        public SlowRequestHandler(TaskCompletionSource gate) => _gate = gate;

        public async ValueTask<string> Handle(SlowRequest request, CancellationToken cancellationToken)
        {
            await _gate.Task.WaitAsync(cancellationToken);
            return "done";
        }
    }

    public sealed record SlowNotification() : INotification;

    public sealed class SlowNotificationHandler : INotificationHandler<SlowNotification>
    {
        private readonly TaskCompletionSource _gate;

        public SlowNotificationHandler(TaskCompletionSource gate) => _gate = gate;

        public Task Handle(SlowNotification notification, CancellationToken cancellationToken)
            => _gate.Task.WaitAsync(cancellationToken);
    }

    public sealed record SlowStreamRequest() : IStreamRequest<int>;

    public sealed class SlowStreamHandler : IStreamRequestHandler<SlowStreamRequest, int>
    {
        private readonly TaskCompletionSource _gate;

        public SlowStreamHandler(TaskCompletionSource gate) => _gate = gate;

        public async IAsyncEnumerable<int> Handle(
            SlowStreamRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return 1;
            await _gate.Task.WaitAsync(cancellationToken);
        }
    }
}
