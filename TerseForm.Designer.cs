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
            libraryLabel = new Label();
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
            libraryID = new TextBox();
            shelfLabel = new Label();
            shelfID = new TextBox();
            seriesLabel = new Label();
            seriesID = new TextBox();
            collectionLabel = new Label();
            collectionID = new TextBox();
            volumeLabel = new Label();
            volumeID = new TextBox();
            bookLabel = new Label();
            bookID = new TextBox();
            chapterLabel = new Label();
            sectionLabel = new Label();
            scrollLabel = new Label();
            status = new Label();
            chapterID = new TextBox();
            sectionID = new TextBox();
            scrollID = new TextBox();
            flowLayoutPanel = new FlowLayoutPanel();
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
            // libraryLabel
            // 
            libraryLabel.Anchor = AnchorStyles.Bottom;
            libraryLabel.CausesValidation = false;
            libraryLabel.Enabled = false;
            libraryLabel.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
            libraryLabel.Location = new Point(3, 9);
            libraryLabel.Name = "libraryLabel";
            libraryLabel.Size = new Size(80, 20);
            libraryLabel.TabIndex = 2;
            libraryLabel.Text = "Library:";
            libraryLabel.TextAlign = ContentAlignment.MiddleRight;
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
            toolStripSeparator3.Size = new Size(177, 6);
            // 
            // preferencesToolStripMenuItem
            // 
            preferencesToolStripMenuItem.Name = "preferencesToolStripMenuItem";
            preferencesToolStripMenuItem.Size = new Size(180, 22);
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
            // libraryID
            // 
            libraryID.Anchor = AnchorStyles.Bottom;
            libraryID.Enabled = false;
            libraryID.Location = new Point(89, 3);
            libraryID.Margin = new Padding(3, 3, 25, 3);
            libraryID.MaxLength = 4;
            libraryID.Name = "libraryID";
            libraryID.Size = new Size(55, 23);
            libraryID.TabIndex = 1;
            libraryID.KeyUp += libraryID_KeyUp;
            // 
            // shelfLabel
            // 
            shelfLabel.Anchor = AnchorStyles.Bottom;
            shelfLabel.CausesValidation = false;
            shelfLabel.Enabled = false;
            shelfLabel.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
            shelfLabel.Location = new Point(172, 9);
            shelfLabel.Name = "shelfLabel";
            shelfLabel.Size = new Size(80, 20);
            shelfLabel.TabIndex = 2;
            shelfLabel.Text = "Shelf:";
            shelfLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // shelfID
            // 
            shelfID.Anchor = AnchorStyles.Bottom;
            shelfID.Enabled = false;
            shelfID.Location = new Point(258, 3);
            shelfID.Margin = new Padding(3, 3, 25, 3);
            shelfID.MaxLength = 4;
            shelfID.Name = "shelfID";
            shelfID.Size = new Size(55, 23);
            shelfID.TabIndex = 2;
            shelfID.KeyUp += shelfID_KeyUp;
            // 
            // seriesLabel
            // 
            seriesLabel.Anchor = AnchorStyles.Bottom;
            seriesLabel.CausesValidation = false;
            seriesLabel.Enabled = false;
            seriesLabel.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
            seriesLabel.Location = new Point(341, 9);
            seriesLabel.Name = "seriesLabel";
            seriesLabel.Size = new Size(80, 20);
            seriesLabel.TabIndex = 2;
            seriesLabel.Text = "Series:";
            seriesLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // seriesID
            // 
            seriesID.Anchor = AnchorStyles.Bottom;
            seriesID.Enabled = false;
            seriesID.Location = new Point(427, 3);
            seriesID.Margin = new Padding(3, 3, 25, 3);
            seriesID.MaxLength = 4;
            seriesID.Name = "seriesID";
            seriesID.Size = new Size(55, 23);
            seriesID.TabIndex = 3;
            seriesID.KeyUp += seriesID_KeyUp;
            // 
            // collectionLabel
            // 
            collectionLabel.Anchor = AnchorStyles.Bottom;
            collectionLabel.CausesValidation = false;
            collectionLabel.Enabled = false;
            collectionLabel.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
            collectionLabel.Location = new Point(510, 9);
            collectionLabel.Name = "collectionLabel";
            collectionLabel.Size = new Size(80, 20);
            collectionLabel.TabIndex = 2;
            collectionLabel.Text = "Collection:";
            collectionLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // collectionID
            // 
            collectionID.Anchor = AnchorStyles.Bottom;
            collectionID.Enabled = false;
            collectionID.Location = new Point(596, 3);
            collectionID.Margin = new Padding(3, 3, 25, 3);
            collectionID.MaxLength = 4;
            collectionID.Name = "collectionID";
            collectionID.Size = new Size(55, 23);
            collectionID.TabIndex = 4;
            collectionID.KeyUp += collectionID_KeyUp;
            // 
            // volumeLabel
            // 
            volumeLabel.Anchor = AnchorStyles.Bottom;
            volumeLabel.CausesValidation = false;
            volumeLabel.Enabled = false;
            volumeLabel.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
            volumeLabel.Location = new Point(679, 9);
            volumeLabel.Name = "volumeLabel";
            volumeLabel.Size = new Size(80, 20);
            volumeLabel.TabIndex = 2;
            volumeLabel.Text = "Volume:";
            volumeLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // volumeID
            // 
            volumeID.Anchor = AnchorStyles.Bottom;
            volumeID.Enabled = false;
            volumeID.Location = new Point(765, 3);
            volumeID.Margin = new Padding(3, 3, 25, 3);
            volumeID.MaxLength = 4;
            volumeID.Name = "volumeID";
            volumeID.Size = new Size(55, 23);
            volumeID.TabIndex = 5;
            volumeID.KeyUp += volumeID_KeyUp;
            // 
            // bookLabel
            // 
            bookLabel.Anchor = AnchorStyles.Bottom;
            bookLabel.CausesValidation = false;
            bookLabel.Enabled = false;
            bookLabel.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
            bookLabel.Location = new Point(3, 38);
            bookLabel.Name = "bookLabel";
            bookLabel.Size = new Size(80, 20);
            bookLabel.TabIndex = 2;
            bookLabel.Text = "Book:";
            bookLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // bookID
            // 
            bookID.Anchor = AnchorStyles.Bottom;
            bookID.Enabled = false;
            bookID.Location = new Point(89, 32);
            bookID.Margin = new Padding(3, 3, 25, 3);
            bookID.MaxLength = 4;
            bookID.Name = "bookID";
            bookID.Size = new Size(55, 23);
            bookID.TabIndex = 6;
            bookID.KeyUp += bookID_KeyUp;
            // 
            // chapterLabel
            // 
            chapterLabel.Anchor = AnchorStyles.Bottom;
            chapterLabel.CausesValidation = false;
            chapterLabel.Enabled = false;
            chapterLabel.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
            chapterLabel.Location = new Point(172, 38);
            chapterLabel.Name = "chapterLabel";
            chapterLabel.Size = new Size(80, 20);
            chapterLabel.TabIndex = 2;
            chapterLabel.Text = "Chapter:";
            chapterLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // sectionLabel
            // 
            sectionLabel.Anchor = AnchorStyles.Bottom;
            sectionLabel.CausesValidation = false;
            sectionLabel.Enabled = false;
            sectionLabel.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
            sectionLabel.Location = new Point(341, 38);
            sectionLabel.Name = "sectionLabel";
            sectionLabel.Size = new Size(80, 20);
            sectionLabel.TabIndex = 2;
            sectionLabel.Text = "Section:";
            sectionLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // scrollLabel
            // 
            scrollLabel.Anchor = AnchorStyles.Bottom;
            scrollLabel.CausesValidation = false;
            scrollLabel.Enabled = false;
            scrollLabel.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
            scrollLabel.Location = new Point(510, 38);
            scrollLabel.Name = "scrollLabel";
            scrollLabel.Size = new Size(80, 20);
            scrollLabel.TabIndex = 2;
            scrollLabel.Text = "Scroll:";
            scrollLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // status
            // 
            status.Anchor = AnchorStyles.Bottom;
            status.CausesValidation = false;
            status.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
            status.Location = new Point(679, 38);
            status.Name = "status";
            status.Size = new Size(230, 20);
            status.TabIndex = 2;
            status.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // chapterID
            // 
            chapterID.Anchor = AnchorStyles.Bottom;
            chapterID.Enabled = false;
            chapterID.Location = new Point(258, 32);
            chapterID.Margin = new Padding(3, 3, 25, 3);
            chapterID.MaxLength = 4;
            chapterID.Name = "chapterID";
            chapterID.Size = new Size(55, 23);
            chapterID.TabIndex = 7;
            chapterID.KeyUp += chapterID_KeyUp;
            // 
            // sectionID
            // 
            sectionID.Anchor = AnchorStyles.Bottom;
            sectionID.Enabled = false;
            sectionID.Location = new Point(427, 32);
            sectionID.Margin = new Padding(3, 3, 25, 3);
            sectionID.MaxLength = 4;
            sectionID.Name = "sectionID";
            sectionID.Size = new Size(55, 23);
            sectionID.TabIndex = 8;
            sectionID.KeyUp += sectionID_KeyUp;
            // 
            // scrollID
            // 
            scrollID.Anchor = AnchorStyles.Bottom;
            scrollID.Enabled = false;
            scrollID.Location = new Point(596, 32);
            scrollID.Margin = new Padding(3, 3, 25, 3);
            scrollID.MaxLength = 4;
            scrollID.Name = "scrollID";
            scrollID.Size = new Size(55, 23);
            scrollID.TabIndex = 9;
            scrollID.KeyUp += scrollID_KeyUp;
            // 
            // flowLayoutPanel
            // 
            flowLayoutPanel.Controls.Add(libraryLabel);
            flowLayoutPanel.Controls.Add(libraryID);
            flowLayoutPanel.Controls.Add(shelfLabel);
            flowLayoutPanel.Controls.Add(shelfID);
            flowLayoutPanel.Controls.Add(seriesLabel);
            flowLayoutPanel.Controls.Add(seriesID);
            flowLayoutPanel.Controls.Add(collectionLabel);
            flowLayoutPanel.Controls.Add(collectionID);
            flowLayoutPanel.Controls.Add(volumeLabel);
            flowLayoutPanel.Controls.Add(volumeID);
            flowLayoutPanel.Controls.Add(wordCountLabel);
            flowLayoutPanel.Controls.Add(bookLabel);
            flowLayoutPanel.Controls.Add(bookID);
            flowLayoutPanel.Controls.Add(chapterLabel);
            flowLayoutPanel.Controls.Add(chapterID);
            flowLayoutPanel.Controls.Add(sectionLabel);
            flowLayoutPanel.Controls.Add(sectionID);
            flowLayoutPanel.Controls.Add(scrollLabel);
            flowLayoutPanel.Controls.Add(scrollID);
            flowLayoutPanel.Controls.Add(status);
            flowLayoutPanel.Dock = DockStyle.Bottom;
            flowLayoutPanel.Location = new Point(0, 576);
            flowLayoutPanel.MaximumSize = new Size(1100, 65);
            flowLayoutPanel.MinimumSize = new Size(925, 65);
            flowLayoutPanel.Name = "flowLayoutPanel";
            flowLayoutPanel.Size = new Size(1100, 65);
            flowLayoutPanel.TabIndex = 11;
            // 
            // wordCountLabel
            // 
            wordCountLabel.Anchor = AnchorStyles.Bottom;
            wordCountLabel.CausesValidation = false;
            wordCountLabel.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
            wordCountLabel.Location = new Point(848, 9);
            wordCountLabel.Name = "wordCountLabel";
            wordCountLabel.Size = new Size(239, 20);
            wordCountLabel.TabIndex = 10;
            wordCountLabel.TextAlign = ContentAlignment.MiddleCenter;
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
            treeView.Size = new Size(450, 547);
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
            Text = "Terse Notepad";
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
        private Label libraryLabel;
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
        private TextBox libraryID;
        private Label shelfLabel;
        private TextBox shelfID;
        private Label seriesLabel;
        private TextBox seriesID;
        private Label collectionLabel;
        private TextBox collectionID;
        private Label volumeLabel;
        private TextBox volumeID;
        private Label bookLabel;
        private TextBox bookID;
        private Label chapterLabel;
        private Label sectionLabel;
        private Label scrollLabel;
        private Label status;
        private TextBox chapterID;
        private TextBox sectionID;
        private TextBox scrollID;
        private VScrollBar scrollbar;
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
        private Label wordCountLabel;
        private ToolStripMenuItem darkModeMenuItem;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripMenuItem reloadMenuItem;
        private ToolStripSeparator toolStripSeparator4;
    }
}