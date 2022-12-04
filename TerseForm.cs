using System.Reflection;
using System.Runtime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static TerseNotepad.TerseText;

namespace TerseNotepad
{
    public partial class TerseForm : Form
    {
        private static readonly string TERSE_FILTER = "Terse File (*.t)|*.t|All files (*.*)|*.*";
        private TerseText _terse = new();

        private static readonly string SCROLL_BREAK = "\x17\n";
        private static readonly string SECTION_BREAK = "\x18\n";
        private static readonly string CHAPTER_BREAK = "\x19\n";

        // Editor State
        private uint _priorLine = 1;
        private uint _priorColumn = 1;
        private TerseConfig _settings = new();

        public TerseForm(string[] args)
        {
            InitializeComponent();
            treeViewToolStripMenuItem.Checked = _settings.TreeView;
            treeView.Visible = _settings.TreeView;
            if (args.Length > 0)
            {
                var filename = args[0];
                if (File.Exists(filename))
                {
                    _settings.Filename = filename;
                    _settings.Coords = args.Length > 1 ? args[1] : _settings.Coords;
                }
            }            

            if (_settings.Filename.Length == 0)
            {
                LoadDefaultTerse();
            }
            
            if (_settings.Filename.Length > 0 && File.Exists(_settings.Filename))
            {
                LoadFile(_settings.Filename);
            }
        }

        private void LoadDefaultTerse()
        {
            _settings.Filename = "";
            var currentAssembly = Assembly.GetExecutingAssembly();
            var items = currentAssembly.GetManifestResourceNames();
            var stream = currentAssembly.GetManifestResourceStream("TerseNotepad.Terse.t");
            if (stream != null)
            {
                var reader = new StreamReader(stream);
                var buffer = reader.ReadToEnd();
                if (buffer != null)
                {
                    LoadData(buffer);
                }
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var version = Assembly.GetExecutingAssembly()?.GetName()?.Version;
            if (version == null)
            {
                version = new Version(0, 0, 5);
            }
            MessageBox.Show($@"Terse Notepad

A reference editor for multi-dimensional text.

Use F2 - F11 to access additional dimensions.
", $"Terse Notepad {version.Major}.{version.Minor}.{version.Build}");
        }

        private void textBox_TextChanged(object sender, EventArgs e)
        {
            collectScroll();
        }

        private void collectScroll()
        {
            _terse.setScroll(textBox.Text);
            if (textBox.Text.Contains('\n'))
            {
                var node = getTreeNode(_terse.Coords.ToString());
                if (node == null)
                {
                    var parent = getParentTreeNode(_terse.Coords);
                    if (parent != null)
                    {
                        TreeNode sectionNode;
                        if (parent.Text.StartsWith("Chapter"))
                        {
                            sectionNode = parent.Nodes.Add($"Section {_terse.Coords.Section}");
                        }
                        else
                        {
                            sectionNode = parent;
                        }

                        var key = _terse.Coords.ToString();
                        var line = getScrollSummary(_terse.Coords, textBox.Text);
                        sectionNode.Nodes.Add(key, line);
                    }
                }
            }
        }

        private void loadScroll()
        {
            textBox.Text = _terse.getScroll();
            _terse.Coords.Line = 1;
            _priorLine = 1;
            _priorColumn = 1;
            // todo: enable the remaining dimensions
            // libraryID.Enabled = true;
            // libraryLabel.Enabled = true;
            // shelfID.Enabled = true;
            // shelfLabel.Enabled = true;
            // worldID.Enabled = true;
            // worldLabel.Enabled = true;
            // languageID.Enabled = true;
            // languageLabel.Enabled = true;
            // branchID.Enabled = true;
            // branchLabel.Enabled = true;
            // volumeID.Enabled = true;
            // volumeLabel.Enabled = true;
            // bookID.Enabled = true;
            // bookLabel.Enabled = true;
            chapterID.Enabled = true;
            chapterLabel.Enabled = true;
            sectionID.Enabled = true;
            sectionLabel.Enabled = true;
            scrollID.Enabled = true;
            scrollLabel.Enabled = true;
            UpdateStatusBar($"Loaded {_settings.Filename}");
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenNewFile();
        }

        private void OpenNewFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = TERSE_FILTER,
                FileName = _settings.Filename
            };
            var result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                LoadFile(dialog.FileName);
            }
        }

        private void RefreshSettings()
        {
            _settings.Reload();
            try
            {
                var font = new Font(_settings.Font, _settings.FontSize);
                textBox.Font = font;
            }
            catch (Exception)
            {
                _settings.LastError = "Invalid Font Settings";
            }
            treeView.Visible = _settings.TreeView;
            textBox.WordWrap = _settings.WordWrap;
            textBox.ZoomFactor = _settings.ZoomFactor;
            var menu = CreateRecentFilesMenu(_settings.RecentFile);
            recentToolStripMenuItem.DropDownItems.Clear();
            recentToolStripMenuItem.DropDownItems.AddRange(menu);
            libraryLabel.Text = _settings.Dimension11;
            shelfLabel.Text = _settings.Dimension10;
            seriesLabel.Text = _settings.Dimension9;
            collectionLabel.Text = _settings.Dimension8;
            volumeLabel.Text = _settings.Dimension7;
            bookLabel.Text = _settings.Dimension6;
            chapterLabel.Text = _settings.Dimension5;
            sectionLabel.Text = _settings.Dimension4;
            scrollLabel.Text = _settings.Dimension3;

            if (!File.Exists(_settings.IniFilePath))
            {
                _settings.Save();
            }
        }
        private ToolStripMenuItem[] CreateRecentFilesMenu(SortedSet<string> files)
        {
            var items = new ToolStripMenuItem[files.Count];
            for (int i = 0; i < files.Count; ++i)
            {
                items[i] = new ToolStripMenuItem
                {
                    Name = $"RecentMenuItem{i}",
                    Text = files.ElementAt(i)
                };
                items[i].Click += new EventHandler(MenuItemClickHandler);
            }

            return items;
        }

        public void MenuItemClickHandler(object? sender, EventArgs e)
        {
            if (sender != null)
            {
                ToolStripMenuItem clickedItem = (ToolStripMenuItem)sender;
                LoadFile(clickedItem.Text);
            }
        }

        private void LoadFile(string filename)
        {
            RefreshSettings();
            var data = File.ReadAllText(filename);
            _settings.Filename = filename;
            LoadData(data);
            UpdateStatusBar($"{filename}");
        }

        private void LoadData(string data)
        {
            var dimensionStream = data.Split(CHAPTER_BREAK);
            _terse.Chapter = new();
            treeView.Nodes.Clear();
            uint chapter_index = 1;
            foreach (var chapter in dimensionStream)
            {
                uint section_index = 1;
                var sections = chapter.Split(SECTION_BREAK);
                var chapterNode = new TreeNode($"Chapter {chapter_index}");
                chapterNode.Name = $"{chapter_index}-0-0";
                foreach (var section in sections)
                {
                    uint scroll_index = 1;
                    var scrolls = section.Split(SCROLL_BREAK);
                    var sectionNode = new TreeNode($"Section {section_index}");
                    sectionNode.Name = $"{chapter_index}-{section_index}-0";
                    foreach (var scroll in scrolls)
                    {
                        _terse.Coords.Scroll = scroll_index;
                        _terse.Coords.Section = section_index;
                        _terse.Coords.Chapter = chapter_index;
                        if (scroll.Length > 0)
                        {
                            var key = _terse.Coords.ToString();
                            var line = getScrollSummary(_terse.Coords, scroll);
                            var scrollNode = sectionNode.Nodes.Add(key, line);
                            _terse.setScroll(scroll, scrollNode);
                        }
                        ++scroll_index;
                    }
                    if (sectionNode.Nodes.Count > 0)
                    {
                        chapterNode.Nodes.Add(sectionNode);
                        _terse.setSectionNode(sectionNode, chapter_index, section_index);
                    }
                    ++section_index;
                }
                if (chapterNode.Nodes.Count > 0)
                {
                    treeView.Nodes.Add(chapterNode);
                    _terse.setChapterNode(chapterNode, chapter_index);
                }
                ++chapter_index;
            }
            treeView.ExpandAll();
            loadScroll();
            coordinateJump(_settings.Coords);
        }

        private string getScrollSummary(Coordinates coords, string scroll)
        {
            var firstLine = scroll.Split("\n")[0];
            var line = firstLine.Length > 40 ? firstLine[..40] : firstLine;
            if (line.Length > 0)
            {
                return line;
            }
            return coords.EditorSummary();
        }

        private void jumpToOrigin()
        {
            _terse.Coords.Reset();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _settings.Filename = "";
            jumpToOrigin();
            LoadData("");
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SyncEditorState();
            Close();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveCurrentFile(false, true);
        }

        private void SaveCurrentFile(bool chooseFile, bool reload)
        {
            if (chooseFile || _settings.Filename.Length == 0)
            {
                if (!ChooseSaveFilename())
                {
                    return;
                }
            }

            var collector = new StringBuilder();
            uint chapter_break_counter = 0;
            foreach (var chapter_index in _terse.Chapter.Keys)
            {
                while (++chapter_break_counter < chapter_index)
                {
                    collector.Append(CHAPTER_BREAK);
                }
                uint section_break_counter = 0;
                foreach (var section_index in _terse.Chapter[chapter_index].Children.Keys)
                {
                    while (++section_break_counter < section_index)
                    {
                        collector.Append(SECTION_BREAK);
                    }
                    uint scroll_break_counter = 0;
                    foreach (var scroll_index in _terse.Chapter[chapter_index].Children[section_index].Children.Keys)
                    {
                        while (++scroll_break_counter < scroll_index)
                        {
                            collector.Append(SCROLL_BREAK);
                        }
                        collector.Append(_terse.Chapter[chapter_index].Children[section_index].Children[scroll_index].Text);
                        collector.Append(SCROLL_BREAK);
                    }
                    collector.Append(SECTION_BREAK);
                }
                collector.Append(CHAPTER_BREAK);
            }

            File.WriteAllText(_settings.Filename, collector.ToString());
            _settings.Coords = _terse.Coords.ToString();
            _settings.Save();
            if (reload)
            {                
                LoadFile(_settings.Filename);
            }
            UpdateStatusBar($"Saved ${_settings.Filename}");
        }

        private void textBox_SelectionChanged(object sender, EventArgs e)
        {
            _terse.Coords.Line = 1;
            var total = 0;
            var offset = textBox.SelectionStart;
            var index = offset + textBox.SelectionLength - 1;
            if (index > -1 && index < textBox.Text.Length && textBox.Text[index] == '\n' && textBox.SelectionLength > 0)
            {
                --textBox.SelectionLength;
            }
            foreach (var line in textBox.Lines)
            {
                var delta = line.Length + 1;
                if ((total + delta) <= offset)
                {
                    total += delta;
                    ++_terse.Coords.Line;
                    _terse.Coords.Column = 1;
                }
                else
                {
                    _terse.Coords.Column = (uint)(offset - total) + 1;
                    break;
                }
            }
            UpdateStatusBar();
        }

        private void UpdateStatusBar(string action = "")
        {
            libraryID.Text = _terse.Coords.Library.ToString();
            shelfID.Text = _terse.Coords.Shelf.ToString();
            seriesID.Text = _terse.Coords.Series.ToString();
            collectionID.Text = _terse.Coords.Collection.ToString();
            bookID.Text = _terse.Coords.Volume.ToString();
            bookID.Text = _terse.Coords.Book.ToString();
            chapterID.Text = _terse.Coords.Chapter.ToString();
            sectionID.Text = _terse.Coords.Section.ToString();
            scrollID.Text = _terse.Coords.Scroll.ToString();
            status.Text = _terse.EditorSummary(action);
        }

        private bool ChooseSaveFilename()
        {
            var saver = new SaveFileDialog
            {
                Filter = TERSE_FILTER,
                FileName = _settings.Filename
            };
            var result = saver.ShowDialog();
            if (result == DialogResult.OK)
            {
                _settings.Filename = saver.FileName;
                return true;
            }

            return false;
        }

        private void textBox_KeyUp(object sender, KeyEventArgs e)
        {
            int scroll_delta = 0;
            int section_delta = 0;
            int chapter_delta = 0;
            bool arrowShifted = false;

            HandleHotkeys(sender, e);
            if (e.Handled)
            {
                return;
            }

            // Set Shifts
            if (!e.Shift && e.KeyCode == Keys.F4)
            {
                chapter_delta = 1;
                e.Handled = true;
            }
            if (e.Shift && e.KeyCode == Keys.F4)
            {
                chapter_delta = -1;
                e.Handled = true;
            }
            if (e.Control && e.KeyCode == Keys.PageDown)
            {
                chapter_delta = 1;
                e.Handled = true;
            }
            if (e.Control && e.KeyCode == Keys.PageUp)
            {
                chapter_delta = -1;
                e.Handled = true;
            }

            // Group Shifts
            if (!e.Shift && e.KeyCode == Keys.F3)
            {
                section_delta = 1;
                e.Handled = true;
            }
            if (e.Shift && e.KeyCode == Keys.F3)
            {
                section_delta = -1;
                e.Handled = true;
            }
            if (e.Shift && e.KeyCode == Keys.PageDown)
            {
                section_delta = 1;
                e.Handled = true;
            }
            if (e.Shift && e.KeyCode == Keys.PageUp)
            {
                section_delta = -1;
                e.Handled = true;
            }

            // Scroll Shifts
            if (!e.Shift && e.KeyCode == Keys.F2)
            {
                scroll_delta = 1;
                e.Handled = true;
            }
            if (e.Shift && e.KeyCode == Keys.F2)
            {
                scroll_delta = -1;
                e.Handled = true;
            }
            if (!e.Shift && !e.Control && e.KeyCode == Keys.PageDown)
            {
                scroll_delta = 1;
                e.Handled = true;
            }
            if (!e.Shift && !e.Control && e.KeyCode == Keys.PageUp)
            {
                scroll_delta = -1;
                e.Handled = true;
            }
            var reachedEnd = textBox.SelectionStart == textBox.TextLength &&
                textBox.Lines.Length > 0 &&
                _priorColumn > textBox.Lines.Last().Length;
            if (!e.Shift && e.KeyCode == Keys.Right && reachedEnd)
            {
                scroll_delta = 1;
                arrowShifted = true;
                e.Handled = true;
            }
            var reachedStart = textBox.SelectionStart == 0 &&
                               _priorColumn == 1;
            if (!e.Shift && e.KeyCode == Keys.Left && reachedStart)
            {
                scroll_delta = -1;
                arrowShifted = true;
                e.Handled = true;
            }
            if (!e.Shift && e.KeyCode == Keys.Down && _priorLine == _terse.Coords.Line)
            {
                scroll_delta = 1;
                arrowShifted = true;
                e.Handled = true;
            }
            if (!e.Shift && e.KeyCode == Keys.Up && _priorLine == 1)
            {
                scroll_delta = -1;
                arrowShifted = true;
                e.Handled = true;
            }
            if (scroll_delta != 0 || section_delta != 0 || chapter_delta != 0)
            {
                collectScroll();
                _terse.processDelta(chapter_delta, section_delta, scroll_delta);
                loadScroll();
                if (arrowShifted)
                {
                    if (scroll_delta < 0)
                    {
                        textBox.SelectionStart = textBox.TextLength > 1 ? textBox.TextLength - 1 : 0;
                    }
                    if (scroll_delta > 0)
                    {
                        textBox.SelectionStart = 0;
                    }
                }
                UpdateStatusBar();
            }
        }

        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (_priorLine != _terse.Coords.Line)
            {
                _priorLine = _terse.Coords.Line;
            }
            if (_priorColumn != _terse.Coords.Column)
            {
                _priorColumn = _terse.Coords.Column;
            }
        }

        private void dimensionReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var result = new StringBuilder();
            foreach (var chapter_index in _terse.Chapter.Keys)
            {
                foreach (var section_index in _terse.Chapter[chapter_index].Children.Keys)
                {
                    foreach (var scroll_index in _terse.Chapter[chapter_index].Children[section_index].Children.Keys)
                    {
                        var content = _terse.Chapter[chapter_index].Children[section_index].Children[scroll_index];
                        var count = content.Text.Length;
                        var summary = content.Text.Split("\n")[0];
                        if (summary.Length > 40) { summary = summary[..40]; }
                        result.AppendLine($"Chapter: {chapter_index}, Section: {section_index}, Scroll: {scroll_index} => {summary} ({count})");
                    }
                }
            }

            textBox.AppendText(result.ToString());
        }

        private void jumpButton_Click(object sender, EventArgs e)
        {
            collectScroll();
            try
            {
                var update = new Coordinates();
                update.Chapter = uint.Parse(chapterID.Text);
                update.Section = uint.Parse(sectionID.Text);
                update.Scroll = uint.Parse(scrollID.Text);
                _terse.Coords = update;
                loadScroll();
            }
            catch
            {
                chapterID.Text = _terse.Coords.Chapter.ToString();
                sectionID.Text = _terse.Coords.Section.ToString();
                scrollID.Text = _terse.Coords.Scroll.ToString();
            }
        }

        private void treeViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeView.Visible = treeViewToolStripMenuItem.Checked;
            _settings.TreeView = treeView.Visible;
        }

        private TreeNode? getTreeNode(string coordinates)
        {
            var node = treeView.Nodes.Find(coordinates, true);
            if (node.Length >= 1)
            {
                return node[0];
            }
            return null;
        }

        private TreeNode? getParentTreeNode(Coordinates coords)
        {
            TreeNode? node;

            var sectionNodeID = $"{coords.Chapter}-{coords.Section}-0";
            node = getTreeNode(sectionNodeID);
            if (node != null)
            {
                return node;
            }

            var chapterNodeID = $"{coords.Chapter}-0-0";
            return getTreeNode(chapterNodeID);
            
        }

        private void deleteNode(string coordinates)
        {
            var priorCoords = _terse.Coords;
            collectScroll();
            if (!coordinates.Contains('-'))
            {
                return;
            }           
            _terse.Coords.Load(coordinates);
            textBox.Text = "";
            getTreeNode(coordinates)?.Remove();
            coordinateJump(priorCoords.ToString());
        }

        private void coordinateJump(string coordinates)
        {
            if (!coordinates.Contains('-'))
            {
                return;
            }
            collectScroll();
            _terse.Coords.Load(coordinates);
            chapterID.Text = _terse.Coords.Chapter.ToString();
            sectionID.Text = _terse.Coords.Section.ToString();
            scrollID.Text = _terse.Coords.Scroll.ToString();
            loadScroll();
        }

        private void SyncEditorState()
        {
            SaveCurrentFile(false, false);
            _settings.Coords = _terse.Coords.ToString();
            _settings.ZoomFactor = textBox.ZoomFactor;
            _settings.WordWrap = textBox.WordWrap;
            _settings.TreeView = treeView.Visible;
            _settings.Save();
        }

        private void TerseForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SyncEditorState();
        }

        private void preferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadFile(_settings.IniFilePath);
        }

        private void defaultTerseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadDefaultTerse();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveCurrentFile(true, false);
        }

        private void HandleHotkeys(object sender, KeyEventArgs e)
        {
            // Ctrl-Shift-S: Save As File
            if (e.Control && e.Shift && e.KeyCode == Keys.S)
            {
                ChooseSaveFilename();
                SaveCurrentFile(false, true);
                e.Handled = true;
                return;
            }
            // Ctrl-S: Save File
            if (e.Control && e.KeyCode == Keys.S)
            {
                SaveCurrentFile(false, true);
                e.Handled = true;
                return;
            }
            // Ctrl-O: Open File
            if (e.Control && e.KeyCode == Keys.O)
            {
                OpenNewFile();
                e.Handled = true;
                return;
            }
            // Ctrl-D: Dimension Report
            if (e.Control && e.KeyCode == Keys.D)
            {
                dimensionReportToolStripMenuItem_Click(sender, e);
                e.Handled = true;
                return;
            }
            // Ctrl-N: New File
            if (e.Control && e.KeyCode == Keys.N)
            {
                newToolStripMenuItem_Click(sender, e);
                e.Handled = true;
                return;
            }
            // Ctrl-,: Preferences
            if (e.Control && e.KeyCode == Keys.Oemcomma)
            {
                preferencesToolStripMenuItem_Click(sender, e);
                e.Handled = true;
                return;
            }
        }

        private void treeView_KeyUp(object sender, KeyEventArgs e)
        {
            HandleHotkeys(sender, e);

            if (e.KeyCode == Keys.Delete ||
                e.KeyCode == Keys.Back)
            {
                var key = treeView.SelectedNode.Name;
                if (key != null && key.Length > 0)
                {
                    deleteNode(key);
                }
            }
        }

        private void libraryID_KeyUp(object sender, KeyEventArgs e)
        {
            HandleHotkeys(sender, e);
        }

        private void shelfID_KeyUp(object sender, KeyEventArgs e)
        {
            HandleHotkeys(sender, e);
        }

        private void seriesID_KeyUp(object sender, KeyEventArgs e)
        {
            HandleHotkeys(sender, e);
        }

        private void collectionID_KeyUp(object sender, KeyEventArgs e)
        {
            HandleHotkeys(sender, e);
        }

        private void volumeID_KeyUp(object sender, KeyEventArgs e)
        {
            HandleHotkeys(sender, e);
        }

        private void bookID_KeyUp(object sender, KeyEventArgs e)
        {
            HandleHotkeys(sender, e);
        }

        private void chapterID_KeyUp(object sender, KeyEventArgs e)
        {
            HandleHotkeys(sender, e);
            UpDownHandler(chapterID, e);
            jumpButton_Click(sender, e);
        }

        private void sectionID_KeyUp(object sender, KeyEventArgs e)
        {
            HandleHotkeys(sender, e);
            UpDownHandler(sectionID, e);
            jumpButton_Click(sender, e);
        }

        private void BumpCoordinate(System.Windows.Forms.TextBox box, int amount)
        {
            var value = int.Parse(box.Text) + amount;
            if (value < 1) { value = 1; }
            if (value > 9999) { value = 9999; }
            box.Text = value.ToString();
        }

        private void UpDownHandler(System.Windows.Forms.TextBox box, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
            {
                BumpCoordinate(box, -1);
            }
            if (e.KeyCode == Keys.Down)
            {
                BumpCoordinate(box, 1);
            }
        }

        private void scrollID_KeyUp(object sender, KeyEventArgs e)
        {
            HandleHotkeys(sender, e);
            UpDownHandler(scrollID, e);
            jumpButton_Click(sender, e);
        }

        private void treeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var coordinates = treeView.SelectedNode.Name;
            if (coordinates.Length == 0)
            {
                return;
            }
            collectScroll();
            coordinateJump(coordinates);
        }

        private void wordWrapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBox.WordWrap = wordWrapToolStripMenuItem.Checked;
            _settings.WordWrap = textBox.WordWrap;
        }
    }
}