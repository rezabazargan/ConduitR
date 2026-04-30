using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ConduitR.Visualizer.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConduitHandlerNavigationAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CDRVS001";

    private static readonly DiagnosticDescriptor HandlerResolved = new(
        DiagnosticId,
        "ConduitR handler resolved",
        "ConduitR {0} '{1}' is handled by '{2}' at {3}",
        "ConduitR.Visualizer",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Shows the handler that receives a ConduitR Send or CreateStream request.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(HandlerResolved);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var handlerIndex = HandlerIndex.Create(compilationContext.Compilation);

            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeInvocation(operationContext, handlerIndex),
                OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, HandlerIndex handlerIndex)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var methodName = invocation.TargetMethod.Name;

        if (methodName is not ("Send" or "CreateStream"))
        {
            return;
        }

        if (invocation.Arguments.Length == 0)
        {
            return;
        }

        var requestValue = UnwrapConversion(invocation.Arguments[0].Value);
        var requestType = requestValue.Type as INamedTypeSymbol;
        if (requestType is null)
        {
            return;
        }

        var isStream = methodName == "CreateStream";
        var handlerType = handlerIndex.FindHandler(requestType, isStream);
        if (handlerType is null)
        {
            return;
        }

        var kind = isStream ? "stream request" : "request";
        var handlerLocation = handlerType.Locations.FirstOrDefault(location => location.IsInSource);
        var additionalLocations = handlerLocation is null
            ? ImmutableArray<Location>.Empty
            : ImmutableArray.Create(handlerLocation);
        var properties = ImmutableDictionary<string, string?>.Empty
            .Add("ConduitR.HandlerName", handlerType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat))
            .Add("ConduitR.HandlerFullName", handlerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        if (handlerLocation is not null)
        {
            var lineSpan = handlerLocation.GetLineSpan();
            properties = properties
                .Add("ConduitR.HandlerFilePath", lineSpan.Path)
                .Add("ConduitR.HandlerLine", (lineSpan.StartLinePosition.Line + 1).ToString());
        }

        var diagnostic = Diagnostic.Create(
            HandlerResolved,
            invocation.Syntax.GetLocation(),
            additionalLocations,
            properties,
            kind,
            requestType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            handlerType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            FormatLocation(handlerLocation));

        context.ReportDiagnostic(diagnostic);
    }

    private static IOperation UnwrapConversion(IOperation operation)
    {
        while (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        return operation;
    }

    private static string FormatLocation(Location? location)
    {
        if (location is null || !location.IsInSource)
        {
            return "source unavailable";
        }

        var lineSpan = location.GetLineSpan();
        var fileName = System.IO.Path.GetFileName(lineSpan.Path);
        return $"{fileName}:{lineSpan.StartLinePosition.Line + 1}";
    }

    private sealed class HandlerIndex
    {
        private readonly ImmutableArray<HandlerRegistration> _requestHandlers;
        private readonly ImmutableArray<HandlerRegistration> _streamHandlers;

        private HandlerIndex(
            ImmutableArray<HandlerRegistration> requestHandlers,
            ImmutableArray<HandlerRegistration> streamHandlers)
        {
            _requestHandlers = requestHandlers;
            _streamHandlers = streamHandlers;
        }

        public static HandlerIndex Create(Compilation compilation)
        {
            var requestHandlers = ImmutableArray.CreateBuilder<HandlerRegistration>();
            var streamHandlers = ImmutableArray.CreateBuilder<HandlerRegistration>();

            foreach (var type in EnumerateTypes(compilation.GlobalNamespace))
            {
                foreach (var implementedInterface in type.AllInterfaces)
                {
                    if (implementedInterface.TypeArguments.Length < 1)
                    {
                        continue;
                    }

                    if (IsConduitInterface(implementedInterface, "IRequestHandler", arity: 2))
                    {
                        requestHandlers.Add(new HandlerRegistration(
                            implementedInterface.TypeArguments[0],
                            type));
                    }
                    else if (IsConduitInterface(implementedInterface, "IStreamRequestHandler", arity: 2))
                    {
                        streamHandlers.Add(new HandlerRegistration(
                            implementedInterface.TypeArguments[0],
                            type));
                    }
                }
            }

            return new HandlerIndex(requestHandlers.ToImmutable(), streamHandlers.ToImmutable());
        }

        public INamedTypeSymbol? FindHandler(INamedTypeSymbol requestType, bool isStream)
        {
            var handlers = isStream ? _streamHandlers : _requestHandlers;
            return handlers.FirstOrDefault(handler =>
                SymbolEqualityComparer.Default.Equals(handler.RequestType, requestType)).HandlerType;
        }

        private static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceOrTypeSymbol symbol)
        {
            foreach (var member in symbol.GetMembers())
            {
                if (member is INamespaceOrTypeSymbol namespaceOrType)
                {
                    foreach (var nestedType in EnumerateTypes(namespaceOrType))
                    {
                        yield return nestedType;
                    }
                }

                if (member is INamedTypeSymbol namedType)
                {
                    yield return namedType;
                }
            }
        }

        private static bool IsConduitInterface(INamedTypeSymbol symbol, string name, int arity) =>
            symbol.Name == name &&
            symbol.Arity == arity &&
            symbol.ContainingNamespace.ToDisplayString() == "ConduitR.Abstractions";
    }

    private readonly struct HandlerRegistration
    {
        public HandlerRegistration(ITypeSymbol requestType, INamedTypeSymbol handlerType)
        {
            RequestType = requestType;
            HandlerType = handlerType;
        }

        public ITypeSymbol RequestType { get; }

        public INamedTypeSymbol HandlerType { get; }
    }
}
