using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using System;
using System.Linq;

namespace CS_Convert_Fix
{
    internal static class TMPArgsFix
    {
        // Replaces:
        //   var tmp = variable
        //   int arg = tmp[1]
        //   Call Method(ref arg)
        //
        // With:
        //   Call Method(ref variable[1])
        public static void FixTmpArgs(Solution solution, MethodDeclarationSyntax method, SemanticModel model, SyntaxNode root, DocumentEditor documentEditor)
        {
            foreach (var localDeclaration in method.DescendantNodes().OfType<LocalDeclarationStatementSyntax>())
            {
                var variableDeclarator = localDeclaration.Declaration.ChildNodes().OfType<VariableDeclaratorSyntax>().FirstOrDefault();
                var variableType = localDeclaration.Declaration.ChildNodes()?.OfType<IdentifierNameSyntax>().FirstOrDefault();

                // Find tmp vars
                // var tmp = Publicg.gtSpezDat.iEckVbTyp;
                if (variableType?.Identifier.ValueText == "var" && variableDeclarator?.Identifier.ValueText.StartsWith("tmp") == true)
                {
                    AnalyzeTmpVariable(solution, localDeclaration, model, root, documentEditor);
                }
            }
        }

        private static void AnalyzeTmpVariable(Solution solution, LocalDeclarationStatementSyntax variable, SemanticModel model, SyntaxNode root, DocumentEditor documentEditor)
        {
            Helper.PrintLine("\t\tAnalyzing variable: " + variable, ConsoleColor.DarkGray);

            var variableDeclarator = variable.Declaration.ChildNodes().OfType<VariableDeclaratorSyntax>().FirstOrDefault();
            var variableDeclaratorSymbol = model.GetDeclaredSymbol(variableDeclarator);
            bool foundArgAssignment = false;
            foreach (var item in SymbolFinder.FindReferencesAsync(variableDeclaratorSymbol, solution).Result)
            {
                foreach (var location in item.Locations)
                {
                    SyntaxNode tmpVariableReference = Helper.GetNodeFromLocation(root, location);

                    // Find assignments to the argument variable
                    // short argfirst = tmp[1];
                    var argAssignment = tmpVariableReference.Ancestors().OfType<LocalDeclarationStatementSyntax>().FirstOrDefault();
                    if (argAssignment != null)
                    {
                        foundArgAssignment = true;
                        AnalyzeArgAssignment(solution, argAssignment, variableDeclarator.Initializer, model, root, documentEditor);

                        Helper.RemoveNode(variable, documentEditor);
                        Helper.RemoveNode(argAssignment, documentEditor);
                    }
                    else
                    {
                        var expression = tmpVariableReference.Ancestors().OfType<ExpressionStatementSyntax>().FirstOrDefault();
                        Helper.RemoveNode(expression, documentEditor);
                    }
                }
            }

            if (!foundArgAssignment)
                Helper.PrintLine("\t\t\tERROR: Found no arg assignment", ConsoleColor.Red);
        }

        private static void AnalyzeArgAssignment(Solution solution, LocalDeclarationStatementSyntax argumentVariable, EqualsValueClauseSyntax tmpEqualsClause, SemanticModel model, SyntaxNode root, DocumentEditor documentEditor)
        {
            Helper.PrintLine("\t\t\tFound arg assignment: " + argumentVariable.Declaration, ConsoleColor.DarkGray);

            var argumentDeclarator = argumentVariable.Declaration.ChildNodes().OfType<VariableDeclaratorSyntax>().FirstOrDefault();
            var argumentEqualsClause = argumentDeclarator.Initializer;

            var tmpAssignment = tmpEqualsClause.ChildNodes().FirstOrDefault(); // Right part of: var tmp = Publicg.gtSpezDat.iEckVbTyp;
            var argAssignment = argumentEqualsClause.ChildNodes().FirstOrDefault() as ElementAccessExpressionSyntax; // Right part of: short argfirst = tmp[1];

            // Combine the assignemnts Publicg.gtSpezDat.iEckVbTyp and tmp[1] to Publicg.gtSpezDat.iEckVbTyp[1]
            var combinedAccessText = tmpAssignment.ToString() + argAssignment.ArgumentList;
            var combinedAccessNode = SyntaxFactory.ParseExpression(combinedAccessText);
            var combinedArgumentNode = SyntaxFactory.Argument(null, SyntaxFactory.Token(SyntaxKind.RefKeyword), combinedAccessNode).NormalizeWhitespace();

            var argumentVariableSymbol = model.GetDeclaredSymbol(argumentDeclarator);

            bool replacedArgument = false;
            foreach (var item in SymbolFinder.FindReferencesAsync(argumentVariableSymbol, solution).Result)
            {
                foreach (var location in item.Locations)
                {
                    SyntaxNode argVariableReference = Helper.GetNodeFromLocation(root, location);

                    // Find ref argument in method call: erfz25.Swap(ref argfirst);
                    var methodArgument = argVariableReference.Ancestors().OfType<ArgumentSyntax>().FirstOrDefault();

                    if (methodArgument == null)
                        continue;

                    Helper.ReplaceNode(methodArgument, combinedArgumentNode, documentEditor);
                    replacedArgument = true;
                    Helper.FixedProblems++;
                }
            }

            if (!replacedArgument)
                Helper.PrintLine("\t\t\t\tERROR: Replaced no argument");
        }
    }
}
