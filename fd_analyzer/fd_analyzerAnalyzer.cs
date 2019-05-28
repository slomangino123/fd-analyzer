using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using fd_analyzer.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static fd_analyzer.DiagnosticId.DiagnosticIds;

namespace fd_analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class fd_analyzerAnalyzer : DiagnosticAnalyzer
    {
        private static DiagnosticDescriptor EmptyConstructorRule = new DiagnosticDescriptor(EmptyConstructorDiagnosticId, Resources.EmptyConstructorTitle, Resources.AggregateMessageFormat, Resources.ConstructorCategory, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Resources.EmptyConstructorDescription);
        private static DiagnosticDescriptor ConstructorsWithParametersShouldBePrivate = new DiagnosticDescriptor(PrivateConstructorDiagnosticId, Resources.PrivateConstructorTitle, Resources.AggregateMessageFormat, Resources.ConstructorCategory, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Resources.PrivateConstructorDescription);
        private static DiagnosticDescriptor ApplyMethodsShouldBePublic = new DiagnosticDescriptor(PublicApplyMethodsDiagnosticId, Resources.PublicApplyMethodsTitle, Resources.AggregateMessageFormat, Resources.ConstructorCategory, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Resources.PublicApplyMethodsDescription);
        private static DiagnosticDescriptor PublicConstructorRule = new DiagnosticDescriptor(PublicConstructorDiagnosticId, Resources.PublicConstructorTitle, Resources.AggregateMessageFormat, Resources.ConstructorCategory, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Resources.PublicConstructorDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(EmptyConstructorRule,
                                             ConstructorsWithParametersShouldBePrivate,
                                             ApplyMethodsShouldBePublic,
                                             PublicConstructorRule);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information

            context.RegisterSyntaxNodeAction(AnalyzeAggregateConstructors, SyntaxKind.ConstructorDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeAggregateApplyMethods, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeAggregateApplyMethods(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;
            var methodStringIdentifier = methodDeclaration.Identifier.ToString();

            if (!methodStringIdentifier.Equals("Apply"))
            {
                return;
            }

            var parentClass = (ClassDeclarationSyntax)methodDeclaration.Parent;

            if (!AggregateAnalyzerExtensions.ParentClassInheritsFromAggregateRoot(parentClass))
            {
                return;
            }

            if (!methodDeclaration.Modifiers.Any(x => x.IsKind(SyntaxKind.PublicKeyword)))
            {
                var diagnostic = Diagnostic.Create(ApplyMethodsShouldBePublic,
                                                   methodDeclaration.GetLocation(),
                                                   Resources.PublicApplyMethodsDescription);
                context.ReportDiagnostic(diagnostic);
                return;
            }
        }

        private void AnalyzeAggregateConstructors(SyntaxNodeAnalysisContext context)
        {
            var constructorDeclaration = (ConstructorDeclarationSyntax)context.Node;
            var constructorStringIdentifier = constructorDeclaration.Identifier.ToString().ToLower();

            // Do not impact non-aggregate classes and test classes.
            if (!constructorStringIdentifier.Contains("aggregate") || constructorStringIdentifier.Contains("test"))
            {
                return;
            }

            var parameterList = constructorDeclaration.ParameterList;
            bool constructorContainsPublicKeyword = constructorDeclaration.Modifiers.Any(x => x.IsKind(SyntaxKind.PublicKeyword));
            bool constructorContainsPrivateKeyword = constructorDeclaration.Modifiers.Any(x => x.IsKind(SyntaxKind.PrivateKeyword));
            bool constructorContainsParameters = parameterList.Parameters.Any();
            bool constructorBodyContainsStatements = constructorDeclaration.Body?.Statements.Any() ?? false;

            if (!constructorContainsParameters)
            {
                if (!constructorContainsPublicKeyword)
                {
                    var diagnostic = Diagnostic.Create(PublicConstructorRule,
                                                       constructorDeclaration.GetLocation(),
                                                       Resources.PublicConstructorDescription);
                    context.ReportDiagnostic(diagnostic);
                }

                if (constructorBodyContainsStatements)
                {
                    var diagnostic = Diagnostic.Create(EmptyConstructorRule,
                                                        constructorDeclaration.GetLocation(),
                                                        Resources.EmptyConstructorDescription);
                    context.ReportDiagnostic(diagnostic);
                }

                return;
            }

            // When the constructor has parameters and is not private. All constructors except default constructor should be private
            if (!constructorContainsPrivateKeyword)
            {
                var diagnostic = Diagnostic.Create(ConstructorsWithParametersShouldBePrivate,
                                                   constructorDeclaration.GetLocation(),
                                                   Resources.PrivateConstructorDescription);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
