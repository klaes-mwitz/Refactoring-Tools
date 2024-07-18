namespace CSEnumConverter
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.verticalSplit = new System.Windows.Forms.SplitContainer();
            this.label2 = new System.Windows.Forms.Label();
            this.txtOutput = new System.Windows.Forms.RichTextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnConvert = new System.Windows.Forms.Button();
            this.panelSettings = new System.Windows.Forms.Panel();
            this.tableTools = new System.Windows.Forms.TableLayoutPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.lblSolution = new System.Windows.Forms.Label();
            this.tableSolution = new System.Windows.Forms.TableLayoutPanel();
            this.btnOpenSolution = new System.Windows.Forms.Button();
            this.txtSolutionPath = new System.Windows.Forms.TextBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.lblWarningOrErrorCount = new System.Windows.Forms.Label();
            this.btnPreviousWarningOrError = new System.Windows.Forms.Button();
            this.btnNextWarningOrError = new System.Windows.Forms.Button();
            this.lblWarningsOrErrors = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.panelEditor = new System.Windows.Forms.Panel();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.txtEnum = new CSEnumConverter.CodeControl();
            ((System.ComponentModel.ISupportInitialize)(this.verticalSplit)).BeginInit();
            this.verticalSplit.Panel1.SuspendLayout();
            this.verticalSplit.Panel2.SuspendLayout();
            this.verticalSplit.SuspendLayout();
            this.panelSettings.SuspendLayout();
            this.tableTools.SuspendLayout();
            this.panel1.SuspendLayout();
            this.tableSolution.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panelEditor.SuspendLayout();
            this.SuspendLayout();
            // 
            // verticalSplit
            // 
            this.verticalSplit.BackColor = System.Drawing.Color.Transparent;
            this.verticalSplit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.verticalSplit.Location = new System.Drawing.Point(0, 0);
            this.verticalSplit.Name = "verticalSplit";
            // 
            // verticalSplit.Panel1
            // 
            this.verticalSplit.Panel1.Controls.Add(this.label2);
            this.verticalSplit.Panel1.Controls.Add(this.txtEnum);
            // 
            // verticalSplit.Panel2
            // 
            this.verticalSplit.Panel2.Controls.Add(this.txtOutput);
            this.verticalSplit.Panel2.Controls.Add(this.label1);
            this.verticalSplit.Size = new System.Drawing.Size(1264, 631);
            this.verticalSplit.SplitterDistance = 400;
            this.verticalSplit.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(0, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(34, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Enum";
            // 
            // txtOutput
            // 
            this.txtOutput.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(33)))), ((int)(((byte)(33)))));
            this.txtOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtOutput.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtOutput.ForeColor = System.Drawing.Color.White;
            this.txtOutput.Location = new System.Drawing.Point(0, 0);
            this.txtOutput.Name = "txtOutput";
            this.txtOutput.ReadOnly = true;
            this.txtOutput.Size = new System.Drawing.Size(860, 631);
            this.txtOutput.TabIndex = 3;
            this.txtOutput.Text = "";
            this.txtOutput.WordWrap = false;
            this.txtOutput.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.txtOutput_MouseDoubleClick);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(25, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Log";
            // 
            // btnConvert
            // 
            this.btnConvert.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnConvert.Location = new System.Drawing.Point(0, 0);
            this.btnConvert.Name = "btnConvert";
            this.btnConvert.Size = new System.Drawing.Size(94, 44);
            this.btnConvert.TabIndex = 3;
            this.btnConvert.Text = "Convert";
            this.btnConvert.UseVisualStyleBackColor = true;
            this.btnConvert.Click += new System.EventHandler(this.btnConvert_Click);
            // 
            // panelSettings
            // 
            this.panelSettings.Controls.Add(this.tableTools);
            this.panelSettings.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelSettings.Location = new System.Drawing.Point(0, 0);
            this.panelSettings.Name = "panelSettings";
            this.panelSettings.Size = new System.Drawing.Size(1264, 50);
            this.panelSettings.TabIndex = 4;
            // 
            // tableTools
            // 
            this.tableTools.ColumnCount = 3;
            this.tableTools.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableTools.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableTools.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableTools.Controls.Add(this.panel1, 0, 0);
            this.tableTools.Controls.Add(this.panel2, 1, 0);
            this.tableTools.Controls.Add(this.panel3, 2, 0);
            this.tableTools.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableTools.Location = new System.Drawing.Point(0, 0);
            this.tableTools.Name = "tableTools";
            this.tableTools.RowCount = 1;
            this.tableTools.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableTools.Size = new System.Drawing.Size(1264, 50);
            this.tableTools.TabIndex = 5;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.lblSolution);
            this.panel1.Controls.Add(this.tableSolution);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(576, 44);
            this.panel1.TabIndex = 0;
            // 
            // lblSolution
            // 
            this.lblSolution.AutoSize = true;
            this.lblSolution.Location = new System.Drawing.Point(0, 0);
            this.lblSolution.Name = "lblSolution";
            this.lblSolution.Size = new System.Drawing.Size(70, 13);
            this.lblSolution.TabIndex = 1;
            this.lblSolution.Text = "Solution Path";
            // 
            // tableSolution
            // 
            this.tableSolution.ColumnCount = 2;
            this.tableSolution.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableSolution.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.tableSolution.Controls.Add(this.btnOpenSolution, 0, 0);
            this.tableSolution.Controls.Add(this.txtSolutionPath, 0, 0);
            this.tableSolution.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tableSolution.Location = new System.Drawing.Point(0, 14);
            this.tableSolution.Name = "tableSolution";
            this.tableSolution.RowCount = 2;
            this.tableSolution.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableSolution.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableSolution.Size = new System.Drawing.Size(576, 30);
            this.tableSolution.TabIndex = 2;
            // 
            // btnOpenSolution
            // 
            this.btnOpenSolution.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnOpenSolution.Location = new System.Drawing.Point(544, 3);
            this.btnOpenSolution.MaximumSize = new System.Drawing.Size(30, 22);
            this.btnOpenSolution.MinimumSize = new System.Drawing.Size(30, 22);
            this.btnOpenSolution.Name = "btnOpenSolution";
            this.btnOpenSolution.Size = new System.Drawing.Size(30, 22);
            this.btnOpenSolution.TabIndex = 2;
            this.btnOpenSolution.Text = "...";
            this.btnOpenSolution.UseVisualStyleBackColor = true;
            this.btnOpenSolution.Click += new System.EventHandler(this.btnOpenSolution_Click);
            // 
            // txtSolutionPath
            // 
            this.txtSolutionPath.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSolutionPath.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtSolutionPath.Location = new System.Drawing.Point(3, 3);
            this.txtSolutionPath.Name = "txtSolutionPath";
            this.txtSolutionPath.Size = new System.Drawing.Size(535, 21);
            this.txtSolutionPath.TabIndex = 0;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.lblWarningOrErrorCount);
            this.panel2.Controls.Add(this.btnPreviousWarningOrError);
            this.panel2.Controls.Add(this.btnNextWarningOrError);
            this.panel2.Controls.Add(this.lblWarningsOrErrors);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(585, 3);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(576, 44);
            this.panel2.TabIndex = 1;
            // 
            // lblWarningOrErrorCount
            // 
            this.lblWarningOrErrorCount.AutoSize = true;
            this.lblWarningOrErrorCount.Location = new System.Drawing.Point(103, 0);
            this.lblWarningOrErrorCount.Name = "lblWarningOrErrorCount";
            this.lblWarningOrErrorCount.Size = new System.Drawing.Size(0, 13);
            this.lblWarningOrErrorCount.TabIndex = 5;
            // 
            // btnPreviousWarningOrError
            // 
            this.btnPreviousWarningOrError.Location = new System.Drawing.Point(3, 16);
            this.btnPreviousWarningOrError.Name = "btnPreviousWarningOrError";
            this.btnPreviousWarningOrError.Size = new System.Drawing.Size(46, 23);
            this.btnPreviousWarningOrError.TabIndex = 4;
            this.btnPreviousWarningOrError.Text = "◀";
            this.btnPreviousWarningOrError.UseVisualStyleBackColor = true;
            this.btnPreviousWarningOrError.Click += new System.EventHandler(this.btnPreviousWarningOrError_Click);
            // 
            // btnNextWarningOrError
            // 
            this.btnNextWarningOrError.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnNextWarningOrError.Location = new System.Drawing.Point(51, 15);
            this.btnNextWarningOrError.Name = "btnNextWarningOrError";
            this.btnNextWarningOrError.Size = new System.Drawing.Size(46, 23);
            this.btnNextWarningOrError.TabIndex = 3;
            this.btnNextWarningOrError.Text = "▶";
            this.btnNextWarningOrError.UseVisualStyleBackColor = true;
            this.btnNextWarningOrError.Click += new System.EventHandler(this.btnNextWarningOrError_Click);
            // 
            // lblWarningsOrErrors
            // 
            this.lblWarningsOrErrors.AutoSize = true;
            this.lblWarningsOrErrors.Location = new System.Drawing.Point(3, 0);
            this.lblWarningsOrErrors.Name = "lblWarningsOrErrors";
            this.lblWarningsOrErrors.Size = new System.Drawing.Size(94, 13);
            this.lblWarningsOrErrors.TabIndex = 2;
            this.lblWarningsOrErrors.Text = "Warnings or Errors";
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.btnConvert);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(1167, 3);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(94, 44);
            this.panel3.TabIndex = 2;
            // 
            // panelEditor
            // 
            this.panelEditor.Controls.Add(this.verticalSplit);
            this.panelEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelEditor.Location = new System.Drawing.Point(0, 50);
            this.panelEditor.Name = "panelEditor";
            this.panelEditor.Size = new System.Drawing.Size(1264, 631);
            this.panelEditor.TabIndex = 3;
            // 
            // txtEnum
            // 
            this.txtEnum.AutoSize = true;
            this.txtEnum.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.txtEnum.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtEnum.Location = new System.Drawing.Point(0, 0);
            this.txtEnum.MinimumSize = new System.Drawing.Size(300, 200);
            this.txtEnum.Name = "txtEnum";
            this.txtEnum.Size = new System.Drawing.Size(400, 631);
            this.txtEnum.TabIndex = 0;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1264, 681);
            this.Controls.Add(this.panelEditor);
            this.Controls.Add(this.panelSettings);
            this.MinimumSize = new System.Drawing.Size(720, 480);
            this.Name = "MainForm";
            this.Text = "MainForm";
            this.verticalSplit.Panel1.ResumeLayout(false);
            this.verticalSplit.Panel1.PerformLayout();
            this.verticalSplit.Panel2.ResumeLayout(false);
            this.verticalSplit.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.verticalSplit)).EndInit();
            this.verticalSplit.ResumeLayout(false);
            this.panelSettings.ResumeLayout(false);
            this.tableTools.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.tableSolution.ResumeLayout(false);
            this.tableSolution.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panelEditor.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private CodeControl txtEnum;
        private System.Windows.Forms.SplitContainer verticalSplit;
        private System.Windows.Forms.Button btnConvert;
        private System.Windows.Forms.Panel panelSettings;
        private System.Windows.Forms.Panel panelEditor;
        private System.Windows.Forms.TableLayoutPanel tableTools;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label lblSolution;
        private System.Windows.Forms.TextBox txtSolutionPath;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnOpenSolution;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.TableLayoutPanel tableSolution;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RichTextBox txtOutput;
        private System.Windows.Forms.Button btnPreviousWarningOrError;
        private System.Windows.Forms.Button btnNextWarningOrError;
        private System.Windows.Forms.Label lblWarningsOrErrors;
        private System.Windows.Forms.Label lblWarningOrErrorCount;
    }
}