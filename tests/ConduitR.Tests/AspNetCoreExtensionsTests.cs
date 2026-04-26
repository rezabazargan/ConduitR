using ConduitR;
using ConduitR.AspNetCore;
using ConduitR.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

public class AspNetCoreExtensionsTests
{
    [Fact]
    public void AddConduitProblemDetails_registers_options_to_service_collection()
    {
        var services = new ServiceCollection();
        services.AddConduitProblemDetails(o => o.StatusCodeMap[typeof(ArgumentNullException)] = StatusCodes.Status418ImATeapot);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ConduitProblemDetailsOptions>>().Value;

        Assert.Equal(StatusCodes.Status418ImATeapot, options.StatusCodeMap[typeof(ArgumentNullException)]);
    }

    [Fact]
    public void MapMediatorPost_builds_route_handler()
    {
        var provider = new ServiceCollection().BuildServiceProvider();
        var routeBuilder = new FakeEndpointRouteBuilder(provider);

        var result = routeBuilder.MapMediatorPost<DummyRequest, string>("/dummy");

        Assert.NotNull(result);
    }

    public sealed record DummyRequest(string Value) : IRequest<string>;

    private sealed class FakeEndpointRouteBuilder : IEndpointRouteBuilder
    {
        public FakeEndpointRouteBuilder(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            DataSources = new List<EndpointDataSource>();
        }

        public IServiceProvider ServiceProvider { get; }
        public ICollection<EndpointDataSource> DataSources { get; }

        public IApplicationBuilder CreateApplicationBuilder() => new ApplicationBuilder(ServiceProvider);
    }
}
