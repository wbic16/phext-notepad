﻿namespace TerseNotepad
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
            this.textBox = new System.Windows.Forms.RichTextBox();
            this.libraryLabel = new System.Windows.Forms.Label();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dimensionReportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.treeViewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.libraryID = new System.Windows.Forms.TextBox();
            this.shelfLabel = new System.Windows.Forms.Label();
            this.shelfID = new System.Windows.Forms.TextBox();
            this.seriesLabel = new System.Windows.Forms.Label();
            this.seriesID = new System.Windows.Forms.TextBox();
            this.collectionLabel = new System.Windows.Forms.Label();
            this.collectionID = new System.Windows.Forms.TextBox();
            this.volumeLabel = new System.Windows.Forms.Label();
            this.volumeID = new System.Windows.Forms.TextBox();
            this.bookLabel = new System.Windows.Forms.Label();
            this.bookID = new System.Windows.Forms.TextBox();
            this.chapterLabel = new System.Windows.Forms.Label();
            this.sectionLabel = new System.Windows.Forms.Label();
            this.pageLabel = new System.Windows.Forms.Label();
            this.status = new System.Windows.Forms.Label();
            this.chapterID = new System.Windows.Forms.TextBox();
            this.sectionID = new System.Windows.Forms.TextBox();
            this.scrollID = new System.Windows.Forms.TextBox();
            this.jumpButton = new System.Windows.Forms.Button();
            this.flowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.libraryScrollbar = new System.Windows.Forms.VScrollBar();
            this.shelfScrollbar = new System.Windows.Forms.VScrollBar();
            this.seriesScrollbar = new System.Windows.Forms.VScrollBar();
            this.collectionScrollbar = new System.Windows.Forms.VScrollBar();
            this.volumeScrollbar = new System.Windows.Forms.VScrollBar();
            this.bookScrollbar = new System.Windows.Forms.VScrollBar();
            this.sectionScrollbar = new System.Windows.Forms.VScrollBar();
            this.chapterScrollbar = new System.Windows.Forms.VScrollBar();
            this.treeView = new System.Windows.Forms.TreeView();
            this.menuStrip.SuspendLayout();
            this.flowLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // textBox
            // 
            this.textBox.Font = new System.Drawing.Font("Cascadia Code", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.textBox.Location = new System.Drawing.Point(256, 27);
            this.textBox.MaxLength = 100000000;
            this.textBox.Name = "textBox";
            this.textBox.Size = new System.Drawing.Size(917, 543);
            this.textBox.TabIndex = 0;
            this.textBox.Text = "";
            this.textBox.SelectionChanged += new System.EventHandler(this.textBox_SelectionChanged);
            this.textBox.TextChanged += new System.EventHandler(this.textBox_TextChanged);
            this.textBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox_KeyDown);
            this.textBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.textBox_KeyUp);
            // 
            // libraryLabel
            // 
            this.libraryLabel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.libraryLabel.CausesValidation = false;
            this.libraryLabel.Enabled = false;
            this.libraryLabel.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.libraryLabel.Location = new System.Drawing.Point(3, 9);
            this.libraryLabel.Name = "libraryLabel";
            this.libraryLabel.Size = new System.Drawing.Size(80, 20);
            this.libraryLabel.TabIndex = 2;
            this.libraryLabel.Text = "Library:";
            this.libraryLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(1384, 24);
            this.menuStrip.TabIndex = 3;
            this.menuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.closeToolStripMenuItem,
            this.openToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // closeToolStripMenuItem
            // 
            this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            this.closeToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.closeToolStripMenuItem.Text = "&New";
            this.closeToolStripMenuItem.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.openToolStripMenuItem.Text = "&Open Terse File...";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.saveToolStripMenuItem.Text = "&Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.dimensionReportToolStripMenuItem,
            this.treeViewToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "&View";
            // 
            // dimensionReportToolStripMenuItem
            // 
            this.dimensionReportToolStripMenuItem.Name = "dimensionReportToolStripMenuItem";
            this.dimensionReportToolStripMenuItem.Size = new System.Drawing.Size(169, 22);
            this.dimensionReportToolStripMenuItem.Text = "&Dimension Report";
            this.dimensionReportToolStripMenuItem.Click += new System.EventHandler(this.dimensionReportToolStripMenuItem_Click);
            // 
            // treeViewToolStripMenuItem
            // 
            this.treeViewToolStripMenuItem.Checked = true;
            this.treeViewToolStripMenuItem.CheckOnClick = true;
            this.treeViewToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.treeViewToolStripMenuItem.Name = "treeViewToolStripMenuItem";
            this.treeViewToolStripMenuItem.Size = new System.Drawing.Size(169, 22);
            this.treeViewToolStripMenuItem.Text = "Tree View";
            this.treeViewToolStripMenuItem.Click += new System.EventHandler(this.treeViewToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
            this.aboutToolStripMenuItem.Text = "About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // libraryID
            // 
            this.libraryID.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.libraryID.Enabled = false;
            this.libraryID.Location = new System.Drawing.Point(89, 3);
            this.libraryID.Margin = new System.Windows.Forms.Padding(3, 3, 25, 3);
            this.libraryID.MaxLength = 4;
            this.libraryID.Name = "libraryID";
            this.libraryID.Size = new System.Drawing.Size(55, 23);
            this.libraryID.TabIndex = 1;
            // 
            // shelfLabel
            // 
            this.shelfLabel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.shelfLabel.CausesValidation = false;
            this.shelfLabel.Enabled = false;
            this.shelfLabel.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.shelfLabel.Location = new System.Drawing.Point(172, 9);
            this.shelfLabel.Name = "shelfLabel";
            this.shelfLabel.Size = new System.Drawing.Size(80, 20);
            this.shelfLabel.TabIndex = 2;
            this.shelfLabel.Text = "Shelf:";
            this.shelfLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // shelfID
            // 
            this.shelfID.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.shelfID.Enabled = false;
            this.shelfID.Location = new System.Drawing.Point(258, 3);
            this.shelfID.Margin = new System.Windows.Forms.Padding(3, 3, 25, 3);
            this.shelfID.MaxLength = 4;
            this.shelfID.Name = "shelfID";
            this.shelfID.Size = new System.Drawing.Size(55, 23);
            this.shelfID.TabIndex = 2;
            // 
            // seriesLabel
            // 
            this.seriesLabel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.seriesLabel.CausesValidation = false;
            this.seriesLabel.Enabled = false;
            this.seriesLabel.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.seriesLabel.Location = new System.Drawing.Point(341, 9);
            this.seriesLabel.Name = "seriesLabel";
            this.seriesLabel.Size = new System.Drawing.Size(80, 20);
            this.seriesLabel.TabIndex = 2;
            this.seriesLabel.Text = "Series:";
            this.seriesLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // seriesID
            // 
            this.seriesID.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.seriesID.Enabled = false;
            this.seriesID.Location = new System.Drawing.Point(427, 3);
            this.seriesID.Margin = new System.Windows.Forms.Padding(3, 3, 25, 3);
            this.seriesID.MaxLength = 4;
            this.seriesID.Name = "seriesID";
            this.seriesID.Size = new System.Drawing.Size(55, 23);
            this.seriesID.TabIndex = 3;
            // 
            // collectionLabel
            // 
            this.collectionLabel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.collectionLabel.CausesValidation = false;
            this.collectionLabel.Enabled = false;
            this.collectionLabel.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.collectionLabel.Location = new System.Drawing.Point(510, 9);
            this.collectionLabel.Name = "collectionLabel";
            this.collectionLabel.Size = new System.Drawing.Size(80, 20);
            this.collectionLabel.TabIndex = 2;
            this.collectionLabel.Text = "Collection:";
            this.collectionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // collectionID
            // 
            this.collectionID.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.collectionID.Enabled = false;
            this.collectionID.Location = new System.Drawing.Point(596, 3);
            this.collectionID.Margin = new System.Windows.Forms.Padding(3, 3, 25, 3);
            this.collectionID.MaxLength = 4;
            this.collectionID.Name = "collectionID";
            this.collectionID.Size = new System.Drawing.Size(55, 23);
            this.collectionID.TabIndex = 4;
            // 
            // volumeLabel
            // 
            this.volumeLabel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.volumeLabel.CausesValidation = false;
            this.volumeLabel.Enabled = false;
            this.volumeLabel.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.volumeLabel.Location = new System.Drawing.Point(679, 9);
            this.volumeLabel.Name = "volumeLabel";
            this.volumeLabel.Size = new System.Drawing.Size(80, 20);
            this.volumeLabel.TabIndex = 2;
            this.volumeLabel.Text = "Volume:";
            this.volumeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // volumeID
            // 
            this.volumeID.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.volumeID.Enabled = false;
            this.volumeID.Location = new System.Drawing.Point(765, 3);
            this.volumeID.Margin = new System.Windows.Forms.Padding(3, 3, 25, 3);
            this.volumeID.MaxLength = 4;
            this.volumeID.Name = "volumeID";
            this.volumeID.Size = new System.Drawing.Size(55, 23);
            this.volumeID.TabIndex = 5;
            // 
            // bookLabel
            // 
            this.bookLabel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.bookLabel.CausesValidation = false;
            this.bookLabel.Enabled = false;
            this.bookLabel.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.bookLabel.Location = new System.Drawing.Point(3, 38);
            this.bookLabel.Name = "bookLabel";
            this.bookLabel.Size = new System.Drawing.Size(80, 20);
            this.bookLabel.TabIndex = 2;
            this.bookLabel.Text = "Book:";
            this.bookLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // bookID
            // 
            this.bookID.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.bookID.Enabled = false;
            this.bookID.Location = new System.Drawing.Point(89, 32);
            this.bookID.Margin = new System.Windows.Forms.Padding(3, 3, 25, 3);
            this.bookID.MaxLength = 4;
            this.bookID.Name = "bookID";
            this.bookID.Size = new System.Drawing.Size(55, 23);
            this.bookID.TabIndex = 6;
            // 
            // chapterLabel
            // 
            this.chapterLabel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.chapterLabel.CausesValidation = false;
            this.chapterLabel.Enabled = false;
            this.chapterLabel.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.chapterLabel.Location = new System.Drawing.Point(172, 38);
            this.chapterLabel.Name = "chapterLabel";
            this.chapterLabel.Size = new System.Drawing.Size(80, 20);
            this.chapterLabel.TabIndex = 2;
            this.chapterLabel.Text = "Chapter:";
            this.chapterLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // sectionLabel
            // 
            this.sectionLabel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.sectionLabel.CausesValidation = false;
            this.sectionLabel.Enabled = false;
            this.sectionLabel.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.sectionLabel.Location = new System.Drawing.Point(341, 38);
            this.sectionLabel.Name = "sectionLabel";
            this.sectionLabel.Size = new System.Drawing.Size(80, 20);
            this.sectionLabel.TabIndex = 2;
            this.sectionLabel.Text = "Section:";
            this.sectionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // pageLabel
            // 
            this.pageLabel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.pageLabel.CausesValidation = false;
            this.pageLabel.Enabled = false;
            this.pageLabel.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.pageLabel.Location = new System.Drawing.Point(510, 38);
            this.pageLabel.Name = "pageLabel";
            this.pageLabel.Size = new System.Drawing.Size(80, 20);
            this.pageLabel.TabIndex = 2;
            this.pageLabel.Text = "Scroll:";
            this.pageLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // status
            // 
            this.status.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.status.CausesValidation = false;
            this.status.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.status.Location = new System.Drawing.Point(765, 38);
            this.status.Name = "status";
            this.status.Size = new System.Drawing.Size(147, 20);
            this.status.TabIndex = 2;
            this.status.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // chapterID
            // 
            this.chapterID.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.chapterID.Enabled = false;
            this.chapterID.Location = new System.Drawing.Point(258, 32);
            this.chapterID.Margin = new System.Windows.Forms.Padding(3, 3, 25, 3);
            this.chapterID.MaxLength = 4;
            this.chapterID.Name = "chapterID";
            this.chapterID.Size = new System.Drawing.Size(55, 23);
            this.chapterID.TabIndex = 7;
            // 
            // sectionID
            // 
            this.sectionID.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.sectionID.Enabled = false;
            this.sectionID.Location = new System.Drawing.Point(427, 32);
            this.sectionID.Margin = new System.Windows.Forms.Padding(3, 3, 25, 3);
            this.sectionID.MaxLength = 4;
            this.sectionID.Name = "sectionID";
            this.sectionID.Size = new System.Drawing.Size(55, 23);
            this.sectionID.TabIndex = 8;
            // 
            // scrollID
            // 
            this.scrollID.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.scrollID.Enabled = false;
            this.scrollID.Location = new System.Drawing.Point(596, 32);
            this.scrollID.Margin = new System.Windows.Forms.Padding(3, 3, 25, 3);
            this.scrollID.MaxLength = 4;
            this.scrollID.Name = "scrollID";
            this.scrollID.Size = new System.Drawing.Size(55, 23);
            this.scrollID.TabIndex = 9;
            // 
            // jumpButton
            // 
            this.jumpButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.jumpButton.Location = new System.Drawing.Point(679, 32);
            this.jumpButton.Name = "jumpButton";
            this.jumpButton.Size = new System.Drawing.Size(80, 23);
            this.jumpButton.TabIndex = 10;
            this.jumpButton.Text = "Jump";
            this.jumpButton.UseVisualStyleBackColor = true;
            this.jumpButton.Click += new System.EventHandler(this.jumpButton_Click);
            // 
            // flowLayoutPanel
            // 
            this.flowLayoutPanel.Controls.Add(this.libraryLabel);
            this.flowLayoutPanel.Controls.Add(this.libraryID);
            this.flowLayoutPanel.Controls.Add(this.shelfLabel);
            this.flowLayoutPanel.Controls.Add(this.shelfID);
            this.flowLayoutPanel.Controls.Add(this.seriesLabel);
            this.flowLayoutPanel.Controls.Add(this.seriesID);
            this.flowLayoutPanel.Controls.Add(this.collectionLabel);
            this.flowLayoutPanel.Controls.Add(this.collectionID);
            this.flowLayoutPanel.Controls.Add(this.volumeLabel);
            this.flowLayoutPanel.Controls.Add(this.volumeID);
            this.flowLayoutPanel.Controls.Add(this.bookLabel);
            this.flowLayoutPanel.Controls.Add(this.bookID);
            this.flowLayoutPanel.Controls.Add(this.chapterLabel);
            this.flowLayoutPanel.Controls.Add(this.chapterID);
            this.flowLayoutPanel.Controls.Add(this.sectionLabel);
            this.flowLayoutPanel.Controls.Add(this.sectionID);
            this.flowLayoutPanel.Controls.Add(this.pageLabel);
            this.flowLayoutPanel.Controls.Add(this.scrollID);
            this.flowLayoutPanel.Controls.Add(this.jumpButton);
            this.flowLayoutPanel.Controls.Add(this.status);
            this.flowLayoutPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flowLayoutPanel.Location = new System.Drawing.Point(250, 576);
            this.flowLayoutPanel.MaximumSize = new System.Drawing.Size(925, 65);
            this.flowLayoutPanel.MinimumSize = new System.Drawing.Size(925, 65);
            this.flowLayoutPanel.Name = "flowLayoutPanel";
            this.flowLayoutPanel.Size = new System.Drawing.Size(925, 65);
            this.flowLayoutPanel.TabIndex = 11;
            // 
            // libraryScrollbar
            // 
            this.libraryScrollbar.Dock = System.Windows.Forms.DockStyle.Right;
            this.libraryScrollbar.Location = new System.Drawing.Point(1358, 24);
            this.libraryScrollbar.Name = "libraryScrollbar";
            this.libraryScrollbar.Size = new System.Drawing.Size(26, 617);
            this.libraryScrollbar.TabIndex = 12;
            // 
            // shelfScrollbar
            // 
            this.shelfScrollbar.Dock = System.Windows.Forms.DockStyle.Right;
            this.shelfScrollbar.Location = new System.Drawing.Point(1332, 24);
            this.shelfScrollbar.Name = "shelfScrollbar";
            this.shelfScrollbar.Size = new System.Drawing.Size(26, 617);
            this.shelfScrollbar.TabIndex = 13;
            // 
            // seriesScrollbar
            // 
            this.seriesScrollbar.Dock = System.Windows.Forms.DockStyle.Right;
            this.seriesScrollbar.Location = new System.Drawing.Point(1306, 24);
            this.seriesScrollbar.Name = "seriesScrollbar";
            this.seriesScrollbar.Size = new System.Drawing.Size(26, 617);
            this.seriesScrollbar.TabIndex = 14;
            // 
            // collectionScrollbar
            // 
            this.collectionScrollbar.Dock = System.Windows.Forms.DockStyle.Right;
            this.collectionScrollbar.Location = new System.Drawing.Point(1280, 24);
            this.collectionScrollbar.Name = "collectionScrollbar";
            this.collectionScrollbar.Size = new System.Drawing.Size(26, 617);
            this.collectionScrollbar.TabIndex = 15;
            // 
            // volumeScrollbar
            // 
            this.volumeScrollbar.Dock = System.Windows.Forms.DockStyle.Right;
            this.volumeScrollbar.Location = new System.Drawing.Point(1254, 24);
            this.volumeScrollbar.Name = "volumeScrollbar";
            this.volumeScrollbar.Size = new System.Drawing.Size(26, 617);
            this.volumeScrollbar.TabIndex = 23;
            // 
            // bookScrollbar
            // 
            this.bookScrollbar.Dock = System.Windows.Forms.DockStyle.Right;
            this.bookScrollbar.Location = new System.Drawing.Point(1228, 24);
            this.bookScrollbar.Name = "bookScrollbar";
            this.bookScrollbar.Size = new System.Drawing.Size(26, 617);
            this.bookScrollbar.TabIndex = 26;
            // 
            // sectionScrollbar
            // 
            this.sectionScrollbar.Dock = System.Windows.Forms.DockStyle.Right;
            this.sectionScrollbar.Location = new System.Drawing.Point(1202, 24);
            this.sectionScrollbar.Name = "sectionScrollbar";
            this.sectionScrollbar.Size = new System.Drawing.Size(26, 617);
            this.sectionScrollbar.TabIndex = 27;
            // 
            // chapterScrollbar
            // 
            this.chapterScrollbar.Dock = System.Windows.Forms.DockStyle.Right;
            this.chapterScrollbar.Location = new System.Drawing.Point(1176, 24);
            this.chapterScrollbar.Name = "chapterScrollbar";
            this.chapterScrollbar.Size = new System.Drawing.Size(26, 617);
            this.chapterScrollbar.TabIndex = 28;
            // 
            // treeView
            // 
            this.treeView.Dock = System.Windows.Forms.DockStyle.Left;
            this.treeView.Location = new System.Drawing.Point(0, 24);
            this.treeView.Name = "treeView";
            this.treeView.Size = new System.Drawing.Size(250, 617);
            this.treeView.TabIndex = 29;
            this.treeView.DoubleClick += new System.EventHandler(this.treeView_DoubleClick);
            // 
            // TerseForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlDark;
            this.ClientSize = new System.Drawing.Size(1384, 641);
            this.Controls.Add(this.flowLayoutPanel);
            this.Controls.Add(this.treeView);
            this.Controls.Add(this.chapterScrollbar);
            this.Controls.Add(this.sectionScrollbar);
            this.Controls.Add(this.bookScrollbar);
            this.Controls.Add(this.volumeScrollbar);
            this.Controls.Add(this.collectionScrollbar);
            this.Controls.Add(this.seriesScrollbar);
            this.Controls.Add(this.shelfScrollbar);
            this.Controls.Add(this.libraryScrollbar);
            this.Controls.Add(this.textBox);
            this.Controls.Add(this.menuStrip);
            this.MainMenuStrip = this.menuStrip;
            this.MinimumSize = new System.Drawing.Size(850, 400);
            this.Name = "TerseForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Terse Notepad";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.TerseForm_FormClosing);
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.flowLayoutPanel.ResumeLayout(false);
            this.flowLayoutPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

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
        private ToolStripMenuItem dimensionReportToolStripMenuItem;
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
        private Label pageLabel;
        private Label status;
        private TextBox chapterID;
        private TextBox sectionID;
        private TextBox scrollID;
        private Button jumpButton;
        private VScrollBar libraryScrollbar;
        private VScrollBar shelfScrollbar;
        private VScrollBar seriesScrollbar;
        private VScrollBar collectionScrollbar;
        private VScrollBar volumeScrollbar;
        private VScrollBar bookScrollbar;
        private VScrollBar sectionScrollbar;
        private VScrollBar chapterScrollbar;
        private TreeView treeView;
        private FlowLayoutPanel flowLayoutPanel;
        private ToolStripMenuItem treeViewToolStripMenuItem;
    }
}