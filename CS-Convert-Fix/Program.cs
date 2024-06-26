using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CS_Convert_Fix
{
    internal static class Program
    {
        static readonly string[] projectList = { "Klaes.Construction", "Klaes.Construction.Analysis", "Klaes.Construction.Design", "Klaes.Construction.Design.Shared" };
        enum FixType
        {
            TMPArgs,
            SwapMethod,
            SwitchFixedString,
            FixedStringGetRef,
            LoopTo
        }

        static async Task Main(string[] args)
        {
            // Attempt to set the version of MSBuild.
            var visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            var instance = visualStudioInstances.Length == 1
                // If there is only one instance of MSBuild on this machine, set that as the one to use.
                ? visualStudioInstances[0]
                // Handle selecting the version of MSBuild you want to use.
                : SelectVisualStudioInstance(visualStudioInstances);

            Console.WriteLine($"Using MSBuild at '{instance.MSBuildPath}' to load projects.");

            // NOTE: Be sure to register an instance with the MSBuildLocator 
            //       before calling MSBuildWorkspace.Create()
            //       otherwise, MSBuildWorkspace won't MEF compose.
            MSBuildLocator.RegisterInstance(instance);

            await FixProblem(args, FixType.TMPArgs);
            await FixProblem(args, FixType.SwapMethod);
            await FixProblem(args, FixType.SwitchFixedString);
            await FixProblem(args, FixType.FixedStringGetRef);
            await FixProblem(args, FixType.LoopTo);

            Helper.PrintLine("========== Successfully fixed: " + Helper.FixedProblems + " locations ==========", ConsoleColor.Green);
        }

        private static async Task FixProblem(string[] args, FixType fixType)
        {
            using (var workspace = MSBuildWorkspace.Create())
            {
                // Print message for WorkspaceFailed event to help diagnosing project load failures.
                workspace.WorkspaceFailed += (o, e) => Console.WriteLine(e.Diagnostic.Message);

                var solutionPath = args[0];
                Console.WriteLine($"Loading solution '{solutionPath}'");

                // Attach progress reporter so we print projects as they are loaded.
                var solution = await workspace.OpenSolutionAsync(solutionPath, new ConsoleProgressReporter());
                Console.WriteLine($"Finished loading solution '{solutionPath}'");

                AnalyzeProjects(ref solution, fixType);

                workspace.TryApplyChanges(solution);
            }
        }

        private static void AnalyzeProjects(ref Solution solution, FixType fixType)
        {
            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId);
                if (!projectList.Contains(project.Name))
                    continue;

                Helper.PrintLine("Analyzing project: " + project.Name, ConsoleColor.Green);

                var compilation = project.GetCompilationAsync().Result;
                AnalyzeDocuments(solution, ref project, compilation, fixType);
                solution = project.Solution;
            }
        }
        private static void AnalyzeDocuments(Solution solution, ref Project project, Compilation compilation, FixType fixType)
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

                var diagnostics = model.GetDiagnostics();
                if (diagnostics.Length > 0)
                    Helper.PrintLine("Diagnostic Errors: " + diagnostics.Length, ConsoleColor.Red);

                Helper.PrintLine("[" + documentIdx++ + "|" + documentCount + "] Analyzing file: " + document.Name, ConsoleColor.White);
                foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    Helper.PrintLine("\tAnalyzing method: " + method.Identifier + method.ParameterList, ConsoleColor.Gray);

                    AnalyzeMethod(solution, method, model, root, documentEditor, fixType);
                }

                document = documentEditor.GetChangedDocument();
                project = document.Project;
            }
        }

        private static void AnalyzeMethod(Solution solution, MethodDeclarationSyntax method, SemanticModel model, SyntaxNode root, DocumentEditor documentEditor, FixType fixType)
        {
            if (fixType == FixType.TMPArgs)
                TMPArgsFix.FixTmpArgs(solution, method, model, root, documentEditor);
            else if (fixType == FixType.SwapMethod)
                SwapMethodFix.FixSwapMethods(solution, method, model, root, documentEditor);
            else if (fixType == FixType.SwitchFixedString)
                SwitchFixedStringFix.FixSwitchFixedStrings(method, model, documentEditor);
            else if (fixType == FixType.FixedStringGetRef)
                FixedStringGetRefFix.FixFixedStringGetRef(solution, method, model, root, documentEditor);
            else if (fixType == FixType.LoopTo)
                LoopToFix.FixLoopTo(solution, method, model, root, documentEditor);
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

        private class ConsoleProgressReporter : IProgress<ProjectLoadProgress>
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
}
