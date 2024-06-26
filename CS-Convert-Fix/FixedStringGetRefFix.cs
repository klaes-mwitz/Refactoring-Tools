using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System.Linq;

namespace CS_Convert_Fix
{
    internal static class FixedStringGetRefFix
    {
        // Replaces the FixedStringCollection and FixedString2D getter with a GetRef() callif used as ref parameter
        public static void FixFixedStringGetRef(Solution solution, MethodDeclarationSyntax method, SemanticModel model, SyntaxNode root, DocumentEditor documentEditor)
        {
            var invocations = method.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (var invocation in invocations)
            {
                AnalyzeFixedStringGetRefInvocation(solution, invocation, model, root, documentEditor);
            }
        }

        private static void AnalyzeFixedStringGetRefInvocation(Solution solution, InvocationExpressionSyntax invocation, SemanticModel model, SyntaxNode root, DocumentEditor documentEditor)
        {
            foreach (var argument in invocation.ArgumentList.Arguments)
            {
                AnalyzeFixedStringGetRefArgument(solution, argument, model, root, documentEditor);
            }
        }

        private static void AnalyzeFixedStringGetRefArgument(Solution solution, ArgumentSyntax argument, SemanticModel model, SyntaxNode root, DocumentEditor documentEditor)
        {
            if (!argument.RefKindKeyword.IsKind(SyntaxKind.RefKeyword))
                return;

            var elementAccess = argument.DescendantNodesAndSelf().OfType<ElementAccessExpressionSyntax>().FirstOrDefault();
            if (elementAccess == null)
                return;

            IdentifierNameSyntax accessIdentifier = null;
            var memberAccess = elementAccess.Expression as MemberAccessExpressionSyntax;
            if (memberAccess != null)
                accessIdentifier = memberAccess.ChildNodes().OfType<IdentifierNameSyntax>().Where(n => n.Identifier.Text == memberAccess.Name.ToString()).FirstOrDefault();
            else
                accessIdentifier = elementAccess.Expression as IdentifierNameSyntax;

            var accessType = model.GetTypeInfo(accessIdentifier).Type;

            if (accessType.Kind == SymbolKind.ErrorType)
                return;

            if (accessType.Name != "FixedStringCollection" && accessType?.Name != "FixedStringArray2D")
                return;

            var argumentString = $"{elementAccess.Expression}.GetRef({elementAccess.ArgumentList.Arguments})";
            var argumentNode = SyntaxFactory.Argument(null, SyntaxFactory.Token(SyntaxKind.RefKeyword), SyntaxFactory.ParseExpression(argumentString)).NormalizeWhitespace();

            Helper.ReplaceNode(argument, argumentNode, documentEditor);
            Helper.FixedProblems++;
        }
    }
}
