using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System;
using System.Linq;

namespace CS_Convert_Fix
{
    internal static class SwitchFixedStringFix
    {
        // Adds a .ToString() to all switch statements using a FixedString
        public static void FixSwitchFixedStrings(MethodDeclarationSyntax method, SemanticModel model, DocumentEditor documentEditor)
        {
            foreach (var switchStatement in method.DescendantNodes().OfType<SwitchStatementSyntax>())
            {
                AnalyzeSwitchStatement(switchStatement, model, documentEditor);
            }
        }

        private static void AnalyzeSwitchStatement(SwitchStatementSyntax switchStatement, SemanticModel model, DocumentEditor documentEditor)
        {
            var variableIdentifier = switchStatement.Expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().LastOrDefault();
            var variableType = model.GetTypeInfo(variableIdentifier).Type;
            if (variableType?.Name != "FixedString" && variableType?.Name != "FixedStringCollection" && variableType?.Name != "FixedStringArray2D")
                return;

            Helper.PrintLine("\t\tAnalyzing switch: " + switchStatement, ConsoleColor.DarkGray);

            var swapString = switchStatement.Expression + ".ToString()";
            var expressionNode = SyntaxFactory.ParseExpression(swapString);

            Helper.ReplaceNode(switchStatement.Expression, expressionNode, documentEditor);
            Helper.FixedProblems++;
        }
    }
}
