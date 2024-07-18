using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSEnumConverter.Converter
{
    internal static class Helper
    {
        /// <summary>
        /// Gets the syntax node from the specified location in the syntax tree.
        /// </summary>
        /// <param name="tree">The syntax tree.</param>
        /// <param name="location">The reference location.</param>
        /// <returns>The syntax node at the specified location.</returns>
        public static SyntaxNode GetNodeFromLocation(SyntaxTree tree, ReferenceLocation location)
        {
            var lineSpan = location.Location.GetLineSpan();
            return tree.GetRoot().DescendantNodes().FirstOrDefault(n => n.GetLocation().GetLineSpan().Equals(lineSpan));
        }

        /// <summary>
        /// Gets the syntax node from the specified reference location.
        /// </summary>
        /// <param name="location">The reference location.</param>
        /// <returns>The syntax node at the specified location.</returns>
        public static SyntaxNode GetNodeFromLocation(ReferenceLocation location)
        {
            return GetNodeFromLocation(location.Location.SourceTree, location);
        }

        /// <summary>
        /// Gets the syntax node from the specified symbol.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>The syntax node for the specified symbol.</returns>
        public static SyntaxNode GetNodeFromSymbol(ISymbol symbol)
        {
            var tree = symbol.DeclaringSyntaxReferences.First().SyntaxTree;
            var span = symbol.DeclaringSyntaxReferences.First().Span;
            return tree.GetRoot().FindNode(span);
        }

        /// <summary>
        /// Gets the fully qualified name of the symbol like classname.name.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>The fully qualified name of the symbol.</returns>
        public static string GetFullyQualifiedName(ISymbol symbol)
        {
            if (symbol == null || IsRootNamespace(symbol) || symbol is IModuleSymbol)
            {
                return string.Empty;
            }

            var stringBuilder = new StringBuilder(symbol.MetadataName);
            symbol = symbol.ContainingSymbol;

            while ((symbol as INamedTypeSymbol)?.TypeKind == TypeKind.Class || (symbol as INamedTypeSymbol)?.TypeKind == TypeKind.Struct)
            {
                stringBuilder.Insert(0, '.');
                stringBuilder.Insert(0, symbol.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));

                symbol = symbol.ContainingSymbol;
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Gets the full metadata name of the symbol.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>The full metadata name of the symbol.</returns>
        public static string GetFullMetadataName(ISymbol symbol)
        {
            if (symbol == null || IsRootNamespace(symbol))
            {
                return string.Empty;
            }

            var stringBuilder = new StringBuilder(symbol.MetadataName);
            var last = symbol;

            symbol = symbol.ContainingSymbol;

            while (!IsRootNamespace(symbol))
            {
                if (symbol is ITypeSymbol && last is ITypeSymbol)
                {
                    stringBuilder.Insert(0, '+');
                }
                else
                {
                    stringBuilder.Insert(0, '.');
                }

                stringBuilder.Insert(0, symbol.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                symbol = symbol.ContainingSymbol;
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Filters the symbols using the original metadata name.
        /// </summary>
        /// <param name="originalMetadataName">The original metadata name.</param>
        /// <param name="symbolList">The list of symbols.</param>
        /// <returns>The symbol that matches the original metadata name.</returns>
        public static ISymbol FilterSymbolsUsingMetadataName(string originalMetadataName, IEnumerable<ISymbol> symbolList)
        {
            foreach (var symbol in symbolList)
            {
                string metadataName = GetFullMetadataName(symbol);
                if (metadataName == originalMetadataName)
                {
                    return symbol;
                }
            }

            return null;
        }

        /// <summary>
        /// Removes the cast expressions from the specified expression node.
        /// </summary>
        /// <param name="expressionNode">The expression node.</param>
        /// <returns>The expression node without cast expressions.</returns>
        public static ExpressionSyntax RemoveCastExpressions(ExpressionSyntax expressionNode)
        {
            while (expressionNode.IsKind(SyntaxKind.CastExpression))
            {
                expressionNode = (expressionNode as CastExpressionSyntax).Expression;
            }

            return expressionNode;
        }

        /// <summary>
        /// Removes the parenthesized expressions from the specified expression node.
        /// </summary>
        /// <param name="expressionNode">The expression node.</param>
        /// <returns>The expression node without parenthesized expressions.</returns>
        public static ExpressionSyntax RemoveParenthesizedExpressions(ExpressionSyntax expressionNode)
        {
            while (expressionNode.IsKind(SyntaxKind.ParenthesizedExpression))
            {
                expressionNode = (expressionNode as ParenthesizedExpressionSyntax).Expression;
            }

            return expressionNode;
        }

        /// <summary>
        /// Checks if the value is a number or a character.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>True if the value is a number or a character, otherwise false.</returns>
        public static bool IsNumberOrChar(object value) => value is sbyte
                                                        || value is byte
                                                        || value is short
                                                        || value is ushort
                                                        || value is int
                                                        || value is uint
                                                        || value is long
                                                        || value is ulong
                                                        || value is char;

        /// <summary>
        /// Gets the constant number from the specified expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="model">The semantic model.</param>
        /// <returns>The constant number from the expression, or null if not a constant number.</returns>
        public static int? GetConstantNumberFromExpression(ExpressionSyntax expression, SemanticModel model)
        {
            var value = model.GetConstantValue(expression);
            if (value.HasValue)
            {
                if (IsNumberOrChar(value.Value))
                {
                    return Convert.ToInt32(value.Value);
                }
                else if (expression.IsKind(SyntaxKind.StringLiteralExpression)) // Convert a string with the length of 1 to a char
                {
                    return Convert.ToInt32(value.Value.ToString()[0]);
                }
            }

            return null;
        }

        /// <summary>
        /// Filters the numerical literal expressions from the specified collection.
        /// </summary>
        /// <param name="literalExpressions">The collection of literal expressions.</param>
        /// <returns>The filtered numerical literal expressions.</returns>
        public static IEnumerable<LiteralExpressionSyntax> FilterNumericalLiteralExpressions(IEnumerable<LiteralExpressionSyntax> literalExpressions)
        {
            return literalExpressions.Where(node =>
                node.IsKind(SyntaxKind.NumericLiteralExpression) ||
                node.IsKind(SyntaxKind.CharacterLiteralExpression) ||
                (node.IsKind(SyntaxKind.StringLiteralExpression) && node.Token.Value.ToString().Length == 1));
        }

        /// <summary>
        /// Gets the literal expression children of the specified node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>The literal expression children of the node.</returns>
        public static IEnumerable<LiteralExpressionSyntax> GetLiteralExpressionChildren(SyntaxNode node)
        {
            var children = node.ChildNodes().OfType<LiteralExpressionSyntax>();
            if (children.Any())
            {
                return children;
            }

            var invocations = node.ChildNodes().OfType<InvocationExpressionSyntax>();
            foreach (var invocation in invocations)
            {
                var identifier = invocation.Expression.ChildNodes().OfType<IdentifierNameSyntax>().FirstOrDefault();
                if (IsConversionMethodAllowedInExpression(identifier))
                {
                    return invocation.ArgumentList.Arguments.Select(arg => arg.Expression as LiteralExpressionSyntax);
                }
            }

            return new List<LiteralExpressionSyntax>();
        }

        /// <summary>
        /// Checks if the conversion method is allowed in the expression.
        /// </summary>
        /// <param name="identifier">The identifier name syntax.</param>
        /// <returns>True if the conversion method is allowed in the expression, otherwise false.</returns>
        public static bool IsConversionMethodAllowedInExpression(IdentifierNameSyntax identifier)
        {
            string[] allowedMethods = { "Asc_C", "erfz25", "erfz25.Asc_C", "Conversions", "ToInteger", "ToShort", "ToChar", "ToString" };
            string methodName = identifier.Identifier.ToString();

            return allowedMethods.Any(name => name.Equals(methodName));
        }

        /// <summary>
        /// Checks if the node is inside a conversion method invocation and returns it if possible.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>The conversion method invocation, or null if not found.</returns>
        public static InvocationExpressionSyntax GetConversionMethodInvocation(SyntaxNode node)
        {
            // Check if the node is directly inside the conversion method and returns it if possible
            if (!(node?.Parent is ArgumentSyntax))
                return null;

            var invocationExpression = node.Ancestors().OfType<InvocationExpressionSyntax>().FirstOrDefault();
            if (invocationExpression?.ArgumentList.Arguments.Count == 1)
            {
                var invocationIdentifier = invocationExpression.Expression.ChildNodes().OfType<IdentifierNameSyntax>().FirstOrDefault();
                if (invocationIdentifier != null && IsConversionMethodAllowedInExpression(invocationIdentifier))
                {
                    return invocationExpression;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if the node is directly inside a cast expression and returns it if possible.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>The cast expression, or null if not found.</returns>
        public static CastExpressionSyntax GetCastExpression(SyntaxNode node)
        {
            if (node is CastExpressionSyntax castExpression)
                return castExpression;

            if (node.Parent is ParenthesizedExpressionSyntax || node.Parent is CastExpressionSyntax)
                return GetCastExpression(node.Parent);

            return null;
        }

        /// <summary>
        /// Checks if the expression is a bitwise expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>True if the expression is a bitwise expression, otherwise false.</returns>
        public static bool IsBitwiseExpression(ExpressionSyntax expression)
        {
            return expression.IsKind(SyntaxKind.BitwiseAndExpression)
                || expression.IsKind(SyntaxKind.BitwiseOrExpression)
                || expression.IsKind(SyntaxKind.BitwiseNotExpression);
        }

        /// <summary>
        /// Gets the highest acnestor binary expression from the specified node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>The highest binary expression, or null if not found.</returns>
        public static BinaryExpressionSyntax GetHighestBinaryExpression(SyntaxNode node)
        {
            return node.Ancestors().OfType<BinaryExpressionSyntax>().LastOrDefault(n => !n.IsKind(SyntaxKind.LogicalAndExpression) && !n.IsKind(SyntaxKind.LogicalOrExpression));
        }

        /// <summary>
        /// Gets all the identifiers with the specified name from the specified node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="name">The name of the identifiers.</param>
        /// <returns>A list of identifiers with the specified name.</returns>
        public static List<IdentifierNameSyntax> GetAllIdentifiersByName(SyntaxNode node, string name)
        {
            return node.DescendantNodes().OfType<IdentifierNameSyntax>().Where(identifier => identifier.Identifier.Text == name).ToList();
        }

        /// <summary>
        /// Checks if the symbol is a global root namespace.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>True if the symbol is a root namespace, otherwise false.</returns>
        private static bool IsRootNamespace(ISymbol symbol)
        {
            INamespaceSymbol s;
            return ((s = symbol as INamespaceSymbol) != null) && s.IsGlobalNamespace;
        }
    }
}
