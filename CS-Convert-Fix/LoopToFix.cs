using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System.Linq;

namespace CS_Convert_Fix
{
    internal static class LoopToFix
    {
        // Function that finds all local variables in a method whose name contain loopTo and replaces them with the correct value
        internal static void FixLoopTo(Solution solution, MethodDeclarationSyntax method, SemanticModel model, SyntaxNode root, DocumentEditor documentEditor)
        {
            // Get all local variable declarations in the method
            var loopToDeclarations = method.DescendantNodes().OfType<LocalDeclarationStatementSyntax>()
                .Where(n => n.Declaration.Variables.Any(d => d.Identifier.Text.Contains("loopTo")));

            foreach (var loopToDeclaration in loopToDeclarations)
            {
                AnalyzeLoopToDeclaration(loopToDeclaration, model, documentEditor);
            }
        }

        private static void AnalyzeLoopToDeclaration(LocalDeclarationStatementSyntax loopToDeclaration, SemanticModel model, DocumentEditor documentEditor)
        {
            // Get the right side of the assignment declaration: var loopTo = value
            var loopToValue = loopToDeclaration.Declaration.Variables.First().Initializer.Value;

            // Get the identifier of the right side of the assignment declaration: value
            var loopToValueIdentifier = loopToValue.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
                                                   .LastOrDefault(n => !n.Parent.IsKind(SyntaxKind.Argument)
                                                       && n.Identifier.Text != "GetLength"
                                                       && n.Identifier.Text != "Length");

            // Get the name of the loopTo Variable: loopTo
            var loopToName = loopToDeclaration.Declaration.Variables.First().Identifier.Text;

            // Find the for loop that uses the loopTo variable
            var forLoopStatement = loopToDeclaration.Parent.ChildNodes().OfType<ForStatementSyntax>()
                .Where(n => n.DescendantNodes().OfType<IdentifierNameSyntax>().Any(ident => ident.Identifier.Text == loopToName)).FirstOrDefault();

            // Find all assignments to the right side of the loopTo variable
            var assignment = forLoopStatement.DescendantNodes().OfType<AssignmentExpressionSyntax>()
                .SelectMany(n => n.Left.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
                .LastOrDefault(ident => !ident.Ancestors().OfType<ArgumentSyntax>().Any()
                    && ident.Identifier.Text == loopToValueIdentifier.Identifier.Text);

            if (assignment != null)
                return;

            // Get the identifier of the loopTo variable in the for loop
            var forLoopToIdentifiers = forLoopStatement.Condition.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Where(ident => ident.Identifier.Text == loopToName);
            foreach (var vorloopToIdentifier in forLoopToIdentifiers)
                Helper.ReplaceNode(vorloopToIdentifier, loopToValue, documentEditor);

            Helper.RemoveNode(loopToDeclaration, documentEditor, SyntaxRemoveOptions.KeepLeadingTrivia);
            Helper.FixedProblems++;
        }
    }
}
