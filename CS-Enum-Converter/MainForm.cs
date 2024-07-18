using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace CSEnumConverter
{
    public partial class MainForm : Form
    {
        private Converter.Converter _converter = new Converter.Converter();
        Thread _analyzerThread;

        public MainForm()
        {
            InitializeComponent();

            Logger.Init(txtOutput);

            // For testing purposes
            txtSolutionPath.Text = Path.GetFullPath(Directory.GetCurrentDirectory() + "..\\..\\..\\..\\EnumConverterTestSolution\\EnumConverterTestSolution.sln");
            txtEnum.TextArea.Text = @"
namespace EnumConverterTestSolution
{
    public class EnumClass
    {
        public enum Enu_Test
        {
            NONE = 0,
            Bit1 = 1 << 0,
            Bit2 = 1 << 1,
            Bit3 = 1 << 2,
            Bit4 = 1 << 3,
            Bit5 = 1 << 4,
            Bit6 = 1 << 5,
            Bit7 = 1 << 6,
            Bit8 = 1 << 7
        }
    }
}";
        }

        private void btnConvert_Click(object sender, EventArgs e)
        {
            if (_analyzerThread != null && _analyzerThread.IsAlive)
                return;

            Logger.Clear();

            string enumText = txtEnum.TextArea.Text;
            string solutionPath = txtSolutionPath.Text;

            if (string.IsNullOrEmpty(enumText))
            {
                Logger.WriteError("Missing Enum Text");
                return;
            }

            if (string.IsNullOrEmpty(solutionPath))
            {
                Logger.WriteError("Missing Solution Path");
                return;
            }

            _analyzerThread = new Thread(() => _converter.ConvertCode(enumText, solutionPath));
            _analyzerThread.Start();
        }

        private void btnOpenSolution_Click(object sender, EventArgs e)
        {
            openFileDialog.Filter = "Visual Studio Solution (*.sln)|*.sln|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string solutionPath = openFileDialog.FileName;
                txtSolutionPath.Text = solutionPath;
            }
        }

        private void txtOutput_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int? logLineNumber = GetSelectedLogLineNumber();
                if (logLineNumber != null)
                    _converter.OpenEditInVisualStudio(logLineNumber.Value);

                UpdateCurrentWarningOrErrorIndex();
            }
        }

        private void btnNextWarningOrError_Click(object sender, EventArgs e)
        {
            var editLocation = LoggerEditLocations.GetNextWarningOrErrorLocation();
            if (editLocation != null)
                GotoEditLocation(editLocation.Value);
        }

        private void btnPreviousWarningOrError_Click(object sender, EventArgs e)
        {
            var editLocation = LoggerEditLocations.GetPreviousWarningOrErrorLocation();
            if (editLocation != null)
                GotoEditLocation(editLocation.Value);
        }

        private void GotoEditLocation(LoggerEditLocations.EditLocation editLocation)
        {
            var logLineNumber = editLocation.LogLineNumber;

            Regex getLogLine = new Regex($"{logLineNumber}\\s*\\|", RegexOptions.IgnoreCase);
            var matches = getLogLine.Matches(txtOutput.Text);
            if (matches.Count > 0)
            {
                var match = matches[0];

                var lineNumber = txtOutput.GetLineFromCharIndex(match.Index);
                var lineLength = txtOutput.Lines[lineNumber].Length;
                txtOutput.SelectionStart = match.Index;
                txtOutput.Select(match.Index, lineLength);
                txtOutput.Focus();
                txtOutput.ScrollToCaret();
            }

            UpdateCurrentWarningOrErrorIndex();

            _converter.OpenEditInVisualStudio(logLineNumber);
            Activate();
        }

        private void UpdateCurrentWarningOrErrorIndex()
        {
            lblWarningOrErrorCount.Text = $"{LoggerEditLocations.GetCurrentWarningOrErrorIndex() + 1}/{LoggerEditLocations.GetCurrentWarningOrErrorCount()}";
        }

        private int? GetSelectedLogLineNumber()
        {
            var lineNumber = txtOutput.GetLineFromCharIndex(txtOutput.SelectionStart);
            var lineText = txtOutput.Lines[lineNumber];

            Regex getLogNumber = new Regex(@"(\d+)\s*\|", RegexOptions.IgnoreCase);
            var matches = getLogNumber.Matches(lineText);
            if (matches.Count == 1)
            {
                var match = matches[0];
                var groups = match.Groups;

                if (groups.Count == 2)
                {
                    var group = groups[1];
                    return Convert.ToInt32(group.Value);
                }
            }

            return null;
        }
    }
}
