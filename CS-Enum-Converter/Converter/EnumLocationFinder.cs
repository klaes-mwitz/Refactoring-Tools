using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace CSEnumConverter.Converter
{
    internal class EnumLocationFinder
    {
        private readonly Enum _enum;
        private readonly CSharpSolution _solution;

        public EnumLocationFinder(Enum enumToFind, CSharpSolution solution)
        {
            _enum = enumToFind;
            _solution = solution;
        }

        /// <summary>
        /// Finds the specified enum in the solution.
        /// </summary>
        /// <returns>The named type symbol representing the enum if found; otherwise, null.</returns>
        public INamedTypeSymbol FindEnum()
        {
            Logger.WriteLine("Searching for the enum in the solution.");

            var namedSymbols = SymbolFinder.FindSourceDeclarationsAsync(_solution.CSSolution, x => x.Equals(_enum.Name)).Result
                .Where(x => x.Kind == SymbolKind.NamedType).Cast<INamedTypeSymbol>().ToList();

            var enumSymbol = Helper.FilterSymbolsUsingMetadataName(_enum.MetadataName, namedSymbols);

            if (enumSymbol == null && namedSymbols.Count == 1)
            {
                enumSymbol = namedSymbols.First();
                Logger.WriteWarning("Could not resolve the exact enum position. Maybe an enclosing class or namespace is missing. Using the first matching enum.");
            }

            if (enumSymbol != null)
            {
                SyntaxNode enumNode = Helper.GetNodeFromSymbol(enumSymbol);
                var document = _solution.CSSolution.GetDocument(enumNode.SyntaxTree);
                Logger.WriteLine(string.Format("Found Enum: {0} in file: {1}\n{2}", _enum.ToString(), document.Name, enumNode.ToFullString()), 0, Color.Aquamarine, enumNode);

                // Check whether the enum in the source matches the provided enum 
                var compareEnum = new Enum();
                compareEnum.ParseCode(enumNode.ToFullString());

                if (!_enum.CompareEntries(compareEnum))
                {
                    Logger.WriteError("The provided enum and the enum in the source do not match.", node: enumNode);
                    return null;
                }

                // Check if the enum has a [Flags] attribute
                if (!enumNode.DescendantNodes().OfType<AttributeSyntax>().Any(attribute => attribute.ToString() == "Flags"))
                    Logger.WriteWarning("The enum does not have a <Flags> attribute. It is highly recommended to add the attribute.", node: enumNode);

                // Check if the enum has a 0 Flag
                if (_enum.GetZeroEntry() == null)
                    Logger.WriteWarning("The enum does not have an entry with the value 0. It is recommended to add one with a name like \"NONE\".", node: enumNode);

                _enum.SetSymbol(enumSymbol);

                return enumSymbol as INamedTypeSymbol;
            }

            Logger.WriteError("No Matching enum found in the solution.");

            return null;
        }

        /// <summary>
        /// Gets the references to the specified enum in the solution.
        /// </summary>
        /// <param name="enumSymbol">The named type symbol representing the enum.</param>
        /// <returns>The collection of reference locations.</returns>
        public IEnumerable<ReferenceLocation> GetEnumReferences(INamedTypeSymbol enumSymbol)
        {
            var enumRefLocations = new List<ReferenceLocation>();
            foreach (var item in SymbolFinder.FindReferencesAsync(enumSymbol, _solution.CSSolution).Result)
            {
                enumRefLocations.AddRange(item.Locations);
            }

            // Filter all references that are not declarations
            var locations = new List<ReferenceLocation>();
            foreach (var location in enumRefLocations)
            {
                var enumNode = (IdentifierNameSyntax)Helper.GetNodeFromLocation(location);
                if (enumNode.Ancestors().OfType<CastExpressionSyntax>().Any())
                    continue;

                if (enumNode.Ancestors().Any(n => n.IsKind(SyntaxKind.LocalDeclarationStatement)
                                               || n.IsKind(SyntaxKind.DeclarationExpression)
                                               || n.IsKind(SyntaxKind.FieldDeclaration)
                                               || n.IsKind(SyntaxKind.Parameter)
                                               || n.IsKind(SyntaxKind.MethodDeclaration)
                                               || n.IsKind(SyntaxKind.PropertyDeclaration)
                                               || n.IsKind(SyntaxKind.VariableDeclaration)))
                {
                    locations.Add(location);
                }
            }

            // Search for all global variables and function calls with the enum type and add the references of the variables to the list
            foreach (var location in locations.ToList())
            {
                var enumNode = (IdentifierNameSyntax)Helper.GetNodeFromLocation(location);
                var methodDeclaration = enumNode.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                var parameterNode = enumNode.Ancestors().OfType<ParameterSyntax>().FirstOrDefault();
                var fieldDeclaration = enumNode.Ancestors().OfType<FieldDeclarationSyntax>().FirstOrDefault();
                var propertyNode = enumNode.Ancestors().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
                var variableDeclaration = enumNode.Ancestors().OfType<VariableDeclarationSyntax>().FirstOrDefault();

                if (methodDeclaration == null && fieldDeclaration?.ChildTokens().Any(t => t.IsKind(SyntaxKind.StaticKeyword)) == true) // Find all global variable reference locations
                {
                    var variableDeclarator = fieldDeclaration.DescendantNodes().OfType<VariableDeclaratorSyntax>().FirstOrDefault();
                    locations.AddRange(FindNodeReferences(variableDeclarator));
                }

                if (propertyNode != null) // Property references
                {
                    locations.AddRange(FindNodeReferences(propertyNode));
                }

                if (methodDeclaration?.ReturnType.DescendantNodes().Contains(enumNode) == true) // Return type references
                {
                    locations.AddRange(FindNodeReferences(methodDeclaration));
                }

                if (variableDeclaration != null) // Variable declarations
                {
                    var variableDeclarator = variableDeclaration.DescendantNodes().OfType<VariableDeclaratorSyntax>().FirstOrDefault();
                    locations.AddRange(FindNodeReferences(variableDeclarator));
                }

                if (parameterNode != null)
                {
                    locations.AddRange(FindNodeReferences(parameterNode)); // Parameter references
                    if (methodDeclaration != null) //Method references
                    {
                        locations.AddRange(FindNodeReferences(methodDeclaration));
                    }
                }
            }

            FilterDuplicateLocations(locations);

            return locations;
        }

        /// <summary>
        /// Finds the references to the specified syntax node in the solution.
        /// </summary>
        /// <param name="node">The syntax node to find references for.</param>
        /// <returns>The list of reference locations.</returns>
        private List<ReferenceLocation> FindNodeReferences(SyntaxNode node)
        {
            var variableSymbol = _solution.GetDeclaredSymbol(node);

            List<ReferenceLocation> locations = new List<ReferenceLocation>();
            foreach (var item in SymbolFinder.FindReferencesAsync(variableSymbol, _solution.CSSolution).Result)
            {
                locations.AddRange(item.Locations);
            }

            return locations;
        }

        /// <summary>
        /// Filters duplicate reference locations in if statements and assignments.
        /// </summary>
        /// <param name="locations">The list of reference locations.</param>
        private void FilterDuplicateLocations(List<ReferenceLocation> locations)
        {
            var nodes = new List<SyntaxNode>();
            foreach (var location in locations.ToList())
            {
                var enumNode = Helper.GetNodeFromLocation(location);

                if (!(enumNode is IdentifierNameSyntax))
                {
                    locations.Remove(location);
                    continue;
                }

                SyntaxNode node = enumNode.Ancestors().OfType<IfStatementSyntax>().FirstOrDefault();
                node = node ?? enumNode.Ancestors().OfType<AssignmentExpressionSyntax>().FirstOrDefault();
                if (node != null)
                {
                    if (nodes.Contains(node))
                    {
                        locations.Remove(location);
                    }
                    else
                    {
                        nodes.Add(node);
                    }
                }
            }
        }
    }
}
