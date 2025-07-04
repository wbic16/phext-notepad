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
            helpToolStripMenuItem = new ToolStripMenuItem();
            defaultPhextToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            phextCoordinate = new TextBox();
            status = new Label();
            flowLayoutPanel = new FlowLayoutPanel();
            wordCountLabel = new Label();
            jumpButton = new Button();
            label1 = new Label();
            sqHost = new TextBox();
            loadButton = new Button();
            pushButton = new Button();
            sqPhext = new TextBox();
            treeView = new TreeView();
            panel1 = new Panel();
            syncButton = new Button();
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
            textBox.Font = new Font("Cascadia Code", 11F);
            textBox.ForeColor = Color.WhiteSmoke;
            textBox.Location = new Point(852, 11);
            textBox.Margin = new Padding(6);
            textBox.MaxLength = 100000000;
            textBox.Name = "textBox";
            textBox.Size = new Size(1700, 1150);
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
            coordinateLabel.Location = new Point(15, 1170);
            coordinateLabel.Margin = new Padding(6, 0, 6, 0);
            coordinateLabel.Name = "coordinateLabel";
            coordinateLabel.Size = new Size(217, 63);
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
            menuStrip.Padding = new Padding(11, 4, 0, 4);
            menuStrip.Size = new Size(2564, 44);
            menuStrip.TabIndex = 3;
            menuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.BackColor = Color.DeepSkyBlue;
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { closeToolStripMenuItem, openToolStripMenuItem, reloadMenuItem, toolStripSeparator4, saveToolStripMenuItem, saveAsToolStripMenuItem, toolStripSeparator2, recentToolStripMenuItem, toolStripSeparator1, exitToolStripMenuItem });
            fileToolStripMenuItem.ForeColor = SystemColors.ControlText;
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(71, 36);
            fileToolStripMenuItem.Text = "&File";
            // 
            // closeToolStripMenuItem
            // 
            closeToolStripMenuItem.BackColor = SystemColors.Menu;
            closeToolStripMenuItem.ForeColor = SystemColors.ControlText;
            closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            closeToolStripMenuItem.Size = new Size(244, 44);
            closeToolStripMenuItem.Text = "&New";
            closeToolStripMenuItem.Click += newToolStripMenuItem_Click;
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.BackColor = SystemColors.Menu;
            openToolStripMenuItem.ForeColor = SystemColors.ControlText;
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.Size = new Size(244, 44);
            openToolStripMenuItem.Text = "&Open...";
            openToolStripMenuItem.Click += openToolStripMenuItem_Click;
            // 
            // reloadMenuItem
            // 
            reloadMenuItem.Enabled = false;
            reloadMenuItem.Name = "reloadMenuItem";
            reloadMenuItem.Size = new Size(244, 44);
            reloadMenuItem.Text = "Re&load";
            reloadMenuItem.Click += reloadMenuItem_Click;
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new Size(241, 6);
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.BackColor = SystemColors.Menu;
            saveToolStripMenuItem.ForeColor = SystemColors.ControlText;
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.Size = new Size(244, 44);
            saveToolStripMenuItem.Text = "&Save";
            saveToolStripMenuItem.Click += saveToolStripMenuItem_Click;
            // 
            // saveAsToolStripMenuItem
            // 
            saveAsToolStripMenuItem.BackColor = SystemColors.Menu;
            saveAsToolStripMenuItem.ForeColor = SystemColors.ControlText;
            saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            saveAsToolStripMenuItem.Size = new Size(244, 44);
            saveAsToolStripMenuItem.Text = "Save &As...";
            saveAsToolStripMenuItem.Click += saveAsToolStripMenuItem_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.BackColor = Color.Black;
            toolStripSeparator2.ForeColor = Color.Black;
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(241, 6);
            // 
            // recentToolStripMenuItem
            // 
            recentToolStripMenuItem.BackColor = SystemColors.Menu;
            recentToolStripMenuItem.ForeColor = SystemColors.ControlText;
            recentToolStripMenuItem.Name = "recentToolStripMenuItem";
            recentToolStripMenuItem.Size = new Size(244, 44);
            recentToolStripMenuItem.Text = "&Recent";
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.BackColor = Color.Black;
            toolStripSeparator1.ForeColor = Color.Black;
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(241, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.BackColor = SystemColors.Menu;
            exitToolStripMenuItem.ForeColor = SystemColors.ControlText;
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(244, 44);
            exitToolStripMenuItem.Text = "E&xit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // editToolStripMenuItem
            // 
            editToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { toolStripSeparator3, preferencesToolStripMenuItem });
            editToolStripMenuItem.ForeColor = SystemColors.ControlText;
            editToolStripMenuItem.Name = "editToolStripMenuItem";
            editToolStripMenuItem.Size = new Size(74, 36);
            editToolStripMenuItem.Text = "&Edit";
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(283, 6);
            // 
            // preferencesToolStripMenuItem
            // 
            preferencesToolStripMenuItem.Name = "preferencesToolStripMenuItem";
            preferencesToolStripMenuItem.Size = new Size(286, 44);
            preferencesToolStripMenuItem.Text = "&Preferences...";
            preferencesToolStripMenuItem.Click += preferencesToolStripMenuItem_Click;
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { lockToScrollMenuItem, wordWrapToolStripMenuItem, darkModeMenuItem, showCoordinatesToolStripMenuItem });
            viewToolStripMenuItem.ForeColor = SystemColors.ControlText;
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(85, 36);
            viewToolStripMenuItem.Text = "&View";
            // 
            // lockToScrollMenuItem
            // 
            lockToScrollMenuItem.CheckOnClick = true;
            lockToScrollMenuItem.Name = "lockToScrollMenuItem";
            lockToScrollMenuItem.Size = new Size(340, 44);
            lockToScrollMenuItem.Text = "&Lock to Scroll";
            lockToScrollMenuItem.CheckedChanged += lockToScrollMenuItem_CheckedChanged;
            // 
            // wordWrapToolStripMenuItem
            // 
            wordWrapToolStripMenuItem.Checked = true;
            wordWrapToolStripMenuItem.CheckOnClick = true;
            wordWrapToolStripMenuItem.CheckState = CheckState.Checked;
            wordWrapToolStripMenuItem.Name = "wordWrapToolStripMenuItem";
            wordWrapToolStripMenuItem.Size = new Size(340, 44);
            wordWrapToolStripMenuItem.Text = "&Word Wrap";
            wordWrapToolStripMenuItem.Click += wordWrapToolStripMenuItem_Click;
            // 
            // darkModeMenuItem
            // 
            darkModeMenuItem.Checked = true;
            darkModeMenuItem.CheckOnClick = true;
            darkModeMenuItem.CheckState = CheckState.Checked;
            darkModeMenuItem.Name = "darkModeMenuItem";
            darkModeMenuItem.Size = new Size(340, 44);
            darkModeMenuItem.Text = "Dark &Mode";
            darkModeMenuItem.Click += darkModeMenuItem_Click;
            // 
            // showCoordinatesToolStripMenuItem
            // 
            showCoordinatesToolStripMenuItem.Checked = true;
            showCoordinatesToolStripMenuItem.CheckOnClick = true;
            showCoordinatesToolStripMenuItem.CheckState = CheckState.Checked;
            showCoordinatesToolStripMenuItem.Name = "showCoordinatesToolStripMenuItem";
            showCoordinatesToolStripMenuItem.Size = new Size(340, 44);
            showCoordinatesToolStripMenuItem.Text = "&Show Coordinates";
            showCoordinatesToolStripMenuItem.CheckedChanged += showCoordinatesToolStripMenuItem_CheckedChanged;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { defaultPhextToolStripMenuItem, aboutToolStripMenuItem });
            helpToolStripMenuItem.ForeColor = SystemColors.ControlText;
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(84, 36);
            helpToolStripMenuItem.Text = "&Help";
            // 
            // defaultPhextToolStripMenuItem
            // 
            defaultPhextToolStripMenuItem.Name = "defaultPhextToolStripMenuItem";
            defaultPhextToolStripMenuItem.Size = new Size(243, 44);
            defaultPhextToolStripMenuItem.Text = "&Contents";
            defaultPhextToolStripMenuItem.Click += defaultPhextToolStripMenuItem_Click;
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(243, 44);
            aboutToolStripMenuItem.Text = "&About";
            aboutToolStripMenuItem.Click += aboutToolStripMenuItem_Click;
            // 
            // phextCoordinate
            // 
            phextCoordinate.Anchor = AnchorStyles.Bottom;
            phextCoordinate.Enabled = false;
            phextCoordinate.Font = new Font("Segoe UI", 14.25F);
            phextCoordinate.Location = new Point(244, 1175);
            phextCoordinate.Margin = new Padding(6);
            phextCoordinate.MaxLength = 64;
            phextCoordinate.Name = "phextCoordinate";
            phextCoordinate.Size = new Size(469, 58);
            phextCoordinate.TabIndex = 1;
            phextCoordinate.TextChanged += phextCoordinate_TextChanged;
            // 
            // status
            // 
            status.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            status.BorderStyle = BorderStyle.FixedSingle;
            status.CausesValidation = false;
            status.Font = new Font("Segoe UI", 11F);
            status.Location = new Point(6, 0);
            status.Margin = new Padding(6, 0, 6, 0);
            status.Name = "status";
            status.Size = new Size(1662, 55);
            status.TabIndex = 2;
            status.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // flowLayoutPanel
            // 
            flowLayoutPanel.Controls.Add(status);
            flowLayoutPanel.Controls.Add(wordCountLabel);
            flowLayoutPanel.Dock = DockStyle.Bottom;
            flowLayoutPanel.Location = new Point(0, 1290);
            flowLayoutPanel.Margin = new Padding(6);
            flowLayoutPanel.MaximumSize = new Size(2600, 139);
            flowLayoutPanel.MinimumSize = new Size(1718, 139);
            flowLayoutPanel.Name = "flowLayoutPanel";
            flowLayoutPanel.Size = new Size(2564, 139);
            flowLayoutPanel.TabIndex = 11;
            // 
            // wordCountLabel
            // 
            wordCountLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            wordCountLabel.BorderStyle = BorderStyle.FixedSingle;
            wordCountLabel.CausesValidation = false;
            wordCountLabel.Font = new Font("Segoe UI", 11F);
            wordCountLabel.Location = new Point(6, 55);
            wordCountLabel.Margin = new Padding(6, 0, 6, 0);
            wordCountLabel.Name = "wordCountLabel";
            wordCountLabel.Size = new Size(2539, 47);
            wordCountLabel.TabIndex = 3;
            wordCountLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // jumpButton
            // 
            jumpButton.Font = new Font("Segoe UI", 12F);
            jumpButton.ForeColor = Color.Black;
            jumpButton.Location = new Point(725, 1175);
            jumpButton.Margin = new Padding(6);
            jumpButton.Name = "jumpButton";
            jumpButton.Size = new Size(115, 58);
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
            label1.Location = new Point(852, 1169);
            label1.Margin = new Padding(6, 0, 6, 0);
            label1.Name = "label1";
            label1.Size = new Size(181, 63);
            label1.TabIndex = 2;
            label1.Text = "SQ Host:";
            label1.TextAlign = ContentAlignment.BottomCenter;
            // 
            // sqHost
            // 
            sqHost.Anchor = AnchorStyles.Bottom;
            sqHost.Font = new Font("Segoe UI", 14.25F);
            sqHost.Location = new Point(1045, 1170);
            sqHost.Margin = new Padding(6);
            sqHost.MaxLength = 64;
            sqHost.Name = "sqHost";
            sqHost.Size = new Size(691, 58);
            sqHost.TabIndex = 1;
            sqHost.Text = "http://127.0.0.1:1337/api/v2";
            // 
            // loadButton
            // 
            loadButton.Font = new Font("Segoe UI", 12F);
            loadButton.ForeColor = Color.Black;
            loadButton.Location = new Point(2110, 1171);
            loadButton.Margin = new Padding(6);
            loadButton.Name = "loadButton";
            loadButton.Size = new Size(139, 58);
            loadButton.TabIndex = 4;
            loadButton.Text = "Pull";
            loadButton.UseVisualStyleBackColor = true;
            loadButton.Click += pullButton_Click;
            // 
            // pushButton
            // 
            pushButton.Font = new Font("Segoe UI", 12F);
            pushButton.ForeColor = Color.Black;
            pushButton.Location = new Point(2261, 1171);
            pushButton.Margin = new Padding(6);
            pushButton.Name = "pushButton";
            pushButton.Size = new Size(139, 58);
            pushButton.TabIndex = 4;
            pushButton.Text = "Push";
            pushButton.UseVisualStyleBackColor = true;
            pushButton.Click += pushButton_Click;
            // 
            // sqPhext
            // 
            sqPhext.Anchor = AnchorStyles.Bottom;
            sqPhext.Font = new Font("Segoe UI", 14.25F);
            sqPhext.Location = new Point(1748, 1170);
            sqPhext.Margin = new Padding(6);
            sqPhext.MaxLength = 64;
            sqPhext.Name = "sqPhext";
            sqPhext.Size = new Size(350, 58);
            sqPhext.TabIndex = 1;
            sqPhext.Text = "index";
            sqPhext.TextChanged += phextCoordinate_TextChanged;
            // 
            // treeView
            // 
            treeView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            treeView.BackColor = Color.Black;
            treeView.BorderStyle = BorderStyle.FixedSingle;
            treeView.ForeColor = Color.White;
            treeView.HideSelection = false;
            treeView.Location = new Point(6, 11);
            treeView.Margin = new Padding(6);
            treeView.Name = "treeView";
            treeView.PathSeparator = "/";
            treeView.Size = new Size(834, 1150);
            treeView.TabIndex = 29;
            treeView.AfterSelect += treeView_AfterSelect;
            treeView.DoubleClick += treeView_DoubleClick;
            treeView.KeyUp += treeView_KeyUp;
            // 
            // panel1
            // 
            panel1.Controls.Add(jumpButton);
            panel1.Controls.Add(sqPhext);
            panel1.Controls.Add(loadButton);
            panel1.Controls.Add(sqHost);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(phextCoordinate);
            panel1.Controls.Add(coordinateLabel);
            panel1.Controls.Add(textBox);
            panel1.Controls.Add(syncButton);
            panel1.Controls.Add(pushButton);
            panel1.Controls.Add(treeView);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 44);
            panel1.Margin = new Padding(6);
            panel1.Name = "panel1";
            panel1.Size = new Size(2564, 1246);
            panel1.TabIndex = 30;
            // 
            // syncButton
            // 
            syncButton.Font = new Font("Segoe UI", 12F);
            syncButton.ForeColor = Color.Black;
            syncButton.Location = new Point(2410, 1171);
            syncButton.Margin = new Padding(6);
            syncButton.Name = "syncButton";
            syncButton.Size = new Size(139, 58);
            syncButton.TabIndex = 4;
            syncButton.Text = "Sync";
            syncButton.UseVisualStyleBackColor = true;
            syncButton.Click += syncButton_Click;
            // 
            // PhextForm
            // 
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ControlDark;
            ClientSize = new Size(2564, 1429);
            Controls.Add(panel1);
            Controls.Add(flowLayoutPanel);
            Controls.Add(menuStrip);
            ForeColor = Color.White;
            MainMenuStrip = menuStrip;
            Margin = new Padding(6);
            MinimumSize = new Size(1556, 773);
            Name = "PhextForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Phext Notepad";
            FormClosing += PhextForm_FormClosing;
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            flowLayoutPanel.ResumeLayout(false);
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
        private Button syncButton;
    }
}