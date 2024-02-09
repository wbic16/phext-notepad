namespace TerseNotepad
{
    partial class TerseForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            textBox = new RichTextBox();
            coordinateLabel = new Label();
            menuStrip = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            closeToolStripMenuItem = new ToolStripMenuItem();
            openToolStripMenuItem = new ToolStripMenuItem();
            reloadMenuItem = new ToolStripMenuItem();
            toolStripSeparator4 = new ToolStripSeparator();
            saveToolStripMenuItem = new ToolStripMenuItem();
            saveAsToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            recentToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            exitToolStripMenuItem = new ToolStripMenuItem();
            editToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator3 = new ToolStripSeparator();
            preferencesToolStripMenuItem = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            lockToScrollMenuItem = new ToolStripMenuItem();
            wordWrapToolStripMenuItem = new ToolStripMenuItem();
            darkModeMenuItem = new ToolStripMenuItem();
            showCoordinatesToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            defaultTerseToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            phextCoordinate = new TextBox();
            status = new Label();
            flowLayoutPanel = new FlowLayoutPanel();
            button1 = new Button();
            wordCountLabel = new Label();
            treeView = new TreeView();
            panel1 = new Panel();
            menuStrip.SuspendLayout();
            flowLayoutPanel.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // textBox
            // 
            textBox.AcceptsTab = true;
            textBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBox.BackColor = Color.Black;
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.Cursor = Cursors.IBeam;
            textBox.Font = new Font("Cascadia Code", 11F, FontStyle.Regular, GraphicsUnit.Point);
            textBox.ForeColor = Color.WhiteSmoke;
            textBox.Location = new Point(459, 5);
            textBox.MaxLength = 100000000;
            textBox.Name = "textBox";
            textBox.Size = new Size(960, 547);
            textBox.TabIndex = 0;
            textBox.Text = "";
            textBox.SelectionChanged += textBox_SelectionChanged;
            textBox.TextChanged += textBox_TextChanged;
            textBox.KeyDown += textBox_KeyDown;
            textBox.KeyUp += textBox_KeyUp;
            // 
            // coordinateLabel
            // 
            coordinateLabel.Anchor = AnchorStyles.Bottom;
            coordinateLabel.CausesValidation = false;
            coordinateLabel.Enabled = false;
            coordinateLabel.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point);
            coordinateLabel.Location = new Point(3, 3);
            coordinateLabel.Name = "coordinateLabel";
            coordinateLabel.Size = new Size(114, 36);
            coordinateLabel.TabIndex = 2;
            coordinateLabel.Text = "Coordinate:";
            coordinateLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // menuStrip
            // 
            menuStrip.BackColor = Color.DeepSkyBlue;
            menuStrip.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, editToolStripMenuItem, viewToolStripMenuItem, helpToolStripMenuItem });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Size = new Size(1384, 24);
            menuStrip.TabIndex = 3;
            menuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.BackColor = Color.DeepSkyBlue;
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { closeToolStripMenuItem, openToolStripMenuItem, reloadMenuItem, toolStripSeparator4, saveToolStripMenuItem, saveAsToolStripMenuItem, toolStripSeparator2, recentToolStripMenuItem, toolStripSeparator1, exitToolStripMenuItem });
            fileToolStripMenuItem.ForeColor = SystemColors.ControlText;
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "&File";
            // 
            // closeToolStripMenuItem
            // 
            closeToolStripMenuItem.BackColor = SystemColors.Menu;
            closeToolStripMenuItem.ForeColor = SystemColors.ControlText;
            closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            closeToolStripMenuItem.Size = new Size(123, 22);
            closeToolStripMenuItem.Text = "&New";
            closeToolStripMenuItem.Click += newToolStripMenuItem_Click;
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.BackColor = SystemColors.Menu;
            openToolStripMenuItem.ForeColor = SystemColors.ControlText;
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.Size = new Size(123, 22);
            openToolStripMenuItem.Text = "&Open...";
            openToolStripMenuItem.Click += openToolStripMenuItem_Click;
            // 
            // reloadMenuItem
            // 
            reloadMenuItem.Enabled = false;
            reloadMenuItem.Name = "reloadMenuItem";
            reloadMenuItem.Size = new Size(123, 22);
            reloadMenuItem.Text = "Re&load";
            reloadMenuItem.Click += reloadMenuItem_Click;
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new Size(120, 6);
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.BackColor = SystemColors.Menu;
            saveToolStripMenuItem.ForeColor = SystemColors.ControlText;
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.Size = new Size(123, 22);
            saveToolStripMenuItem.Text = "&Save";
            saveToolStripMenuItem.Click += saveToolStripMenuItem_Click;
            // 
            // saveAsToolStripMenuItem
            // 
            saveAsToolStripMenuItem.BackColor = SystemColors.Menu;
            saveAsToolStripMenuItem.ForeColor = SystemColors.ControlText;
            saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            saveAsToolStripMenuItem.Size = new Size(123, 22);
            saveAsToolStripMenuItem.Text = "Save &As...";
            saveAsToolStripMenuItem.Click += saveAsToolStripMenuItem_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.BackColor = Color.Black;
            toolStripSeparator2.ForeColor = Color.Black;
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(120, 6);
            // 
            // recentToolStripMenuItem
            // 
            recentToolStripMenuItem.BackColor = SystemColors.Menu;
            recentToolStripMenuItem.ForeColor = SystemColors.ControlText;
            recentToolStripMenuItem.Name = "recentToolStripMenuItem";
            recentToolStripMenuItem.Size = new Size(123, 22);
            recentToolStripMenuItem.Text = "&Recent";
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.BackColor = Color.Black;
            toolStripSeparator1.ForeColor = Color.Black;
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(120, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.BackColor = SystemColors.Menu;
            exitToolStripMenuItem.ForeColor = SystemColors.ControlText;
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(123, 22);
            exitToolStripMenuItem.Text = "E&xit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // editToolStripMenuItem
            // 
            editToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { toolStripSeparator3, preferencesToolStripMenuItem });
            editToolStripMenuItem.ForeColor = SystemColors.ControlText;
            editToolStripMenuItem.Name = "editToolStripMenuItem";
            editToolStripMenuItem.Size = new Size(39, 20);
            editToolStripMenuItem.Text = "&Edit";
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(141, 6);
            // 
            // preferencesToolStripMenuItem
            // 
            preferencesToolStripMenuItem.Name = "preferencesToolStripMenuItem";
            preferencesToolStripMenuItem.Size = new Size(144, 22);
            preferencesToolStripMenuItem.Text = "&Preferences...";
            preferencesToolStripMenuItem.Click += preferencesToolStripMenuItem_Click;
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { lockToScrollMenuItem, wordWrapToolStripMenuItem, darkModeMenuItem, showCoordinatesToolStripMenuItem });
            viewToolStripMenuItem.ForeColor = SystemColors.ControlText;
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(44, 20);
            viewToolStripMenuItem.Text = "&View";
            // 
            // lockToScrollMenuItem
            // 
            lockToScrollMenuItem.CheckOnClick = true;
            lockToScrollMenuItem.Name = "lockToScrollMenuItem";
            lockToScrollMenuItem.Size = new Size(170, 22);
            lockToScrollMenuItem.Text = "&Lock to Scroll";
            lockToScrollMenuItem.CheckedChanged += lockToScrollMenuItem_CheckedChanged;
            // 
            // wordWrapToolStripMenuItem
            // 
            wordWrapToolStripMenuItem.Checked = true;
            wordWrapToolStripMenuItem.CheckOnClick = true;
            wordWrapToolStripMenuItem.CheckState = CheckState.Checked;
            wordWrapToolStripMenuItem.Name = "wordWrapToolStripMenuItem";
            wordWrapToolStripMenuItem.Size = new Size(170, 22);
            wordWrapToolStripMenuItem.Text = "&Word Wrap";
            wordWrapToolStripMenuItem.Click += wordWrapToolStripMenuItem_Click;
            // 
            // darkModeMenuItem
            // 
            darkModeMenuItem.Checked = true;
            darkModeMenuItem.CheckOnClick = true;
            darkModeMenuItem.CheckState = CheckState.Checked;
            darkModeMenuItem.Name = "darkModeMenuItem";
            darkModeMenuItem.Size = new Size(170, 22);
            darkModeMenuItem.Text = "Dark &Mode";
            darkModeMenuItem.Click += darkModeMenuItem_Click;
            // 
            // showCoordinatesToolStripMenuItem
            // 
            showCoordinatesToolStripMenuItem.Checked = true;
            showCoordinatesToolStripMenuItem.CheckOnClick = true;
            showCoordinatesToolStripMenuItem.CheckState = CheckState.Checked;
            showCoordinatesToolStripMenuItem.Name = "showCoordinatesToolStripMenuItem";
            showCoordinatesToolStripMenuItem.Size = new Size(170, 22);
            showCoordinatesToolStripMenuItem.Text = "&Show Coordinates";
            showCoordinatesToolStripMenuItem.CheckedChanged += showCoordinatesToolStripMenuItem_CheckedChanged;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { defaultTerseToolStripMenuItem, aboutToolStripMenuItem });
            helpToolStripMenuItem.ForeColor = SystemColors.ControlText;
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(44, 20);
            helpToolStripMenuItem.Text = "&Help";
            // 
            // defaultTerseToolStripMenuItem
            // 
            defaultTerseToolStripMenuItem.Name = "defaultTerseToolStripMenuItem";
            defaultTerseToolStripMenuItem.Size = new Size(122, 22);
            defaultTerseToolStripMenuItem.Text = "&Contents";
            defaultTerseToolStripMenuItem.Click += defaultTerseToolStripMenuItem_Click;
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(122, 22);
            aboutToolStripMenuItem.Text = "&About";
            aboutToolStripMenuItem.Click += aboutToolStripMenuItem_Click;
            // 
            // phextCoordinate
            // 
            phextCoordinate.Anchor = AnchorStyles.Bottom;
            phextCoordinate.Enabled = false;
            phextCoordinate.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point);
            phextCoordinate.Location = new Point(123, 3);
            phextCoordinate.MaxLength = 64;
            phextCoordinate.Name = "phextCoordinate";
            phextCoordinate.Size = new Size(265, 33);
            phextCoordinate.TabIndex = 1;
            phextCoordinate.TextChanged += phextCoordinate_TextChanged;
            // 
            // status
            // 
            status.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            status.BorderStyle = BorderStyle.FixedSingle;
            status.CausesValidation = false;
            status.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
            status.Location = new Point(475, 6);
            status.Name = "status";
            status.Size = new Size(896, 27);
            status.TabIndex = 2;
            status.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // flowLayoutPanel
            // 
            flowLayoutPanel.Controls.Add(coordinateLabel);
            flowLayoutPanel.Controls.Add(phextCoordinate);
            flowLayoutPanel.Controls.Add(button1);
            flowLayoutPanel.Controls.Add(status);
            flowLayoutPanel.Controls.Add(wordCountLabel);
            flowLayoutPanel.Dock = DockStyle.Bottom;
            flowLayoutPanel.Location = new Point(0, 576);
            flowLayoutPanel.MaximumSize = new Size(1400, 65);
            flowLayoutPanel.MinimumSize = new Size(925, 65);
            flowLayoutPanel.Name = "flowLayoutPanel";
            flowLayoutPanel.Size = new Size(1384, 65);
            flowLayoutPanel.TabIndex = 11;
            // 
            // button1
            // 
            button1.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            button1.ForeColor = Color.Black;
            button1.Location = new Point(394, 3);
            button1.Name = "button1";
            button1.Size = new Size(75, 33);
            button1.TabIndex = 4;
            button1.Text = "Jump";
            button1.UseVisualStyleBackColor = true;
            button1.Click += jumpButton_Click;
            // 
            // wordCountLabel
            // 
            wordCountLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            wordCountLabel.BorderStyle = BorderStyle.FixedSingle;
            wordCountLabel.CausesValidation = false;
            wordCountLabel.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
            wordCountLabel.Location = new Point(3, 39);
            wordCountLabel.Name = "wordCountLabel";
            wordCountLabel.Size = new Size(1368, 23);
            wordCountLabel.TabIndex = 3;
            wordCountLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // treeView
            // 
            treeView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            treeView.BackColor = Color.Black;
            treeView.BorderStyle = BorderStyle.FixedSingle;
            treeView.ForeColor = Color.White;
            treeView.HideSelection = false;
            treeView.Location = new Point(3, 5);
            treeView.Name = "treeView";
            treeView.PathSeparator = "/";
            treeView.Size = new Size(450, 541);
            treeView.TabIndex = 29;
            treeView.AfterSelect += treeView_AfterSelect;
            treeView.DoubleClick += treeView_DoubleClick;
            treeView.KeyUp += treeView_KeyUp;
            // 
            // panel1
            // 
            panel1.Controls.Add(textBox);
            panel1.Controls.Add(treeView);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 24);
            panel1.Name = "panel1";
            panel1.Size = new Size(1384, 552);
            panel1.TabIndex = 30;
            // 
            // TerseForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ControlDark;
            ClientSize = new Size(1384, 641);
            Controls.Add(panel1);
            Controls.Add(flowLayoutPanel);
            Controls.Add(menuStrip);
            ForeColor = Color.White;
            MainMenuStrip = menuStrip;
            MinimumSize = new Size(850, 400);
            Name = "TerseForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Phext Notepad";
            FormClosing += TerseForm_FormClosing;
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            flowLayoutPanel.ResumeLayout(false);
            flowLayoutPanel.PerformLayout();
            panel1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private RichTextBox textBox;
        private Label coordinateLabel;
        private MenuStrip menuStrip;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem openToolStripMenuItem;
        private ToolStripMenuItem closeToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ToolStripMenuItem saveToolStripMenuItem;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem showCoordinatesToolStripMenuItem;
        private TextBox phextCoordinate;
        private Label status;
        private TreeView treeView;
        private FlowLayoutPanel flowLayoutPanel;
        private ToolStripMenuItem lockToScrollMenuItem;
        private ToolStripMenuItem editToolStripMenuItem;
        private ToolStripMenuItem preferencesToolStripMenuItem;
        private ToolStripMenuItem defaultTerseToolStripMenuItem;
        private ToolStripMenuItem saveAsToolStripMenuItem;
        private Panel panel1;
        private ToolStripMenuItem wordWrapToolStripMenuItem;
        private ToolStripMenuItem recentToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem darkModeMenuItem;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripMenuItem reloadMenuItem;
        private ToolStripSeparator toolStripSeparator4;
        private Label wordCountLabel;
        private Button button1;
    }
}