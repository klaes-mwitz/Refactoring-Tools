using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace CSEnumConverter
{
    internal static class LoggerEditLocations
    {
        public struct EditLocation
        {
            public int LogLineNumber;
            public int FileLineNumber;
            public string FilePath;

            public EditLocation(int logLineNumber, int fileLineNumber, string filePath)
            {
                LogLineNumber = logLineNumber;
                FileLineNumber = fileLineNumber;
                FilePath = filePath;
            }
        }

        private static readonly List<EditLocation> _edits = new List<EditLocation>();
        private static readonly List<EditLocation> _warningsOrErrors = new List<EditLocation>();
        private static int _currentWarningOrErrorIndex = -1;

        /// <summary>
        /// Adds a node to the logger edit locations.
        /// </summary>
        /// <param name="node">The syntax node.</param>
        /// <param name="logLineNumber">The line number in the log.</param>
        /// <param name="isWarningOrError">Indicates whether the node is a warning or error.</param>
        public static void AddNode(SyntaxNode node, int logLineNumber, bool isWarningOrError = false)
        {
            string filePath = node.SyntaxTree.FilePath;
            var lineSpan = node.SyntaxTree.GetLineSpan(node.Span);
            int lineNumber = lineSpan.StartLinePosition.Line + 1;

            AddEdit(logLineNumber, lineNumber, filePath, isWarningOrError);
        }

        /// <summary>
        /// Adds a warning or error node to the logger edit locations.
        /// </summary>
        /// <param name="node">The syntax node.</param>
        /// <param name="logLineNumber">The line number in the log.</param>
        public static void AddWarningOrError(SyntaxNode node, int logLineNumber)
        {
            AddNode(node, logLineNumber, true);
        }

        /// <summary>
        /// Gets the edit location for the specified log line number.
        /// </summary>
        /// <param name="logLineNumber">The line number in the log.</param>
        /// <returns>The edit location.</returns>
        public static EditLocation GetEditLocation(int logLineNumber)
        {
            for (int i = 0; i < _warningsOrErrors.Count; i++)
            {
                if (_warningsOrErrors[i].LogLineNumber == logLineNumber)
                {
                    _currentWarningOrErrorIndex = i;
                    break;
                }
            }

            return _edits.Where(edit => edit.LogLineNumber == logLineNumber).FirstOrDefault();
        }

        /// <summary>
        /// Gets the next warning or error location.
        /// </summary>
        /// <returns>The next warning or error location.</returns>
        public static EditLocation? GetNextWarningOrErrorLocation()
        {
            if (_currentWarningOrErrorIndex < _warningsOrErrors.Count - 1)
                return _warningsOrErrors[++_currentWarningOrErrorIndex];

            return null;
        }

        /// <summary>
        /// Gets the previous warning or error location.
        /// </summary>
        /// <returns>The previous warning or error location.</returns>
        public static EditLocation? GetPreviousWarningOrErrorLocation()
        {
            if (_currentWarningOrErrorIndex > 0)
                return _warningsOrErrors[--_currentWarningOrErrorIndex];

            return null;
        }

        /// <summary>
        /// Gets the current warning or error index.
        /// </summary>
        /// <returns>The current warning or error index.</returns>
        public static int GetCurrentWarningOrErrorIndex()
        {
            return _currentWarningOrErrorIndex;
        }

        /// <summary>
        /// Gets the count of warnings or errors.
        /// </summary>
        /// <returns>The count of warnings or errors.</returns>
        public static int GetCurrentWarningOrErrorCount()
        {
            return _warningsOrErrors.Count;
        }

        /// <summary>
        /// Clears the logger edit locations.
        /// </summary>
        public static void Clear()
        {
            _edits.Clear();
            _warningsOrErrors.Clear();
            _currentWarningOrErrorIndex = -1;
        }

        /// <summary>
        /// Adds an edit to the logger edit locations.
        /// </summary>
        /// <param name="logLineNumber">The line number in the log.</param>
        /// <param name="fileLineNumber">The line number in the file.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="isWarningOrError">Indicates whether the edit is a warning or error.</param>
        private static void AddEdit(int logLineNumber, int fileLineNumber, string filePath, bool isWarningOrError = false)
        {
            if (isWarningOrError)
                _warningsOrErrors.Add(new EditLocation(logLineNumber, fileLineNumber, filePath));
            else
                _edits.Add(new EditLocation(logLineNumber, fileLineNumber, filePath));
        }
    }
}
