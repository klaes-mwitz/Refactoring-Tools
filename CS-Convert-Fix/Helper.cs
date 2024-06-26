using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using System;
using System.Diagnostics;
using System.Linq;

namespace CS_Convert_Fix
{
    internal static class Helper
    {
        public static int FixedProblems;

        public static void PrintLine(string message, ConsoleColor color = ConsoleColor.White, bool debugOutput = false)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();

            if (debugOutput)
                Debug.WriteLine(message);
        }

        public static void RemoveNode(SyntaxNode node, DocumentEditor documentEditor, SyntaxRemoveOptions? syntaxRemoveOptions = null)
        {
            if (syntaxRemoveOptions != null)
                documentEditor.RemoveNode(node, (SyntaxRemoveOptions)syntaxRemoveOptions);
            else
                documentEditor.RemoveNode(node);
            PrintLine("\t\t\t\tRemoved Node: " + node, ConsoleColor.Cyan, true);
        }

        public static void ReplaceNode(SyntaxNode src, SyntaxNode dst, DocumentEditor documentEditor)
        {
            documentEditor.ReplaceNode(src, dst);
            PrintLine("\t\t\t\tReplaced: " + src + " with: " + dst, ConsoleColor.Green, true);
        }

        public static SyntaxNode GetNodeFromLocation(SyntaxNode node, ReferenceLocation location)
        {
            var lineSpan = location.Location.GetLineSpan();
            return node.DescendantNodes().FirstOrDefault(n => n.GetLocation().GetLineSpan().Equals(lineSpan));
        }
    }
}
