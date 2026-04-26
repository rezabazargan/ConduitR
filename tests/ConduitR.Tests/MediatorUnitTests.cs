using System;
using System.Threading;
using System.Threading.Tasks;
using ConduitR;
using ConduitR.Abstractions;
using Xunit;

public class MediatorUnitTests
{
    [Fact]
    public void Send_throws_when_request_is_null()
    {
        var mediator = new Mediator(_ => Array.Empty<object>());

        Assert.Throws<ArgumentNullException>(() => mediator.Send<string>(null!));
    }

    [Fact]
    public async Task Send_throws_when_no_handler_registered()
    {
        var mediator = new Mediator(_ => Array.Empty<object>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(new MissingRequest()).AsTask());
    }

    [Fact]
    public async Task Send_throws_when_multiple_handlers_registered()
    {
        var mediator = new Mediator(type => new object[] { new DuplicateHandler(), new DuplicateHandler() });

        await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(new DuplicateRequest()).AsTask());
    }

    [Fact]
    public void CreateStream_throws_when_request_is_null()
    {
        var mediator = new Mediator(_ => Array.Empty<object>());

        Assert.Throws<ArgumentNullException>(() => mediator.CreateStream<int>(null!));
    }

    [Fact]
    public void CreateStream_throws_when_multiple_stream_handlers_registered()
    {
        var mediator = new Mediator(type => new object[] { new DuplicateStreamHandler(), new DuplicateStreamHandler() });

        Assert.Throws<InvalidOperationException>(() => mediator.CreateStream(new DuplicateStreamRequest()));
    }

    [Fact]
    public async Task Publish_throws_when_notification_is_null()
    {
        var mediator = new Mediator(_ => Array.Empty<object>());

        await Assert.ThrowsAsync<ArgumentNullException>(() => mediator.Publish<MissingNotification>(null!));
    }

    public sealed record MissingRequest() : IRequest<string>;
    public sealed record DuplicateRequest() : IRequest<string>;
    public sealed record DuplicateStreamRequest() : IStreamRequest<int>;
    public sealed record MissingNotification() : INotification;

    public sealed class DuplicateHandler : IRequestHandler<DuplicateRequest, string>
    {
        public ValueTask<string> Handle(DuplicateRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(string.Empty);
    }

    public sealed class DuplicateStreamHandler : IStreamRequestHandler<DuplicateStreamRequest, int>
    {
        public async IAsyncEnumerable<int> Handle(DuplicateStreamRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            yield break;
        }
    }
}
