using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using EPiServerTooltips.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EPiServerTooltips
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ContentPropertyAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "EPiServerTooltips";

        private static readonly LocalizableString Title = nameof(Resources.AnalyzerTitle).GetLocalizableString();
        private static readonly LocalizableString MessageFormat = nameof(Resources.AnalyzerMessageFormat).GetLocalizableString();
        private static readonly LocalizableString Description = nameof(Resources.AnalyzerDescription).GetLocalizableString();
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzePropertyNode, SyntaxKind.PropertyDeclaration);
        }

        private static void AnalyzePropertyNode(SyntaxNodeAnalysisContext context)
        {
            var declaration = (PropertyDeclarationSyntax)context.Node;

            // Filter by modifier
            foreach (SyntaxToken modifier in declaration.Modifiers)
            {
                if (modifier.IsKind(SyntaxKind.VirtualKeyword) ||
                    modifier.IsKind(SyntaxKind.OverrideKeyword) ||
                    modifier.IsKind(SyntaxKind.AbstractKeyword) ||
                    !modifier.IsKind(SyntaxKind.PublicKeyword) ||
                    modifier.IsKind(SyntaxKind.StaticKeyword))
                    return;
            }

            // Skip if not having get and set
            AccessorDeclarationSyntax getAccessor = declaration.AccessorList.Accessors
                .FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
            if (getAccessor == null) return;
            AccessorDeclarationSyntax setAccessor = declaration.AccessorList.Accessors
                .FirstOrDefault(a => a.IsKind(SyntaxKind.SetAccessorDeclaration));
            if (setAccessor == null) return;

            // Skip if not both accessors are public
            if (getAccessor.Modifiers.Any())
                return;
            if (setAccessor.Modifiers.Any())
                return;

            // Skip if not IContent
            var classSymbol = (INamedTypeSymbol) context.SemanticModel.GetDeclaredSymbol(declaration.Parent);
            if (IsDerivedFromInterface(classSymbol, EPiServerTypes.IContent) == false)
                return;

            // Skip if ignored
            IPropertySymbol declaredSymbol = context.SemanticModel.GetDeclaredSymbol(declaration);
            if (HasAttribute(declaredSymbol, EPiServerTypes.IgnoreAttribute))
                return;

            // It's a faulty one
            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), declaration.Identifier.Text));
        }

        private static bool HasAttribute(IPropertySymbol propertySymbol, in string attributeFullName)
        {
            while (true)
            {
                ImmutableArray<AttributeData> attributes = propertySymbol.GetAttributes();
                foreach (AttributeData attribute in attributes)
                {
                    if (attribute.ToString() == attributeFullName) return true;
                }

                if (!propertySymbol.IsOverride) return false;

                propertySymbol = propertySymbol.OverriddenProperty;
            }
        }

        private static bool IsDerivedFromInterface(in INamedTypeSymbol classSymbol, in string interfaceTypeName)
        {
            ImmutableArray<INamedTypeSymbol> interfaces = classSymbol.AllInterfaces;

            foreach (INamedTypeSymbol symbol in interfaces)
            {
                if (string.Equals(symbol.ToString(), interfaceTypeName, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
