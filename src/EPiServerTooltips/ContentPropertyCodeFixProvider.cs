using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace EPiServerTooltips
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ContentPropertyCodeFixProvider)), Shared]
    public class ContentPropertyCodeFixProvider : CodeFixProvider
    {
        private static readonly string Title = Resources.FixerTitle;

        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(ContentPropertyAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedDocument: c => AddVirtualKeywordAsync(context.Document, declaration, c),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static async Task<Document> AddVirtualKeywordAsync(Document document, PropertyDeclarationSyntax declaration, CancellationToken cancellationToken)
        {
            // Add virtual keyword
            PropertyDeclarationSyntax newDeclaration = declaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.VirtualKeyword));

            // Tell it to be formatted
            PropertyDeclarationSyntax formatted = newDeclaration.WithAdditionalAnnotations(Formatter.Annotation);

            // Replace old declaration
            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = oldRoot.ReplaceNode(declaration, formatted);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
