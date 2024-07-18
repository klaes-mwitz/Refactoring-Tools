using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace CSEnumConverter.Converter
{
    internal class CSharpSolution
    {
        public Solution CSSolution { get; set; }
        public MSBuildWorkspace Workspace { get; private set; }
        private readonly Dictionary<ProjectId, Compilation> _compilations = new Dictionary<ProjectId, Compilation>();

        /// <summary>
        /// Opens the solution and starts replacing literals with the correspodnig enum values
        /// </summary>
        /// <param name="path">The path of the solution file.</param>
        public void OpenSolution(string path)
        {
            path = Path.GetFullPath(path);
            if (!File.Exists(path))
            {
                Logger.WriteError("The solution path is not valid");
                return;
            }

            if (!MSBuildLocator.IsRegistered)
            {
                // Attempt to set the version of MSBuild.
                var visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
                var instance = visualStudioInstances.Length == 1
                    // If there is only one instance of MSBuild on this machine, set that as the one to use.
                    ? visualStudioInstances[0]
                    // Handle selecting the version of MSBuild you want to use.
                    : SelectVisualStudioInstance(visualStudioInstances);

                Logger.WriteLine($"Using MSBuild at '{instance.MSBuildPath}' to load projects.");

                // NOTE: Be sure to register an instance with the MSBuildLocator 
                //       before calling MSBuildWorkspace.Create()
                //       otherwise, MSBuildWorkspace won't MEF compose.

                MSBuildLocator.RegisterInstance(instance);
            }

            Workspace = MSBuildWorkspace.Create();

            // Print message for WorkspaceFailed event to help diagnosing project load failures.
            Workspace.WorkspaceFailed += (o, e) => Logger.WriteError(e.Diagnostic.Message, 1);

            // Attach progress reporter so we print projects as they are loaded.
            Logger.WriteLine(string.Format("Loading solution: {0}", path));
            Logger.WriteLine("Note: Following loading errors may be ignored", 1, Color.Aqua);
            CSSolution = Workspace.OpenSolutionAsync(path, new ConsoleProgressReporter()).Result;
            Logger.WriteLine(string.Format("Successfully loaded solution with {0} errors", Logger.ErrorCount));
            Logger.ResetCounter();

            CreateCompilations();
        }

        /// <summary>
        /// Returns the project compilation of the given node
        /// </summary>
        /// <param name="node">The node to get the project compilation from</param>
        /// <returns></returns>
        public Compilation GetCompilation(SyntaxNode node)
        {
            var documentId = CSSolution.GetDocumentId(node.SyntaxTree);
            return GetCompilation(documentId.ProjectId);
        }

        /// <summary>
        /// Returns the project compilation
        /// </summary>
        /// <param name="projectId">The project to get the compilation from</param>
        /// <returns></returns>
        public Compilation GetCompilation(ProjectId projectId)
        {
            return _compilations[projectId];
        }

        /// <summary>
        /// Retrieves the symbol information of the given node
        /// </summary>
        /// <param name="node">The node to get the symbolinfo from</param>
        /// <returns>The symbol info of the node</returns>
        public SymbolInfo GetSymbolInfo(SyntaxNode node)
        {
            var documentId = CSSolution.GetDocumentId(node.SyntaxTree);
            var model = GetCompilation(documentId.ProjectId).GetSemanticModel(node.SyntaxTree);

            return model.GetSymbolInfo(node);
        }

        /// <summary>
        /// Retrieves the symbol from a declaration node
        /// </summary>
        /// <param name="node">The node to get the symbol from</param>
        /// <returns>The symbol of the node</returns>
        public ISymbol GetDeclaredSymbol(SyntaxNode node)
        {
            var documentId = CSSolution.GetDocumentId(node.SyntaxTree);
            var model = GetCompilation(documentId.ProjectId).GetSemanticModel(node.SyntaxTree.GetRoot().SyntaxTree);

            return model.GetDeclaredSymbol(node);
        }

        /// <summary>
        /// Retrieves type information from the given node
        /// </summary>
        /// <param name="node">The node to get the type from</param>
        /// <returns>The Type info from the node</returns>
        public TypeInfo GetTypeInfo(SyntaxNode node)
        {
            var documentId = CSSolution.GetDocumentId(node.SyntaxTree);
            var model = GetCompilation(documentId.ProjectId).GetSemanticModel(node.SyntaxTree.GetRoot().SyntaxTree);

            return model.GetTypeInfo(node);
        }

        /// <summary>
        /// Closes the opened solution
        /// </summary>
        public void Close()
        {
            Workspace.Dispose();
            Workspace = null;
            CSSolution = null;
        }

        /// <summary>
        /// Opens the corresponding location of the given log line number in an Visual Studio instance
        /// </summary>
        /// <param name="logLineNumber"></param>
        public void OpenLocation(int logLineNumber)
        {
            var editLocation = LoggerEditLocations.GetEditLocation(logLineNumber);

            if (!File.Exists(editLocation.FilePath))
                return;

            try
            {
                EnvDTE.DTE dte2 = (EnvDTE80.DTE2)System.Runtime.InteropServices.Marshal.GetActiveObject("VisualStudio.DTE");
                dte2.MainWindow.Activate();
                EnvDTE.Window w = dte2.ItemOperations.OpenFile(editLocation.FilePath, EnvDTE.Constants.vsViewKindTextView);
                System.Threading.Thread.Sleep(50);
                ((EnvDTE.TextSelection)dte2.ActiveDocument.Selection).GotoLine(editLocation.FileLineNumber, true);
            }
            catch (Exception e)
            {
                Console.Write(e.Message);
            }
        }

        /// <summary>
        /// Creates compilations of all projects in the solution
        /// </summary>
        private void CreateCompilations()
        {
            foreach (var project in CSSolution.Projects)
            {
                var compilation = project.GetCompilationAsync().Result;
                _compilations.Add(project.Id, compilation);
            }
        }

        private VisualStudioInstance SelectVisualStudioInstance(VisualStudioInstance[] visualStudioInstances)
        {
            Logger.WriteLine("Multiple installs of MSBuild detected please select one:");
            for (int i = 0; i < visualStudioInstances.Length; i++)
            {
                Logger.WriteLine($"Instance {i + 1}");
                Logger.WriteLine($"    Name: {visualStudioInstances[i].Name}");
                Logger.WriteLine($"    Version: {visualStudioInstances[i].Version}");
                Logger.WriteLine($"    MSBuild Path: {visualStudioInstances[i].MSBuildPath}");
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

                Logger.WriteLine("Input not accepted, try again.");
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

                Logger.WriteLine($"{loadProgress.Operation,-15} {loadProgress.ElapsedTime,-15:m\\:ss\\.fffffff} {projectDisplay}", 1);
            }
        }
    }
}
