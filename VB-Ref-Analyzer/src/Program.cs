using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace src
{
    internal static class Program
    {
        private static int removedRefs = 0;

        static int Main(string[] args)
        {
            // Attempt to set the version of MSBuild.
            var visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            var instance = visualStudioInstances.Length == 1
                ? visualStudioInstances[0] // If there is only one instance of MSBuild on this machine, set that as the one to use.
                : SelectVisualStudioInstance(visualStudioInstances); // Handle selecting the version of MSBuild you want to use.

            Console.WriteLine($"Using MSBuild at '{instance.MSBuildPath}' to load projects.");

            MSBuildLocator.RegisterInstance(instance);
            var workspace = MSBuildWorkspace.Create(); // On Exception -> Rebuild Project

            var solutionPath = Path.Combine(Environment.CurrentDirectory, args[0]);
            PrintLine($"Loading solution '{solutionPath}'", ConsoleColor.Green);
            var solution = workspace.OpenSolutionAsync(solutionPath, new ConsoleProgressReporter()).Result;

            AnalyzeProjects(ref solution);

            if (workspace.TryApplyChanges(solution))
                PrintLine("========== Successfully removed " + removedRefs + " refs ==========", ConsoleColor.Green);
            else
            {
                PrintLine("========== Error: Cannot apply changes ==========", ConsoleColor.Red);
                return 0;
            }

            return removedRefs;
        }

        private static void AnalyzeProjects(ref Solution solution)
        {
            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId);
                if (project.Language != "Visual Basic")
                    continue;

                PrintLine("Analyzing project: " + project.Name, ConsoleColor.Green);

                var compilation = project.GetCompilationAsync().Result;
                AnalyzeDocuments(solution, ref project, compilation);
                solution = project.Solution;
            }
        }
        private static void AnalyzeDocuments(Solution solution, ref Project project, Compilation compilation)
        {
            int documentIdx = 1;
            int documentCount = project.Documents.Count();

            foreach (var documentId in project.DocumentIds)
            {
                var document = project.GetDocument(documentId);
                var tree = document.GetSyntaxTreeAsync().Result;
                var model = compilation.GetSemanticModel(tree);
                var root = document.GetSyntaxRootAsync().Result;
                var documentEditor = DocumentEditor.CreateAsync(document).Result;

                PrintLine("[" + documentIdx++ + "|" + documentCount + "] Analyzing file: " + document.Name, ConsoleColor.White);
                foreach (var method in root.DescendantNodes().OfType<MethodStatementSyntax>())
                {
                    PrintLine("\tAnalyzing method: " + method, ConsoleColor.Gray);

                    var redims = method.Parent.DescendantNodes().OfType<ReDimStatementSyntax>();
                    var assignments = method.Parent.DescendantNodes().OfType<AssignmentStatementSyntax>();

                    AnalyzeParameter(solution, method, model, root, documentEditor, redims, assignments);
                }

                document = documentEditor.GetChangedDocument();
                project = document.Project;
            }
        }

        private static void AnalyzeParameter(Solution solution,
                                             MethodStatementSyntax method,
                                             SemanticModel model,
                                             SyntaxNode root,
                                             DocumentEditor documentEditor,
                                             IEnumerable<ReDimStatementSyntax> redims,
                                             IEnumerable<AssignmentStatementSyntax> assignments)
        {
            if (method.ParameterList == null)
                return;

            foreach (var parameter in method.ParameterList.Parameters)
            {
                var parameterSymbol = model.GetDeclaredSymbol(parameter);
                if (parameterSymbol.RefKind != RefKind.Ref)
                    continue;

                PrintLine("\t\tAnalyzing parameter: " + parameter, ConsoleColor.DarkGray);

                var canDeleteRef = true;
                foreach (var item in SymbolFinder.FindReferencesAsync(parameterSymbol, solution).Result)
                {
                    foreach (var location in item.Locations)
                    {
                        SyntaxNode parameterReference = GetNodeFromLocation(root, location);
                        if (parameterReference == null) continue;

                        // Check for value assignments
                        if (FindWriteAssignments(assignments, parameterReference, model))
                        {
                            canDeleteRef = false;
                            goto DELETE_REF;
                        }

                        // Check for redims
                        if (FindReDims(redims, parameterReference))
                        {
                            canDeleteRef = false;
                            goto DELETE_REF;
                        }

                        // Check for method calls with ref
                        if (FindRefInvocations(model, parameterReference))
                        {
                            canDeleteRef = false;
                            goto DELETE_REF;
                        }
                    }
                }

                DELETE_REF:
                if (canDeleteRef)
                {
                    RemoveRef(documentEditor, parameter);
                }
            }
        }

        private static bool FindReDims(IEnumerable<ReDimStatementSyntax> redims, SyntaxNode parameterReference)
        {
            foreach (var redim in redims)
            {
                foreach (var clause in redim.Clauses)
                {
                    var Identifier = clause.DescendantNodes().OfType<IdentifierNameSyntax>().FirstOrDefault();

                    if (Identifier.Identifier.ToString().Equals(parameterReference.ToString()))
                    {
                        PrintLine("\t\t\tFound ReDim: " + redim.ToString(), ConsoleColor.Yellow);

                        return true;
                    }
                }
            }

            return false;
        }

        private static bool FindRefInvocations(SemanticModel model, SyntaxNode parameterReference)
        {
            if (!(parameterReference is SimpleArgumentSyntax))
                return false;

            foreach (var invocation in parameterReference.Ancestors().OfType<InvocationExpressionSyntax>())
            {
                if (invocation.ArgumentList == null)
                    continue;

                // Filter array access
                var invocationIdentifier = invocation.Expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().LastOrDefault();
                var identifierSymbolInfo = model.GetSymbolInfo(invocationIdentifier);
                var identifierSymbol = identifierSymbolInfo.Symbol ?? identifierSymbolInfo.CandidateSymbols.FirstOrDefault();
                if (!(identifierSymbol is IMethodSymbol))
                    continue;

                var argument = invocation.ArgumentList.Arguments.Where(arg => arg.Equals(parameterReference)).FirstOrDefault();
                if (argument != null)
                {
                    var argumentParameter = ((IArgumentOperation)model.GetOperation(argument))?.Parameter;

                    if (argumentParameter == null)
                        argumentParameter = ((IParameterReferenceOperation)model.GetOperation(argument.ChildNodes().FirstOrDefault()))?.Parameter;

                    if (argumentParameter != null && argumentParameter.RefKind == RefKind.Ref)
                    {
                        Print("\t\t\tFound Ref Parameter: " + argumentParameter, ConsoleColor.Cyan);
                        PrintLine(" in: " + invocation, ConsoleColor.DarkCyan);

                        return true;
                    }
                }
            }

            return false;
        }

        private static bool FindWriteAssignments(IEnumerable<AssignmentStatementSyntax> assignments, SyntaxNode parameterReference, SemanticModel model)
        {
            var parameterIdentifier = parameterReference.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().FirstOrDefault();
            var parameterTypeInfo = model.GetTypeInfo(parameterIdentifier);

            foreach (var assignment in assignments)
            {
                bool isMidAssignment = assignment.IsKind(SyntaxKind.MidAssignmentStatement) && parameterTypeInfo.Type.SpecialType == SpecialType.System_String;

                if (!isMidAssignment)
                {
                    if (parameterReference is SimpleArgumentSyntax)
                        continue;

                    if (assignment.Left.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any())
                        continue;

                    if (assignment.Left.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>().Any())
                        continue;
                }

                var assignmentNodes = assignment.Left.DescendantNodesAndSelf().Where(n => n.Equals(parameterReference));
                if (assignmentNodes.Any())
                {
                    PrintLine("\t\t\tFound Write: " + assignment, ConsoleColor.Magenta);

                    return true;
                }
            }

            return false;
        }

        private static void RemoveRef(DocumentEditor documentEditor, ParameterSyntax parameter)
        {
            var newModifiers = parameter.Modifiers.Where(modifier => !modifier.IsKind(SyntaxKind.ByRefKeyword));
            var syntaxModifiers = SyntaxTokenList.Create(new SyntaxToken());
            syntaxModifiers = syntaxModifiers.AddRange(newModifiers.ToArray());

            var updatedParameterNode = parameter.WithModifiers(syntaxModifiers);
            documentEditor.ReplaceNode(parameter, updatedParameterNode);

            removedRefs++;
            PrintLine("\t\t\tRemoved Ref: " + parameter, ConsoleColor.Green);
        }

        public static SyntaxNode GetNodeFromLocation(SyntaxNode node, ReferenceLocation location)
        {
            var lineSpan = location.Location.GetLineSpan();
            return node.DescendantNodes().FirstOrDefault(n => n.GetLocation().GetLineSpan().Equals(lineSpan));
        }

        private static void PrintLine(string message, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static void Print(string message, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.Write(message);
            Console.ResetColor();
        }

        private static VisualStudioInstance SelectVisualStudioInstance(VisualStudioInstance[] visualStudioInstances)
        {
            Console.WriteLine("Multiple installs of MSBuild detected please select one:");
            for (int i = 0; i < visualStudioInstances.Length; i++)
            {
                Console.WriteLine($"Instance {i + 1}");
                Console.WriteLine($"    Name: {visualStudioInstances[i].Name}");
                Console.WriteLine($"    Version: {visualStudioInstances[i].Version}");
                Console.WriteLine($"    MSBuild Path: {visualStudioInstances[i].MSBuildPath}");
            }

            while (true)
            {
                var userResponse = Console.ReadLine();
                if (int.TryParse(userResponse, out int instanceNumber) &&
                    instanceNumber > 0 &&
                    instanceNumber <= visualStudioInstances.Length)
                {
                    return visualStudioInstances[instanceNumber - 1];
                }

                Console.WriteLine("Input not accepted, try again.");
            }
        }
    }
    internal class ConsoleProgressReporter : IProgress<ProjectLoadProgress>
    {
        public void Report(ProjectLoadProgress loadProgress)
        {
            var projectDisplay = Path.GetFileName(loadProgress.FilePath);
            if (loadProgress.TargetFramework != null)
            {
                projectDisplay += $" ({loadProgress.TargetFramework})";
            }

            Console.WriteLine($"{loadProgress.Operation,-15} {loadProgress.ElapsedTime,-15:m\\:ss\\.fffffff} {projectDisplay}");
        }
    }
}
