using System.Collections.Immutable;
using ConduitR.Visualizer.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

public sealed class VisualizerAnalyzerTests
{
    [Fact]
    public async Task Analyzer_reports_handler_for_send_call()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using ConduitR.Abstractions;

            public sealed record CreateOrder : IRequest<CreateOrderResult>;
            public sealed record CreateOrderResult;

            public sealed class CreateOrderHandler : IRequestHandler<CreateOrder, CreateOrderResult>
            {
                public ValueTask<CreateOrderResult> Handle(CreateOrder request, CancellationToken cancellationToken)
                {
                    return ValueTask.FromResult(new CreateOrderResult());
                }
            }

            public sealed class Endpoint
            {
                public ValueTask<CreateOrderResult> Handle(IMediator mediator)
                {
                    return mediator.Send(new CreateOrder());
                }
            }
            """;

        var diagnostics = await RunAnalyzerAsync(source);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(ConduitHandlerNavigationAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("CreateOrderHandler", diagnostic.GetMessage());
    }

    [Fact]
    public async Task Analyzer_reports_handler_for_create_stream_call()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Threading;
            using ConduitR;
            using ConduitR.Abstractions;

            public sealed record WatchOrders : IStreamRequest<OrderEvent>;
            public sealed record OrderEvent;

            public sealed class WatchOrdersHandler : IStreamRequestHandler<WatchOrders, OrderEvent>
            {
                public async IAsyncEnumerable<OrderEvent> Handle(WatchOrders request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
                {
                    yield return new OrderEvent();
                    await System.Threading.Tasks.Task.CompletedTask;
                }
            }

            public sealed class Endpoint
            {
                public IAsyncEnumerable<OrderEvent> Handle(Mediator mediator)
                {
                    return mediator.CreateStream(new WatchOrders());
                }
            }
            """;

        var diagnostics = await RunAnalyzerAsync(source);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(ConduitHandlerNavigationAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("WatchOrdersHandler", diagnostic.GetMessage());
    }

    private static async Task<ImmutableArray<Diagnostic>> RunAnalyzerAsync(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        var trustedPlatformAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")) ?? string.Empty;
        var references = trustedPlatformAssemblies
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Concat(new[]
            {
                MetadataReference.CreateFromFile(typeof(ConduitR.Abstractions.IMediator).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ConduitR.Mediator).Assembly.Location)
            })
            .ToArray();

        var compilation = CSharpCompilation.Create(
            "AnalyzerTests",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var compilationErrors = compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        Assert.True(compilationErrors.Length == 0, string.Join(Environment.NewLine, compilationErrors.Select(error => error.ToString())));

        var analyzer = new ConduitHandlerNavigationAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }
}
