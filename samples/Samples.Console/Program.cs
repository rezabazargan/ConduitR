using System.Reflection;
using ConduitR.Abstractions;
using ConduitR.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddConduit(cfg =>
        {
            cfg.AddHandlersFromAssemblies(Assembly.GetExecutingAssembly());
        });
    })
    .Build();

var mediator = host.Services.GetRequiredService<IMediator>();
var result = await mediator.Send(new Ping("world"));
Console.WriteLine(result);

public sealed record Ping(string Name) : IRequest<string>;

public sealed class PingHandler : IRequestHandler<Ping, string>
{
    public ValueTask<string> Handle(Ping request, CancellationToken cancellationToken)
        => ValueTask.FromResult($"Hello, {request.Name}!");
}
