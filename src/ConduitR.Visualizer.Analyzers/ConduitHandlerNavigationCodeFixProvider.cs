using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace ConduitR.Visualizer.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConduitHandlerNavigationCodeFixProvider))]
[Shared]
public sealed class ConduitHandlerNavigationCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(ConduitHandlerNavigationAnalyzer.DiagnosticId);

    public override FixAllProvider? GetFixAllProvider() => null;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.FirstOrDefault();
        if (diagnostic is null ||
            !diagnostic.Properties.TryGetValue("ConduitR.HandlerFilePath", out var handlerFilePath) ||
            string.IsNullOrWhiteSpace(handlerFilePath))
        {
            return Task.CompletedTask;
        }

        var resolvedHandlerFilePath = handlerFilePath!;
        var handlerDocument = FindDocumentByPath(context.Document.Project.Solution, resolvedHandlerFilePath);
        if (handlerDocument is null)
        {
            return Task.CompletedTask;
        }

        var handlerName = diagnostic.Properties.TryGetValue("ConduitR.HandlerName", out var name) &&
            !string.IsNullOrWhiteSpace(name)
                ? name
                : "handler";
        var title = $"Go to ConduitR handler '{handlerName}'";

        context.RegisterCodeFix(
            new OpenHandlerDocumentCodeAction(title, handlerDocument.Id, resolvedHandlerFilePath),
            diagnostic);

        return Task.CompletedTask;
    }

    private static Document? FindDocumentByPath(Solution solution, string filePath)
    {
        foreach (var document in solution.Projects.SelectMany(project => project.Documents))
        {
            if (string.Equals(document.FilePath, filePath, System.StringComparison.OrdinalIgnoreCase))
            {
                return document;
            }
        }

        return null;
    }

    private sealed class OpenHandlerDocumentCodeAction : CodeAction
    {
        private readonly DocumentId _documentId;
        private readonly string _equivalenceKey;

        public OpenHandlerDocumentCodeAction(string title, DocumentId documentId, string handlerFilePath)
        {
            Title = title;
            _documentId = documentId;
            _equivalenceKey = $"{nameof(ConduitHandlerNavigationCodeFixProvider)}:{handlerFilePath}";
        }

        public override string Title { get; }

        public override string EquivalenceKey => _equivalenceKey;

        protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(
            CancellationToken cancellationToken)
        {
            IEnumerable<CodeActionOperation> operations = new[]
            {
                new OpenDocumentOperation(_documentId, true)
            };

            return Task.FromResult(operations);
        }
    }
}
