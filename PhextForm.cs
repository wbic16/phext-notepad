using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PhextNotepad
{
    public partial class PhextForm : Form
    {
        private static readonly string PHEXT_FILTER = "Phext (*.phext)|*.phext|All files (*.*)|*.*";
        private PhextModel _model = new();

        // Vim Integration
        [DllImport("USER32.DLL")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int X, int Y, int cx, int cy, uint flags);

        // Editor State
        private int _priorLine = 1;
        private int _priorColumn = 1;
        private Coordinates? _checkout = null;
        private PhextConfig _settings = new();
        private Font SCROLL_NODE_FONT = new("Cascadia Code", 11);

        public PhextForm(string[] args)
        {
            InitializeComponent();

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
                LoadDefaultPhext();
            }

            if (_settings.Filename.Length > 0 && File.Exists(_settings.Filename))
            {
                LoadFile(_settings.Filename, false);
            }
        }

        private void LoadDefaultPhext()
        {
            _settings.Filename = "";
            var currentAssembly = Assembly.GetExecutingAssembly();
            var stream = currentAssembly.GetManifestResourceStream("PhextNotepad.phext");
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
            MessageBox.Show($@"Phext Notepad

A reference editor for multi-dimensional text.

Use F2 - F11 to access additional dimensions.
", $"Phext Notepad {version.Major}.{version.Minor}.{version.Build}");
        }

        private void textBox_TextChanged(object sender, EventArgs e)
        {
            collectScroll();
        }

        private void collectScroll()
        {
            if (_checkout == null || _checkout.ToString() != _model.Coords.ToString())
            {
                return;
            }
            reloadMenuItem.Enabled = true;
            _model.Phext.setScroll(textBox.Text);
            if (textBox.Text.Length == 0)
            {
                return;
            }
            treeView.BeginUpdate();
            treeView.SuspendLayout();
            var node = getTreeNode(_model.Phext.Coords);
            if (node != null)
            {
                if (textBox.Lines != null && textBox.Lines.Length >= 1)
                {
                    var line = PhextModel.GetScrollSummary(_model.Phext.Coords, textBox.Text);
                    node.Text = _settings.ShowCoordinates ? $"{node.Name}? {line}" : line;
                }
            }
            else
            {
                var parent = getParentTreeNode(_model.Phext.Coords);
                if (parent != null)
                {
                    TreeNode sectionNode;
                    if (parent.Text.StartsWith("Chapter"))
                    {
                        sectionNode = parent.Nodes.Add($"Section {_model.Phext.Coords.Section}");
                    }
                    else
                    {
                        sectionNode = parent;
                    }

                    var line = PhextModel.GetScrollSummary(_model.Phext.Coords, textBox.Text);
                    var scrollNode = _model.CreateNode(sectionNode, line, _settings.ShowCoordinates);
                    scrollNode.NodeFont = SCROLL_NODE_FONT;
                    treeView.SelectedNode = scrollNode;
                    textBox.SelectionStart = textBox.Text.Length;
                }
            }

            treeView.ExpandAll();
            treeView.ResumeLayout();
            treeView.EndUpdate();
        }

        private void loadScroll()
        {
            textBox.SuspendLayout();
            _checkout = new Coordinates(_model.Coords);
            textBox.Text = _model.Phext.getScroll();
            _model.Phext.Coords.Line = 1;
            _priorLine = 1;
            _priorColumn = 1;
            phextCoordinate.Enabled = true;
            coordinateLabel.Enabled = true;
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
                Filter = PHEXT_FILTER,
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
            showCoordinatesToolStripMenuItem.Checked = _settings.ShowCoordinates;
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
                _checkout = null;
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
            _model.Load(data, _settings.ShowCoordinates, treeView);
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
            _model.Phext.Coords.Reset();
            _settings.Coords = _model.Phext.Coords.ToString();
            LoadData("", true);
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

            collectScroll();
            var serialized = _model.Serialize();
            File.WriteAllText(_settings.Filename, serialized);

            _settings.Coords = _model.Phext.Coords.ToString();
            if (!_settings.Filename.EndsWith("PhextNotepad\\PhextNotepad.ini"))
            {
                _settings.Save();
            }
            if (reload)
            {
                LoadFile(_settings.Filename, false);
            }
            UpdateUI($"Saved {_settings.Filename}");
        }

        private void textBox_SelectionChanged(object sender, EventArgs e)
        {
            _model.Phext.Coords.Line = 1;
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
                    ++_model.Phext.Coords.Line;
                    _model.Phext.Coords.Column = 1;
                }
                else
                {
                    _model.Phext.Coords.Column = offset - total + 1;
                    break;
                }
            }
            UpdateUI();
        }

        private void UpdateUI(string action = "")
        {
            phextCoordinate.Text = _model.Phext.Coords.ToString();

            textBox.Enabled = _checkout != null;

            status.Text = _model.Phext.EditorSummary(action);
            wordCountLabel.Text = $"Bytes: {_model.ByteCount}, Words (Total): {FormatNumber(_model.WordCount)}, Words (Page): {FormatNumber(_model.ScrollWordCount)}";
        }

        private bool ChooseSaveFilename()
        {
            var saver = new SaveFileDialog
            {
                Filter = PHEXT_FILTER,
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
            Coordinates delta = new();

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

            // Library Shifts
            if (!e.Shift && e.KeyCode == Keys.F10)
            {
                delta.Library = 1;
                e.Handled = true;
            }
            if (e.Shift && e.KeyCode == Keys.F10)
            {
                delta.Library = -1;
                e.Handled = true;
            }

            // Shelf Shifts
            if (!e.Shift && e.KeyCode == Keys.F9)
            {
                delta.Shelf = 1;
                e.Handled = true;
            }
            if (e.Shift && e.KeyCode == Keys.F9)
            {
                delta.Shelf = -1;
                e.Handled = true;
            }

            // Series Shifts
            if (!e.Shift && e.KeyCode == Keys.F8)
            {
                delta.Series = 1;
                e.Handled = true;
            }
            if (e.Shift && e.KeyCode == Keys.F8)
            {
                delta.Series = -1;
                e.Handled = true;
            }

            // Collection Shifts
            if (!e.Shift && e.KeyCode == Keys.F7)
            {
                delta.Collection = 1;
                e.Handled = true;
            }
            if (e.Shift && e.KeyCode == Keys.F7)
            {
                delta.Collection = -1;
                e.Handled = true;
            }

            // Volume Shifts
            if (!e.Shift && e.KeyCode == Keys.F6)
            {
                delta.Volume = 1;
                e.Handled = true;
            }
            if (e.Shift && e.KeyCode == Keys.F6)
            {
                delta.Volume = -1;
                e.Handled = true;
            }

            // Book Shifts
            if (!e.Shift && e.KeyCode == Keys.F5)
            {
                delta.Book = 1;
                e.Handled = true;
            }
            if (e.Shift && e.KeyCode == Keys.F5)
            {
                delta.Book = -1;
                e.Handled = true;
            }

            // Chapter Shifts
            if (!e.Shift && e.KeyCode == Keys.F4)
            {
                delta.Chapter = 1;
                e.Handled = true;
            }
            if (e.Shift && e.KeyCode == Keys.F4)
            {
                delta.Chapter = -1;
                e.Handled = true;
            }
            if (e.Control && e.KeyCode == Keys.PageDown)
            {
                delta.Chapter = 1;
                e.Handled = true;
            }
            if (e.Control && e.KeyCode == Keys.PageUp)
            {
                delta.Chapter = -1;
                e.Handled = true;
            }

            // Section Shifts
            if (!e.Shift && e.KeyCode == Keys.F3)
            {
                delta.Section = 1;
                e.Handled = true;
            }
            if (e.Shift && e.KeyCode == Keys.F3)
            {
                delta.Section = -1;
                e.Handled = true;
            }
            if (e.Shift && e.KeyCode == Keys.PageDown)
            {
                delta.Section = 1;
                e.Handled = true;
            }
            if (e.Shift && e.KeyCode == Keys.PageUp)
            {
                delta.Section = -1;
                e.Handled = true;
            }

            // Scroll Shifts
            if (!e.Shift && e.KeyCode == Keys.F2)
            {
                delta.Scroll = 1;
                e.Handled = true;
            }
            if (e.Shift && e.KeyCode == Keys.F2)
            {
                delta.Scroll = -1;
                e.Handled = true;
            }
            if (e.Alt && e.KeyCode == Keys.PageDown)
            {
                delta.Scroll = 1;
                e.Handled = true;
            }
            if (e.Alt && e.KeyCode == Keys.PageUp)
            {
                delta.Scroll = -1;
                e.Handled = true;
            }

            if (delta.HasDelta())
            {
                collectScroll();
                _model.Phext.processDelta(delta);
                loadScroll();
            }
        }

        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (_priorLine != _model.Phext.Coords.Line)
            {
                _priorLine = _model.Phext.Coords.Line;
            }
            if (_priorColumn != _model.Phext.Coords.Column)
            {
                _priorColumn = _model.Phext.Coords.Column;
            }

            // Ctrl-V: Paste plain-text only
            if (e.Control && e.KeyCode == Keys.V)
            {
                textBox.SelectedText = Clipboard.GetText();
                e.Handled = true;
                return;
            }
        }

        private Coordinates GetPhextCoordinate()
        {
            var parts = phextCoordinate.Text.Replace('.', '/').Split('/');
            if (parts.Length != 9)
            {
                return _model.Phext.Coords;
            }

            var result = new Coordinates(true);
            if (short.TryParse(parts[0], out short p0))
                result.Library = p0;
            else
                result.Intermediate = true;
            if (short.TryParse(parts[1], out short p1))
                result.Shelf = p1;
            else
                result.Intermediate = true;
            if (short.TryParse(parts[2], out short p2))
                result.Series = p2;
            else
                result.Intermediate = true;
            if (short.TryParse(parts[3], out short p3))
                result.Collection = p3;
            else
                result.Intermediate = true;
            if (short.TryParse(parts[4], out short p4))
                result.Volume = p4;
            else
                result.Intermediate = true;
            if (short.TryParse(parts[5], out short p5))
                result.Book = p5;
            else
                result.Intermediate = true;
            if (short.TryParse(parts[6], out short p6))
                result.Chapter = p6;
            else
                result.Intermediate = true;
            if (short.TryParse(parts[7], out short p7))
                result.Section = p7;
            else
                result.Intermediate = true;
            if (short.TryParse(parts[8], out short p8))
                result.Scroll = p8;
            else
                result.Intermediate = true;

            return result;
        }

        private void jumpButton_Click(object sender, EventArgs e)
        {
            collectScroll();
            
            var next = GetPhextCoordinate();
            if (!next.Intermediate)
            {
                _model.Phext.Coords = next;
                loadScroll();
            }
        }

        private TreeNode? getTreeNode(Coordinates coordinates)
        {
            return _model.Find(coordinates);
        }

        private TreeNode? getParentTreeNode(Coordinates coords)
        {
            if (coords.Chapter == 0 || coords.Section == 0)
            {
                return null;
            }

            // todo: rewrite from scratch using new idioms
            return null;
        }

        // pre: the UI always shows the selected node...
        private void deleteNode(string coordinates, bool requestConfirmation = true)
        {
            if (!coordinates.Contains('-'))
            {
                return;
            }
            var test = new Coordinates(coordinates);
            if (!test.IsValid())
            {
                return;
            }
            _checkout = test;
            _model.Phext.Coords = test;
            UpdateUI("Delete");
            var node = getTreeNode(_model.Phext.Coords);
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

            textBox.Text = "";
            collectScroll();

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
        }

        private void coordinateJump(string coordinates, bool storeFirst)
        {
            if (_checkout?.ToString() == coordinates)
            {
                return;
            }
            if (storeFirst)
            {
                _checkout = new Coordinates(_model.Coords);
                collectScroll();
            }
            var test = new Coordinates(coordinates);
            if (!test.IsValid())
            {
                return;
            }
            _model.Coords = test.Clamp();
            phextCoordinate.Text = _model.Coords.ToString();
            loadScroll();
            _checkout = new Coordinates(_model.Coords);
        }

        private void SyncEditorState()
        {
            SaveCurrentFile(false, false);
            _settings.Coords = _model.Phext.Coords.ToString();
            _settings.ZoomFactor = textBox.ZoomFactor;
            _settings.WordWrap = textBox.WordWrap;
            _settings.TreeView = treeView.Visible;
            _settings.Theme = darkModeMenuItem.Checked ? "Dark" : "Light";
            _settings.Save();
        }

        private void PhextForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SyncEditorState();
        }

        private void preferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadFile(_settings.IniFilePath, true);
        }

        private void defaultPhextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadDefaultPhext();
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
            // Ctrl-/: Reload
            if (e.Control && e.KeyCode == Keys.OemQuestion)
            {
                reloadMenuItem_Click(sender, e);
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

        private short GetSubCoordinate(char breakType)
        {
            var parts = phextCoordinate.Text.Split('/');
            if (parts.Length != 3)
            {
                return 1;
            }

            int primary = -1;
            if (breakType == PhextModel.LIBRARY_BREAK ||
                breakType == PhextModel.SHELF_BREAK ||
                breakType == PhextModel.SERIES_BREAK)
            {
                primary = 0;
            }
            if (breakType == PhextModel.COLLECTION_BREAK ||
                breakType == PhextModel.VOLUME_BREAK ||
                breakType == PhextModel.BOOK_BREAK)
            {
                primary = 1;
            }
            if (breakType == PhextModel.CHAPTER_BREAK ||
                breakType == PhextModel.SECTION_BREAK ||
                breakType == PhextModel.SCROLL_BREAK)
            {
                primary = 2;
            }

            if (primary == -1)
            {
                return 1;
            }

            var subparts = parts[primary].Split('.');
            if (subparts.Length != 3)
            {
                return 1;
            }

            int subindex = -1;
            if (breakType == PhextModel.LIBRARY_BREAK ||
                breakType == PhextModel.COLLECTION_BREAK ||
                breakType == PhextModel.CHAPTER_BREAK)
            {
                subindex = 0;
            }
            if (breakType == PhextModel.SHELF_BREAK ||
                breakType == PhextModel.VOLUME_BREAK ||
                breakType == PhextModel.SECTION_BREAK)
            {
                subindex = 1;
            }
            if (breakType == PhextModel.SERIES_BREAK ||
                breakType == PhextModel.BOOK_BREAK ||
                breakType == PhextModel.SCROLL_BREAK)
            {
                subindex = 2;
            }
            if (subindex == -1)
            {
                return 1;
            }

            return short.Parse(subparts[subindex]);
        }

        private string RebuildCoordinate(char breakType, short value)
        {
            Coordinates parts = GetPhextCoordinate();

            switch (breakType)
            {
                case PhextModel.LIBRARY_BREAK:
                    parts.Library = value;
                    break;
                case PhextModel.SHELF_BREAK:
                    parts.Shelf = value;
                    break;
                case PhextModel.SERIES_BREAK:
                    parts.Series = value;
                    break;
                case PhextModel.COLLECTION_BREAK:
                    parts.Collection = value;
                    break;
                case PhextModel.VOLUME_BREAK:
                    parts.Volume = value;
                    break;
                case PhextModel.BOOK_BREAK:
                    parts.Book = value;
                    break;
                case PhextModel.CHAPTER_BREAK:
                    parts.Chapter = value;
                    break;
                case PhextModel.SECTION_BREAK:
                    parts.Section = value;
                    break;
                case PhextModel.SCROLL_BREAK:
                    parts.Scroll = value;
                    break;
            }

            return parts.ToString();
        }

        private void BumpCoordinate(char breakType, short amount)
        {
            short value = (short)(GetSubCoordinate(breakType) + amount);
            if (value < 1) { value = 1; }
            if (value > 999) { value = 999; }
            phextCoordinate.Text = RebuildCoordinate(breakType, value);
        }

        private void UpDownHandler(char breakType, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
            {
                BumpCoordinate(breakType, -1);
            }
            if (e.KeyCode == Keys.Down)
            {
                BumpCoordinate(breakType, 1);
            }
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

        private void SetEditorTheme()
        {
            bool mode = _settings.DarkMode;
            treeView.BackColor = mode ? _settings.Color1 : _settings.Color2;
            treeView.ForeColor = mode ? _settings.Color2 : _settings.Color1;
            textBox.BackColor = mode ? _settings.Color1 : _settings.Color2;
            textBox.ForeColor = mode ? _settings.Color2 : _settings.Color1;
            BackColor = mode ? _settings.Color3 : _settings.Color4;
            menuStrip.BackColor = BackColor;
            fileToolStripMenuItem.BackColor = BackColor;
        }

        private void darkModeMenuItem_Click(object sender, EventArgs e)
        {
            _settings.Theme = darkModeMenuItem.Checked ? "Dark" : "Light";
            _settings.Save();
            SetEditorTheme();
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
                textBox.Width = Width - 24;
            }
            else
            {
                textBox.Left = treeView.Right;
                textBox.Width = Width - treeView.Width - 24;
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

        private void showCoordinatesToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            _settings.ShowCoordinates = showCoordinatesToolStripMenuItem.Checked;
            _settings.Save();
            LoadFile(_settings.Filename, false);
        }

        private void phextCoordinate_TextChanged(object sender, EventArgs e)
        {
            var prior = _model.Phext.Coords;
            var test = GetPhextCoordinate();
            if (!test.Intermediate && prior.ToString() != test.ToString())
            {
                jumpButton_Click(sender, e);
            }
        }
    }
}