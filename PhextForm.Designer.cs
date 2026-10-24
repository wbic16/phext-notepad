namespace PhextNotepad
{
    partial class PhextForm
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
            showTreeViewToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            defaultPhextToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            phextCoordinate = new TextBox();
            status = new Label();
            flowLayoutPanel = new FlowLayoutPanel();
            labelApiKey = new Label();
            sqApiKey = new TextBox();
            wordCountLabel = new Label();
            jumpButton = new Button();
            label1 = new Label();
            sqHost = new TextBox();
            loadButton = new Button();
            pushButton = new Button();
            sqPhext = new TextBox();
            treeView = new TreeView();
            panel1 = new Panel();
            editorSplit = new SplitContainer();
            commandBar = new Panel();
            syncButton = new Button();
            menuStrip.SuspendLayout();
            flowLayoutPanel.SuspendLayout();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)editorSplit).BeginInit();
            editorSplit.Panel1.SuspendLayout();
            editorSplit.Panel2.SuspendLayout();
            editorSplit.SuspendLayout();
            commandBar.SuspendLayout();
            SuspendLayout();
            // 
            // textBox
            // 
            textBox.AcceptsTab = true;
            textBox.BackColor = Color.Black;
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.Cursor = Cursors.IBeam;
            textBox.Dock = DockStyle.Fill;
            textBox.Font = new Font("Cascadia Code", 11F);
            textBox.ForeColor = Color.WhiteSmoke;
            textBox.Location = new Point(0, 0);
            textBox.Margin = new Padding(5);
            textBox.MaxLength = 100000000;
            textBox.Name = "textBox";
            textBox.Size = new Size(1309, 898);
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
            coordinateLabel.Font = new Font("Segoe UI", 14.25F);
            coordinateLabel.Location = new Point(12, 5);
            coordinateLabel.Margin = new Padding(5, 0, 5, 0);
            coordinateLabel.Name = "coordinateLabel";
            coordinateLabel.Size = new Size(167, 49);
            coordinateLabel.TabIndex = 2;
            coordinateLabel.Text = "Coordinate:";
            coordinateLabel.TextAlign = ContentAlignment.BottomCenter;
            // 
            // menuStrip
            // 
            menuStrip.BackColor = Color.DeepSkyBlue;
            menuStrip.ImageScalingSize = new Size(32, 32);
            menuStrip.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, editToolStripMenuItem, viewToolStripMenuItem, helpToolStripMenuItem });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Padding = new Padding(8, 3, 0, 3);
            menuStrip.Size = new Size(1972, 35);
            menuStrip.TabIndex = 3;
            menuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.BackColor = Color.DeepSkyBlue;
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { closeToolStripMenuItem, openToolStripMenuItem, reloadMenuItem, toolStripSeparator4, saveToolStripMenuItem, saveAsToolStripMenuItem, toolStripSeparator2, recentToolStripMenuItem, toolStripSeparator1, exitToolStripMenuItem });
            fileToolStripMenuItem.ForeColor = SystemColors.ControlText;
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(54, 29);
            fileToolStripMenuItem.Text = "&File";
            // 
            // closeToolStripMenuItem
            // 
            closeToolStripMenuItem.BackColor = SystemColors.Menu;
            closeToolStripMenuItem.ForeColor = SystemColors.ControlText;
            closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            closeToolStripMenuItem.Size = new Size(188, 34);
            closeToolStripMenuItem.Text = "&New";
            closeToolStripMenuItem.Click += newToolStripMenuItem_Click;
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.BackColor = SystemColors.Menu;
            openToolStripMenuItem.ForeColor = SystemColors.ControlText;
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.Size = new Size(188, 34);
            openToolStripMenuItem.Text = "&Open...";
            openToolStripMenuItem.Click += openToolStripMenuItem_Click;
            // 
            // reloadMenuItem
            // 
            reloadMenuItem.Enabled = false;
            reloadMenuItem.Name = "reloadMenuItem";
            reloadMenuItem.Size = new Size(188, 34);
            reloadMenuItem.Text = "Re&load";
            reloadMenuItem.Click += reloadMenuItem_Click;
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new Size(185, 6);
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.BackColor = SystemColors.Menu;
            saveToolStripMenuItem.ForeColor = SystemColors.ControlText;
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.Size = new Size(188, 34);
            saveToolStripMenuItem.Text = "&Save";
            saveToolStripMenuItem.Click += saveToolStripMenuItem_Click;
            // 
            // saveAsToolStripMenuItem
            // 
            saveAsToolStripMenuItem.BackColor = SystemColors.Menu;
            saveAsToolStripMenuItem.ForeColor = SystemColors.ControlText;
            saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            saveAsToolStripMenuItem.Size = new Size(188, 34);
            saveAsToolStripMenuItem.Text = "Save &As...";
            saveAsToolStripMenuItem.Click += saveAsToolStripMenuItem_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.BackColor = Color.Black;
            toolStripSeparator2.ForeColor = Color.Black;
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(185, 6);
            // 
            // recentToolStripMenuItem
            // 
            recentToolStripMenuItem.BackColor = SystemColors.Menu;
            recentToolStripMenuItem.ForeColor = SystemColors.ControlText;
            recentToolStripMenuItem.Name = "recentToolStripMenuItem";
            recentToolStripMenuItem.Size = new Size(188, 34);
            recentToolStripMenuItem.Text = "&Recent";
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.BackColor = Color.Black;
            toolStripSeparator1.ForeColor = Color.Black;
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(185, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.BackColor = SystemColors.Menu;
            exitToolStripMenuItem.ForeColor = SystemColors.ControlText;
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(188, 34);
            exitToolStripMenuItem.Text = "E&xit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // editToolStripMenuItem
            // 
            editToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { toolStripSeparator3, preferencesToolStripMenuItem });
            editToolStripMenuItem.ForeColor = SystemColors.ControlText;
            editToolStripMenuItem.Name = "editToolStripMenuItem";
            editToolStripMenuItem.Size = new Size(58, 29);
            editToolStripMenuItem.Text = "&Edit";
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(213, 6);
            // 
            // preferencesToolStripMenuItem
            // 
            preferencesToolStripMenuItem.Name = "preferencesToolStripMenuItem";
            preferencesToolStripMenuItem.Size = new Size(216, 34);
            preferencesToolStripMenuItem.Text = "&Preferences...";
            preferencesToolStripMenuItem.Click += preferencesToolStripMenuItem_Click;
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { lockToScrollMenuItem, wordWrapToolStripMenuItem, darkModeMenuItem, showCoordinatesToolStripMenuItem, showTreeViewToolStripMenuItem });
            viewToolStripMenuItem.ForeColor = SystemColors.ControlText;
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(65, 29);
            viewToolStripMenuItem.Text = "&View";
            // 
            // lockToScrollMenuItem
            // 
            lockToScrollMenuItem.CheckOnClick = true;
            lockToScrollMenuItem.Name = "lockToScrollMenuItem";
            lockToScrollMenuItem.Size = new Size(270, 34);
            lockToScrollMenuItem.Text = "&Lock to Scroll";
            lockToScrollMenuItem.CheckedChanged += lockToScrollMenuItem_CheckedChanged;
            // 
            // wordWrapToolStripMenuItem
            // 
            wordWrapToolStripMenuItem.Checked = true;
            wordWrapToolStripMenuItem.CheckOnClick = true;
            wordWrapToolStripMenuItem.CheckState = CheckState.Checked;
            wordWrapToolStripMenuItem.Name = "wordWrapToolStripMenuItem";
            wordWrapToolStripMenuItem.Size = new Size(270, 34);
            wordWrapToolStripMenuItem.Text = "&Word Wrap";
            wordWrapToolStripMenuItem.Click += wordWrapToolStripMenuItem_Click;
            // 
            // darkModeMenuItem
            // 
            darkModeMenuItem.Checked = true;
            darkModeMenuItem.CheckOnClick = true;
            darkModeMenuItem.CheckState = CheckState.Checked;
            darkModeMenuItem.Name = "darkModeMenuItem";
            darkModeMenuItem.Size = new Size(270, 34);
            darkModeMenuItem.Text = "Dark &Mode";
            darkModeMenuItem.Click += darkModeMenuItem_Click;
            // 
            // showCoordinatesToolStripMenuItem
            // 
            showCoordinatesToolStripMenuItem.Checked = true;
            showCoordinatesToolStripMenuItem.CheckOnClick = true;
            showCoordinatesToolStripMenuItem.CheckState = CheckState.Checked;
            showCoordinatesToolStripMenuItem.Name = "showCoordinatesToolStripMenuItem";
            showCoordinatesToolStripMenuItem.Size = new Size(270, 34);
            showCoordinatesToolStripMenuItem.Text = "&Show Coordinates";
            showCoordinatesToolStripMenuItem.CheckedChanged += showCoordinatesToolStripMenuItem_CheckedChanged;
            // 
            // showTreeViewToolStripMenuItem
            // 
            showTreeViewToolStripMenuItem.Checked = true;
            showTreeViewToolStripMenuItem.CheckOnClick = true;
            showTreeViewToolStripMenuItem.CheckState = CheckState.Checked;
            showTreeViewToolStripMenuItem.Name = "showTreeViewToolStripMenuItem";
            showTreeViewToolStripMenuItem.Size = new Size(270, 34);
            showTreeViewToolStripMenuItem.Text = "Show Tree View";
            showTreeViewToolStripMenuItem.CheckedChanged += showTreeViewToolStripMenuItem_CheckedChanged;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { defaultPhextToolStripMenuItem, aboutToolStripMenuItem });
            helpToolStripMenuItem.ForeColor = SystemColors.ControlText;
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(65, 29);
            helpToolStripMenuItem.Text = "&Help";
            // 
            // defaultPhextToolStripMenuItem
            // 
            defaultPhextToolStripMenuItem.Name = "defaultPhextToolStripMenuItem";
            defaultPhextToolStripMenuItem.Size = new Size(185, 34);
            defaultPhextToolStripMenuItem.Text = "&Contents";
            defaultPhextToolStripMenuItem.Click += defaultPhextToolStripMenuItem_Click;
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(185, 34);
            aboutToolStripMenuItem.Text = "&About";
            aboutToolStripMenuItem.Click += aboutToolStripMenuItem_Click;
            // 
            // phextCoordinate
            // 
            phextCoordinate.Anchor = AnchorStyles.Bottom;
            phextCoordinate.Enabled = false;
            phextCoordinate.Font = new Font("Segoe UI", 14.25F);
            phextCoordinate.Location = new Point(188, 9);
            phextCoordinate.Margin = new Padding(5);
            phextCoordinate.MaxLength = 64;
            phextCoordinate.Name = "phextCoordinate";
            phextCoordinate.Size = new Size(362, 45);
            phextCoordinate.TabIndex = 1;
            phextCoordinate.TextChanged += phextCoordinate_TextChanged;
            // 
            // status
            // 
            status.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            status.BorderStyle = BorderStyle.FixedSingle;
            status.CausesValidation = false;
            status.Font = new Font("Segoe UI", 11F);
            status.Location = new Point(852, 6);
            status.Margin = new Padding(5, 0, 5, 0);
            status.Name = "status";
            status.Size = new Size(1106, 43);
            status.TabIndex = 2;
            status.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // flowLayoutPanel
            // 
            flowLayoutPanel.Controls.Add(labelApiKey);
            flowLayoutPanel.Controls.Add(sqApiKey);
            flowLayoutPanel.Controls.Add(status);
            flowLayoutPanel.Controls.Add(wordCountLabel);
            flowLayoutPanel.Dock = DockStyle.Bottom;
            flowLayoutPanel.Location = new Point(0, 1007);
            flowLayoutPanel.Margin = new Padding(5);
            flowLayoutPanel.MaximumSize = new Size(2000, 109);
            flowLayoutPanel.MinimumSize = new Size(1322, 109);
            flowLayoutPanel.Name = "flowLayoutPanel";
            flowLayoutPanel.Size = new Size(1972, 109);
            flowLayoutPanel.TabIndex = 11;
            // 
            // labelApiKey
            // 
            labelApiKey.Anchor = AnchorStyles.Bottom;
            labelApiKey.CausesValidation = false;
            labelApiKey.Enabled = false;
            labelApiKey.Font = new Font("Segoe UI", 14.25F);
            labelApiKey.Location = new Point(5, 6);
            labelApiKey.Margin = new Padding(5, 0, 5, 0);
            labelApiKey.Name = "labelApiKey";
            labelApiKey.Size = new Size(104, 49);
            labelApiKey.TabIndex = 2;
            labelApiKey.Text = "Token:";
            labelApiKey.TextAlign = ContentAlignment.BottomCenter;
            labelApiKey.Click += labelApiKey_Click;
            // 
            // sqApiKey
            // 
            sqApiKey.Anchor = AnchorStyles.Bottom;
            sqApiKey.Font = new Font("Segoe UI", 14.25F);
            sqApiKey.Location = new Point(119, 5);
            sqApiKey.Margin = new Padding(5);
            sqApiKey.MaxLength = 128;
            sqApiKey.Name = "sqApiKey";
            sqApiKey.Size = new Size(723, 45);
            sqApiKey.TabIndex = 1;
            sqApiKey.UseSystemPasswordChar = true;
            // 
            // wordCountLabel
            // 
            wordCountLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            wordCountLabel.BorderStyle = BorderStyle.FixedSingle;
            wordCountLabel.CausesValidation = false;
            wordCountLabel.Font = new Font("Segoe UI", 11F);
            wordCountLabel.Location = new Point(5, 55);
            wordCountLabel.Margin = new Padding(5, 0, 5, 0);
            wordCountLabel.Name = "wordCountLabel";
            wordCountLabel.Size = new Size(1954, 37);
            wordCountLabel.TabIndex = 3;
            wordCountLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // jumpButton
            // 
            jumpButton.Anchor = AnchorStyles.Bottom;
            jumpButton.Font = new Font("Segoe UI", 12F);
            jumpButton.ForeColor = Color.Black;
            jumpButton.Location = new Point(558, 9);
            jumpButton.Margin = new Padding(5);
            jumpButton.Name = "jumpButton";
            jumpButton.Size = new Size(88, 45);
            jumpButton.TabIndex = 4;
            jumpButton.Text = "Jump";
            jumpButton.UseVisualStyleBackColor = true;
            jumpButton.Click += jumpButton_Click;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Bottom;
            label1.CausesValidation = false;
            label1.Enabled = false;
            label1.Font = new Font("Segoe UI", 14.25F);
            label1.Location = new Point(655, 4);
            label1.Margin = new Padding(5, 0, 5, 0);
            label1.Name = "label1";
            label1.Size = new Size(139, 49);
            label1.TabIndex = 2;
            label1.Text = "SQ Host:";
            label1.TextAlign = ContentAlignment.BottomCenter;
            // 
            // sqHost
            // 
            sqHost.Anchor = AnchorStyles.Bottom;
            sqHost.Font = new Font("Segoe UI", 14.25F);
            sqHost.Location = new Point(804, 5);
            sqHost.Margin = new Padding(5);
            sqHost.MaxLength = 64;
            sqHost.Name = "sqHost";
            sqHost.Size = new Size(597, 45);
            sqHost.TabIndex = 1;
            sqHost.Text = "https://sq.mirrorborn.us/api/v2";
            // 
            // loadButton
            // 
            loadButton.Anchor = AnchorStyles.Bottom;
            loadButton.Font = new Font("Segoe UI", 12F);
            loadButton.ForeColor = Color.Black;
            loadButton.Location = new Point(1623, 6);
            loadButton.Margin = new Padding(5);
            loadButton.Name = "loadButton";
            loadButton.Size = new Size(107, 45);
            loadButton.TabIndex = 4;
            loadButton.Text = "Pull";
            loadButton.UseVisualStyleBackColor = true;
            loadButton.Click += pullButton_Click;
            // 
            // pushButton
            // 
            pushButton.Anchor = AnchorStyles.Bottom;
            pushButton.Font = new Font("Segoe UI", 12F);
            pushButton.ForeColor = Color.Black;
            pushButton.Location = new Point(1739, 6);
            pushButton.Margin = new Padding(5);
            pushButton.Name = "pushButton";
            pushButton.Size = new Size(107, 45);
            pushButton.TabIndex = 4;
            pushButton.Text = "Push";
            pushButton.UseVisualStyleBackColor = true;
            pushButton.Click += pushButton_Click;
            // 
            // sqPhext
            // 
            sqPhext.Anchor = AnchorStyles.Bottom;
            sqPhext.Font = new Font("Segoe UI", 14.25F);
            sqPhext.Location = new Point(1432, 6);
            sqPhext.Margin = new Padding(5);
            sqPhext.MaxLength = 64;
            sqPhext.Name = "sqPhext";
            sqPhext.Size = new Size(170, 45);
            sqPhext.TabIndex = 1;
            sqPhext.Text = "BASE";
            sqPhext.TextChanged += phextCoordinate_TextChanged;
            // 
            // treeView
            // 
            treeView.BackColor = Color.Black;
            treeView.BorderStyle = BorderStyle.FixedSingle;
            treeView.Dock = DockStyle.Fill;
            treeView.ForeColor = Color.White;
            treeView.HideSelection = false;
            treeView.Location = new Point(0, 0);
            treeView.Margin = new Padding(0);
            treeView.Name = "treeView";
            treeView.PathSeparator = "/";
            treeView.Size = new Size(642, 898);
            treeView.TabIndex = 29;
            treeView.AfterSelect += treeView_AfterSelect;
            treeView.DoubleClick += treeView_DoubleClick;
            treeView.KeyUp += treeView_KeyUp;
            // 
            // editorSplit
            // 
            editorSplit.BorderStyle = BorderStyle.None;
            editorSplit.Dock = DockStyle.Fill;
            editorSplit.FixedPanel = FixedPanel.Panel1;
            editorSplit.Location = new Point(0, 0);
            editorSplit.Margin = new Padding(0);
            editorSplit.Name = "editorSplit";
            editorSplit.Orientation = Orientation.Vertical;
            editorSplit.Size = new Size(1972, 908);
            editorSplit.SplitterDistance = 642;
            editorSplit.SplitterWidth = 6;
            editorSplit.TabIndex = 31;
            editorSplit.Panel1.Controls.Add(treeView);
            editorSplit.Panel2.Controls.Add(textBox);
            editorSplit.SplitterMoved += editorSplit_SplitterMoved;
            // 
            // commandBar
            // 
            commandBar.Controls.Add(coordinateLabel);
            commandBar.Controls.Add(phextCoordinate);
            commandBar.Controls.Add(jumpButton);
            commandBar.Controls.Add(label1);
            commandBar.Controls.Add(sqHost);
            commandBar.Controls.Add(sqPhext);
            commandBar.Controls.Add(loadButton);
            commandBar.Controls.Add(pushButton);
            commandBar.Controls.Add(syncButton);
            commandBar.Dock = DockStyle.Bottom;
            commandBar.Location = new Point(0, 908);
            commandBar.Margin = new Padding(0);
            commandBar.Name = "commandBar";
            commandBar.Size = new Size(1972, 64);
            commandBar.TabIndex = 32;
            // 
            // panel1
            // 
            panel1.Controls.Add(editorSplit);
            panel1.Controls.Add(commandBar);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 35);
            panel1.Margin = new Padding(5);
            panel1.Name = "panel1";
            panel1.Size = new Size(1972, 972);
            panel1.TabIndex = 30;
            // 
            // syncButton
            // 
            syncButton.Anchor = AnchorStyles.Bottom;
            syncButton.Font = new Font("Segoe UI", 12F);
            syncButton.ForeColor = Color.Black;
            syncButton.Location = new Point(1854, 6);
            syncButton.Margin = new Padding(5);
            syncButton.Name = "syncButton";
            syncButton.Size = new Size(107, 45);
            syncButton.TabIndex = 4;
            syncButton.Text = "Sync";
            syncButton.UseVisualStyleBackColor = true;
            syncButton.Click += syncButton_Click;
            // 
            // PhextForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ControlDark;
            ClientSize = new Size(1972, 1116);
            Controls.Add(panel1);
            Controls.Add(flowLayoutPanel);
            Controls.Add(menuStrip);
            ForeColor = Color.White;
            MainMenuStrip = menuStrip;
            Margin = new Padding(5);
            MinimumSize = new Size(1202, 616);
            Name = "PhextForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Phext Notepad";
            FormClosing += PhextForm_FormClosing;
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            flowLayoutPanel.ResumeLayout(false);
            flowLayoutPanel.PerformLayout();
            editorSplit.Panel1.ResumeLayout(false);
            editorSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)editorSplit).EndInit();
            editorSplit.ResumeLayout(false);
            commandBar.ResumeLayout(false);
            commandBar.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
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
        private ToolStripMenuItem defaultPhextToolStripMenuItem;
        private ToolStripMenuItem saveAsToolStripMenuItem;
        private Panel panel1;
        private SplitContainer editorSplit;
        private Panel commandBar;
        private ToolStripMenuItem wordWrapToolStripMenuItem;
        private ToolStripMenuItem recentToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem darkModeMenuItem;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripMenuItem reloadMenuItem;
        private ToolStripSeparator toolStripSeparator4;
        private Label wordCountLabel;
        private Button jumpButton;
        private Label label1;
        private TextBox sqHost;
        private Button loadButton;
        private Button pushButton;
        private TextBox sqPhext;
        private Label labelApiKey;
        private TextBox sqApiKey;
        private Button syncButton;
        private ToolStripMenuItem showTreeViewToolStripMenuItem;
    }
}