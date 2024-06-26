using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using System.Linq;

namespace CS_Convert_Fix
{
    internal static class SwapMethodFix
    {
        // Replaces:
        //   int argfirst = variable1
        //   int argsecond = variable2
        //   Call Swap(ref argfirst, argsecond)
        //   variable1 = argfirst
        //   variable2 = argsecond
        //
        // With:
        //   (variable1, variable2) = (variable2, variable1)
        public static void FixSwapMethods(Solution solution, MethodDeclarationSyntax method, SemanticModel model, SyntaxNode root, DocumentEditor documentEditor)
        {
            var invocations = method.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (var swapInvocation in invocations.Where(n => n.Expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().LastOrDefault()?.Identifier.Text == "Swap"))
            {
                AnalyzeSwapInvocation(solution, swapInvocation, model, root, documentEditor);
            }
        }

        private static void AnalyzeSwapInvocation(Solution solution, InvocationExpressionSyntax swapInvocation, SemanticModel model, SyntaxNode root, DocumentEditor documentEditor)
        {
            var firstArg = AnalyzeSwapArgument(solution, swapInvocation.ArgumentList.Arguments[0], model, root, documentEditor);
            var secondArg = AnalyzeSwapArgument(solution, swapInvocation.ArgumentList.Arguments[1], model, root, documentEditor);

            if (firstArg == null || secondArg == null)
                return;

            var swapText = $"({firstArg}, {secondArg}) = ({secondArg}, {firstArg})";
            var swapNode = SyntaxFactory.ParseExpression(swapText);
            var swapExpression = SyntaxFactory.ExpressionStatement(swapNode, SyntaxFactory.Token(SyntaxFactory.TriviaList(), SyntaxKind.SemicolonToken, SyntaxFactory.TriviaList(SyntaxFactory.Comment(" // Swap"), SyntaxFactory.Whitespace("\n"))));
            swapExpression = swapExpression.WithLeadingTrivia(swapInvocation.Parent.GetLeadingTrivia());

            Helper.ReplaceNode(swapInvocation.Parent, swapExpression, documentEditor);
            Helper.FixedProblems++;
        }

        // Analyzes an argument of the swap method, retrieves the initializer and removes the uneccessary assignments and declarations
        // Returns the original initializer of the argument variable
        private static SyntaxNode AnalyzeSwapArgument(Solution solution, ArgumentSyntax argument, SemanticModel model, SyntaxNode root, DocumentEditor documentEditor)
        {
            var argumentIdentifier = argument.Expression as IdentifierNameSyntax;
            if (!(argumentIdentifier?.Identifier.Text.StartsWith("arg") == true))
                return null;

            var argumentSymbol = model.GetSymbolInfo(argumentIdentifier).Symbol;

            // Find argument assignment and remove it
            foreach (var item in SymbolFinder.FindReferencesAsync(argumentSymbol, solution).Result)
            {
                foreach (var location in item.Locations)
                {
                    var argReference = Helper.GetNodeFromLocation(root, location);
                    var assignment = argReference.Ancestors().OfType<AssignmentExpressionSyntax>().FirstOrDefault();
                    if (assignment != null)
                        Helper.RemoveNode(assignment.Parent, documentEditor);
                }
            }

            // Find argument declaration and remove it
            var argDeclaration = argumentSymbol.DeclaringSyntaxReferences.FirstOrDefault().GetSyntax().AncestorsAndSelf().OfType<LocalDeclarationStatementSyntax>().FirstOrDefault();
            var argDeclarator = argDeclaration.Declaration.ChildNodes().OfType<VariableDeclaratorSyntax>().FirstOrDefault();
            var argInitializer = argDeclarator.Initializer.ChildNodes().FirstOrDefault();

            Helper.RemoveNode(argDeclaration, documentEditor);

            return argInitializer;
        }
    }
}
