using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using static TerseNotepad.TerseText;

namespace TerseNotepad
{
    public partial class TerseForm : Form
    {
        private static readonly string TERSE_FILTER = "Terse File (*.t)|*.t|All files (*.*)|*.*";
        private TerseModel _model = new();

        // Vim Integration
        [DllImport("USER32.DLL")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int X, int Y, int cx, int cy, uint flags);
  
        // Editor State
        private uint _priorLine = 1;
        private uint _priorColumn = 1;
        private Coordinates _checkout = new();
        private TerseConfig _settings = new();
        private Font SCROLL_NODE_FONT = new("Cascadia Code", 11);

        private Vim.Vim? vimEditor { get; set; } = null;

        public TerseForm(string[] args)
        {
            InitializeComponent();
            InitializeExternalEditor();

            lockToScrollMenuItem.Checked = Control.IsKeyLocked(Keys.Scroll);
            LoadFonts();
            scrollLockUIUpdate();
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
                LoadFile(_settings.Filename, true);
            }

            vimModeToolStripMenuItem.Checked = _settings.VimMode;
            if (_settings.VimMode)
            {                
                vimEditor?.SetForeground();
                SendToBack();
            }
        }

        private void InitializeExternalEditor()
        {
            if (!_settings.VimMode)
            {
                return;
            }
            // VIM Integration via OLE
            vimEditor = new();
            var vimWindow = (IntPtr)vimEditor.GetHwnd();
            var screenCoordinates = PointToScreen(Location);
            var vimX = screenCoordinates.X + textBox.Left;
            var vimY = screenCoordinates.Y + textBox.Top;
            var vimWidth = textBox.Size.Width;
            var vimHeight = textBox.Size.Height;
            SetWindowPos(vimWindow, Handle, vimX, vimY, vimWidth, vimHeight, 0);
        }

        private void LoadDefaultTerse()
        {
            _settings.Filename = "";
            var currentAssembly = Assembly.GetExecutingAssembly();
            var stream = currentAssembly.GetManifestResourceStream("TerseNotepad.Terse.t");
            if (stream != null)
            {
                var reader = new StreamReader(stream);
                var buffer = reader.ReadToEnd();
                if (buffer != null)
                {
                    LoadData(buffer, true);
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
            if (!_settings.VimMode)
            {
                collectScroll();
            }
        }

        private void collectScroll()
        {
            if (!_checkout.IsValid())
            {
                return;
            }
            _model.Coords = _checkout;
            reloadMenuItem.Enabled = true;
            if (vimEditor != null)
            {
                var scratchpad = _settings.Filename + $".scroll-{_model.Coords}";
                if (File.Exists(scratchpad))
                {
                    VimSetEditMode(false);
                    vimEditor.SendKeys(":w!\n");
                    try
                    {
                        textBox.Text = File.ReadAllText(scratchpad);
                    }
                    catch { }
                }
            }
            _model.Terse.setScroll(textBox.Text);
            if (textBox.Text.Length == 0)
            {
                return;
            }
            treeView.BeginUpdate();
            treeView.SuspendLayout();
            var node = getTreeNode(_model.Terse.Coords.ToString());
            if (node != null)
            {
                if (textBox.Lines != null && textBox.Lines.Length >= 1)
                {
                    node.Text = node.Name + "? " + TerseModel.GetScrollSummary(_model.Terse.Coords, textBox.Lines.First());
                }
            }
            else
            {
                var parent = getParentTreeNode(_model.Terse.Coords);
                if (parent != null)
                {
                    TreeNode sectionNode;
                    if (parent.Text.StartsWith("Chapter"))
                    {
                        sectionNode = parent.Nodes.Add($"Section {_model.Terse.Coords.Section}");
                    }
                    else
                    {
                        sectionNode = parent;
                    }

                    var key = _model.Terse.Coords.ToString();
                    var line = TerseModel.GetScrollSummary(_model.Terse.Coords, textBox.Text);
                    var scrollNode = sectionNode.Nodes.Add(key, key + ": " + line);
                    scrollNode.NodeFont = SCROLL_NODE_FONT;
                    treeView.SelectedNode = scrollNode;
                    textBox.SelectionStart = textBox.Text.Length;
                }
            }

            treeView.ExpandAll();
            treeView.ResumeLayout();
            treeView.EndUpdate();
        }

        private void updateScrollbarValue(ScrollBar bar, uint value)
        {
            int translated = (int)value;
            if (translated >= bar.Minimum && translated <= bar.Maximum)
            {
                bar.Value = translated;
            }
        }

        private void VimSetEditMode(bool insert = true, bool retry = false)
        {
            try
            {
                if (vimEditor == null)
                {
                    InitializeExternalEditor();
                }
                if (vimEditor == null)
                {
                    return;
                }
                var test = vimEditor.Eval("mode()");
                if (test == "n" && insert)
                {
                    vimEditor.SendKeys("i");
                }
                if (test == "i" && !insert)
                {
                    vimEditor.SendKeys("<C-\\><C-N>");
                }
            }
            catch
            {
                vimEditor = new();
                if (retry)
                {
                    VimSetEditMode(insert, false);
                }
            }
        }

        private void loadScroll()
        {
            textBox.SuspendLayout();
            _checkout = new Coordinates(_model.Coords);
            textBox.Text = _model.Terse.getScroll();
            if (_settings.VimMode && vimEditor != null)
            {
                VimSetEditMode(false);
                var scratchpad = _settings.Filename + $".scroll-{_model.Coords}";
                File.WriteAllText(scratchpad, textBox.Text);
                vimEditor.SendKeys($":e! {scratchpad}\n");
            }
            _model.Terse.Coords.Line = 1;
            _priorLine = 1;
            _priorColumn = 1;
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
            UpdateUI($"Loaded {_settings.Filename}");

            if (treeView.Visible)
            {
                var id = $"{_model.Coords}";
                var node = treeView.Nodes.Find(id, true);
                if (node != null && node.Length >= 1)
                {
                    treeView.SelectedNode = node[0];
                }
            }

            textBox.ResumeLayout();
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
                LoadFile(dialog.FileName, true);
            }
        }

        private void LoadFonts()
        {
            try
            {
                SCROLL_NODE_FONT = new Font(_settings.Font, _settings.FontSize);
                textBox.Font = SCROLL_NODE_FONT;
                treeView.Font = SCROLL_NODE_FONT;
            }
            catch (Exception)
            {
                _settings.LastError = "Invalid Font Settings";
            }
        }

        private void RefreshSettings()
        {
            _settings.Reload();
            LoadFonts();
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
            SetEditorTheme();

            if (!File.Exists(_settings.IniFilePath))
            {
                _settings.Save();
            }
        }
        private ToolStripMenuItem[] CreateRecentFilesMenu(SortedDictionary<int, string> files)
        {
            var items = new List<ToolStripMenuItem>();
            var i = 0;
            var used = new HashSet<string>();
            foreach (var key in files.Keys.OrderByDescending(q => q))
            {
                var filename = files[key];
                if (used.Contains(filename)) { continue; }
                used.Add(filename);
                if (!File.Exists(filename))
                {
                    continue;
                }
                var next = new ToolStripMenuItem
                {
                    Name = $"RecentMenuItem{++i}",
                    Text = filename
                };
                next.Click += new EventHandler(MenuItemClickHandler);
                items.Add(next);
            }

            return items.ToArray();
        }

        public void MenuItemClickHandler(object? sender, EventArgs e)
        {
            if (sender != null)
            {
                ToolStripMenuItem clickedItem = (ToolStripMenuItem)sender;
                LoadFile(clickedItem.Text, true);
            }
        }

        private void LoadFile(string filename, bool resetView)
        {
            RefreshSettings();
            if (File.Exists(filename))
            {
                var data = File.ReadAllText(filename);
                _settings.Filename = filename;
                LoadData(data, resetView);
                UpdateUI($"{filename}");
            }
            else
            {
                UpdateUI($"!{filename}");
            }
        }

        public static string FormatNumber(int num)
        {
            if (num >= 100000)
                return FormatNumber(num / 1000) + "K";

            if (num >= 10000)
                return (num / 1000D).ToString("0.#") + "K";

            return num.ToString("#,0");
        }

        private void LoadData(string data, bool resetView)
        {
            treeView.SuspendLayout();
            treeView.BeginUpdate();
            treeView.Nodes.Clear();
            _model.Load(data, treeView, sectionScrollbar, chapterScrollbar);            
            treeView.ExpandAll();
            treeView.EndUpdate();
            treeView.ResumeLayout();
            if (resetView)
            {
                jumpToOrigin();
            }
            else
            {
                coordinateJump(_settings.Coords, false);
            }
        }

        private void jumpToOrigin()
        {
            _model.Coords.Reset();
            coordinateJump(_settings.Coords, false);
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _settings.Filename = "";
            _model.Terse.Coords.Reset();
            _settings.Coords = _model.Terse.Coords.ToString();
            LoadData("", true);
        }

        private void CloseExternalEditor()
        {
            if (!_settings.VimMode)
            {
                return;
            }

            try
            {
                vimEditor?.SendKeys(":q!\n");
            }
            catch { }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseExternalEditor();
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

            var serialized = _model.Serialize();

            File.WriteAllText(_settings.Filename, serialized);
            _settings.Coords = _model.Terse.Coords.ToString();
            if (!_settings.Filename.EndsWith("TerseNotepad\\terse.ini"))
            {
                _settings.Save();
            }
            if (reload)
            {
                LoadFile(_settings.Filename, false);
            }
            UpdateUI($"Saved ${_settings.Filename}");
        }

        private void textBox_SelectionChanged(object sender, EventArgs e)
        {
            _model.Terse.Coords.Line = 1;
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
                    ++_model.Terse.Coords.Line;
                    _model.Terse.Coords.Column = 1;
                }
                else
                {
                    _model.Terse.Coords.Column = (uint)(offset - total) + 1;
                    break;
                }
            }
            UpdateUI();
        }

        private void UpdateUI(string action = "")
        {
            libraryID.Text = _model.Terse.Coords.Library.ToString();
            shelfID.Text = _model.Terse.Coords.Shelf.ToString();
            seriesID.Text = _model.Terse.Coords.Series.ToString();
            collectionID.Text = _model.Terse.Coords.Collection.ToString();
            volumeID.Text = _model.Terse.Coords.Volume.ToString();
            bookID.Text = _model.Terse.Coords.Book.ToString();
            chapterID.Text = _model.Terse.Coords.Chapter.ToString();
            sectionID.Text = _model.Terse.Coords.Section.ToString();
            scrollID.Text = _model.Terse.Coords.Scroll.ToString();

            updateScrollbarValue(scrollScrollbar, _model.Terse.Coords.Scroll);
            updateScrollbarValue(sectionScrollbar, _model.Terse.Coords.Section);
            updateScrollbarValue(chapterScrollbar, _model.Terse.Coords.Chapter);

            status.Text = _model.Terse.EditorSummary(action);
            wordCountLabel.Text = $"Doc: {FormatNumber(_model.WordCount)}, Scroll: {FormatNumber(_model.ScrollWordCount)}";
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

            if (treeView.Visible == false)
            {
                // Scroll Lock
                // Do not allow dimension shifts when the treeView is invisible
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
            if (!e.Shift && e.KeyCode == Keys.Down && _priorLine == _model.Terse.Coords.Line)
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
                _model.Terse.processDelta(chapter_delta, section_delta, scroll_delta);
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
            }
        }

        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (_priorLine != _model.Terse.Coords.Line)
            {
                _priorLine = _model.Terse.Coords.Line;
            }
            if (_priorColumn != _model.Terse.Coords.Column)
            {
                _priorColumn = _model.Terse.Coords.Column;
            }

            // Ctrl-V: Paste plain-text only
            if (e.Control && e.KeyCode == Keys.V)
            {
                textBox.SelectedText = Clipboard.GetText();
                e.Handled = true;
                return;
            }
        }

        private void dimensionReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var result = new StringBuilder();
            foreach (var chapter_index in _model.Terse.Chapter.Keys)
            {
                foreach (var section_index in _model.Terse.Chapter[chapter_index].Children.Keys)
                {
                    foreach (var scroll_index in _model.Terse.Chapter[chapter_index].Children[section_index].Children.Keys)
                    {
                        var content = _model.Terse.Chapter[chapter_index].Children[section_index].Children[scroll_index];
                        var count = content.Text.Length;
                        var summary = TerseModel.GetScrollSummary(_model.Coords, content.Text);
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
                _model.Terse.Coords = update;
                loadScroll();
            }
            catch
            {
                chapterID.Text = _model.Terse.Coords.Chapter.ToString();
                sectionID.Text = _model.Terse.Coords.Section.ToString();
                scrollID.Text = _model.Terse.Coords.Scroll.ToString();
            }
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

        // pre: the UI always shows the selected node...
        private void deleteNode(string coordinates, bool requestConfirmation = true)
        {
            if (!coordinates.Contains('-'))
            {
                return;
            }
            _checkout = new Coordinates(coordinates);
            _model.Terse.Coords.Load(coordinates);
            UpdateUI("Delete");
            var node = getTreeNode(coordinates);
            if (node == null) { return; }
            if (requestConfirmation)
            {
                var count = 1 + node.GetNodeCount(true);
                if (count > 1)
                {
                    var confirm = MessageBox.Show($"Are you sure you want to delete {count} nodes?", "Node Delete Confirmation", MessageBoxButtons.YesNo);
                    if (confirm == DialogResult.No)
                    {
                        return;
                    }
                }
            }

            var deleteList = new List<string>();
            foreach (TreeNode child in node.Nodes)
            {
                deleteList.Add(child.Name);
            }
            foreach (var childCoordinates in deleteList)
            {
                deleteNode(childCoordinates, false);
            }
            node.Remove();
            textBox.Text = "";
            collectScroll();
        }

        private void coordinateJump(string coordinates, bool storeFirst)
        {
            if (!coordinates.Contains('-'))
            {
                return;
            }
            if (storeFirst)
            {
                collectScroll();
            }
            _model.Terse.Coords.Load(coordinates);
            chapterID.Text = _model.Terse.Coords.Chapter.ToString();
            sectionID.Text = _model.Terse.Coords.Section.ToString();
            scrollID.Text = _model.Terse.Coords.Scroll.ToString();
            loadScroll();
        }

        private void SyncEditorState()
        {
            SaveCurrentFile(false, false);
            _settings.Coords = _model.Terse.Coords.ToString();
            _settings.ZoomFactor = textBox.ZoomFactor;
            _settings.WordWrap = textBox.WordWrap;
            _settings.TreeView = treeView.Visible;
            _settings.Theme = treeView.BackColor == Color.Black ? "Dark" : "Light";
            _settings.Save();
        }

        private void TerseForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            CloseExternalEditor();
            SyncEditorState();
        }

        private void preferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadFile(_settings.IniFilePath, true);
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
            // Scroll Lock
            if (e.KeyCode == Keys.Scroll)
            {
                lockToScrollMenuItem.Checked = Control.IsKeyLocked(Keys.Scroll);
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
            coordinateJump(coordinates, true);
        }

        private void wordWrapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBox.WordWrap = wordWrapToolStripMenuItem.Checked;
            _settings.WordWrap = textBox.WordWrap;
        }

        private void scrollScrollbar_ValueChanged(object sender, EventArgs e)
        {
            scrollID.Text = scrollScrollbar.Value.ToString();
            jumpButton_Click(sender, e);
        }

        private void sectionScrollbar_ValueChanged(object sender, EventArgs e)
        {
            sectionID.Text = sectionScrollbar.Value.ToString();
            jumpButton_Click(sender, e);
        }

        private void chapterScrollbar_ValueChanged(object sender, EventArgs e)
        {
            chapterID.Text = chapterScrollbar.Value.ToString();
            jumpButton_Click(sender, e);
        }        

        private void UpdateScrollbarMaximum(System.Windows.Forms.TextBox box, System.Windows.Forms.ScrollBar bar)
        {
            try
            {
                var test = int.Parse(box.Text);
                if (test > bar.Maximum)
                {
                    bar.Maximum = test;
                }
            }
            catch
            {
            }
        }

        private void scrollID_TextChanged(object sender, EventArgs e)
        {
            UpdateScrollbarMaximum(scrollID, scrollScrollbar);
        }

        private void sectionID_TextChanged(object sender, EventArgs e)
        {
            UpdateScrollbarMaximum(sectionID, sectionScrollbar);
        }

        private void chapterID_TextChanged(object sender, EventArgs e)
        {
            UpdateScrollbarMaximum(chapterID, chapterScrollbar);
        }

        private void SetEditorTheme()
        {
            bool mode = _settings.DarkMode;
            treeView.BackColor = mode ? Color.Black : Color.White;
            treeView.ForeColor = mode ? Color.White : Color.Black;
            textBox.BackColor = mode ? Color.Black : Color.White;
            textBox.ForeColor = mode ? Color.White : Color.Black;
            BackColor = mode ? Color.DeepSkyBlue : Color.DarkGray;
        }

        private void darkModeMenuItem_Click(object sender, EventArgs e)
        {
            _settings.Theme = darkModeMenuItem.Checked ? "Dark" : "Light";
            _settings.Save();
            SetEditorTheme();
        }

        private void vimModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!_settings.VimMode && vimModeToolStripMenuItem.Checked)
            {
                if (_settings.Filename.Length == 0)
                {
                    if (!ChooseSaveFilename())
                    {
                        MessageBox.Show("You must save the Terse doc before enabling VIM");
                        vimModeToolStripMenuItem.Checked = false;
                        return;
                    }
                }
            }
            _settings.VimMode = vimModeToolStripMenuItem.Checked;
            if (_settings.VimMode)
            {
                InitializeExternalEditor();
            }
            else
            {
                if (vimEditor != null)
                {
                    VimSetEditMode(false);
                    vimEditor.SendKeys(":q!\n");
                    vimEditor = null;
                }
            }
        }

        private void lockToScrollMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            scrollLockUIUpdate();
        }

        private void scrollLockUIUpdate()
        {
            treeView.Visible = !lockToScrollMenuItem.Checked;
            _settings.TreeView = treeView.Visible;

            if (lockToScrollMenuItem.Checked)
            {
                textBox.Left = 0;
                textBox.Width = Width - 100;
            }
            else
            {
                textBox.Left = treeView.Right;
                textBox.Width = Width - treeView.Width - 100;
            }
        }

        private void reloadMenuItem_Click(object sender, EventArgs e)
        {
            LoadFile(_settings.Filename, false);
            reloadMenuItem.Enabled = false;
        }

        private void treeView_DoubleClick(object sender, EventArgs e)
        {
            textBox.Focus();
            textBox.SelectionStart = textBox.Text.Length;
        }
    }
}