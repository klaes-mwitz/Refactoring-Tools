using Microsoft.CodeAnalysis;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CSEnumConverter
{
    internal static class Logger
    {
        public static int ErrorCount { get; private set; } = 0;

        public static int WarningCount { get; private set; } = 0;

        private static RichTextBox _logControl = null;
        private static int currentLine = 0;

        /// <summary>
        /// Initializes the logger with the specified log control.
        /// </summary>
        /// <param name="codeControl">The log control to initialize.</param>
        public static void Init(RichTextBox codeControl)
        {
            _logControl = codeControl;
        }

        /// <summary>
        /// Writes a line of text to the log control.
        /// </summary>
        /// <param name="text">The text to write.</param>
        /// <param name="indentLevel">The level of indentation for the text.</param>
        /// <param name="color">The color of the text.</param>
        /// <param name="node">The syntax node associated with the text.</param>
        public static void WriteLine(string text, int indentLevel = 0, Color? color = null, SyntaxNode node = null)
        {
            if (indentLevel != 0)
                text = new string('\t', indentLevel) + text;

            _logControl.Invoke((MethodInvoker)delegate
            {
                if (color == null)
                {
                    _logControl.AppendText(currentLine + "\t| " + $"{text}{Environment.NewLine}");
                }
                else
                {
                    _logControl.SuspendLayout();
                    _logControl.AppendText(currentLine + "\t| ");
                    _logControl.SelectionColor = (Color)color;
                    _logControl.AppendText($"{text}{Environment.NewLine}");
                    _logControl.ScrollToCaret();
                    _logControl.ResumeLayout();
                }
            });

            if (node != null)
            {
                LoggerEditLocations.AddNode(node, currentLine);
            }

            currentLine++;
        }

        /// <summary>
        /// Writes an error message to the log control.
        /// </summary>
        /// <param name="text">The error message to write.</param>
        /// <param name="indentLevel">The level of indentation for the error message.</param>
        /// <param name="node">The syntax node associated with the error message.</param>
        public static void WriteError(string text, int indentLevel = 0, SyntaxNode node = null)
        {
            ErrorCount++;
            if (node != null)
                LoggerEditLocations.AddWarningOrError(node, currentLine);

            WriteLine("Error: " + text, indentLevel, Color.Tomato, node);
        }

        /// <summary>
        /// Writes a warning message to the log control.
        /// </summary>
        /// <param name="text">The warning message to write.</param>
        /// <param name="indentLevel">The level of indentation for the warning message.</param>
        /// <param name="node">The syntax node associated with the warning message.</param>
        public static void WriteWarning(string text, int indentLevel = 0, SyntaxNode node = null)
        {
            WarningCount++;
            if (node != null)
                LoggerEditLocations.AddWarningOrError(node, currentLine);

            WriteLine("Warning: " + text, indentLevel, Color.Yellow, node);
        }

        /// <summary>
        /// Resets the error and warning counters.
        /// </summary>
        public static void ResetCounter()
        {
            WarningCount = 0;
            ErrorCount = 0;
        }

        /// <summary>
        /// Clears the log control and resets the error and warning counters.
        /// </summary>
        public static void Clear()
        {
            LoggerEditLocations.Clear();
            ResetCounter();
            _logControl.Text = "";
            currentLine = 0;
        }

        /// <summary>
        /// Saves the log to a file.
        /// </summary>
        public static void SaveLog()
        {
            DateTime currentDateTime = DateTime.Now;
            string formattedDateTime = currentDateTime.ToString("yyyy-MM-dd H-m-ss");

            string appPath = Path.GetDirectoryName(Application.ExecutablePath);
            string filePath = Path.Combine(appPath, "logs", formattedDateTime);
            filePath = Path.ChangeExtension(filePath, "txt");

            string text = "";
            _logControl.Invoke((MethodInvoker)delegate
            {
                text = _logControl.Text;
            });

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.Write(text);
            }
        }
    }
}
