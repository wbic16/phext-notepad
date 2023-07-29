using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

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
        private int _priorLine = 1;
        private int _priorColumn = 1;
        private Coordinates? _checkout = null;
        private TerseConfig _settings = new();
        private Font SCROLL_NODE_FONT = new("Cascadia Code", 11);

        public TerseForm(string[] args)
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
                LoadDefaultTerse();
            }

            if (_settings.Filename.Length > 0 && File.Exists(_settings.Filename))
            {
                LoadFile(_settings.Filename, false);
            }
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
            collectScroll();
        }

        private void collectScroll()
        {
            if (_checkout == null || _checkout.ToString() != _model.Coords.ToString())
            {
                return;
            }
            reloadMenuItem.Enabled = true;
            _model.Terse.setScroll(textBox.Text);
            if (textBox.Text.Length == 0)
            {
                return;
            }
            treeView.BeginUpdate();
            treeView.SuspendLayout();
            var node = getTreeNode(_model.Terse.Coords);
            if (node != null)
            {
                if (textBox.Lines != null && textBox.Lines.Length >= 1)
                {
                    var line = TerseModel.GetScrollSummary(_model.Terse.Coords, textBox.Text);
                    node.Text = _settings.ShowCoordinates ? $"{node.Name}? {line}" : line;
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

                    var line = TerseModel.GetScrollSummary(_model.Terse.Coords, textBox.Text);
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
            textBox.Text = _model.Terse.getScroll();
            _model.Terse.Coords.Line = 1;
            _priorLine = 1;
            _priorColumn = 1;
            libraryID.Enabled = true;
            libraryLabel.Enabled = true;
            shelfID.Enabled = true;
            shelfLabel.Enabled = true;
            seriesID.Enabled = true;
            seriesLabel.Enabled = true;
            collectionID.Enabled = true;
            collectionLabel.Enabled = true;
            volumeID.Enabled = true;
            volumeLabel.Enabled = true;
            bookID.Enabled = true;
            bookLabel.Enabled = true;
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
            _model.Terse.Coords.Reset();
            _settings.Coords = _model.Terse.Coords.ToString();
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
                    _model.Terse.Coords.Column = offset - total + 1;
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

            textBox.Enabled = _checkout != null;

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
                _model.Terse.processDelta(delta);
                loadScroll();
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

        private void jumpButton_Click(object sender, EventArgs e)
        {
            collectScroll();
            try
            {
                var update = new Coordinates
                {
                    Scroll = short.Parse(scrollID.Text),
                    Section = short.Parse(sectionID.Text),
                    Chapter = short.Parse(chapterID.Text),
                    Book = short.Parse(bookID.Text),
                    Volume = short.Parse(volumeID.Text),
                    Collection = short.Parse(collectionID.Text),
                    Series = short.Parse(seriesID.Text),
                    Shelf = short.Parse(shelfID.Text),
                    Library = short.Parse(libraryID.Text)
                };
                _model.Terse.Coords = update;
                loadScroll();
            }
            catch
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
            _model.Terse.Coords = test;
            UpdateUI("Delete");
            var node = getTreeNode(_model.Terse.Coords);
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
            scrollID.Text = _model.Coords.Scroll.ToString();
            sectionID.Text = _model.Coords.Section.ToString();
            chapterID.Text = _model.Coords.Chapter.ToString();
            bookID.Text = _model.Coords.Book.ToString();
            volumeID.Text = _model.Coords.Volume.ToString();
            collectionID.Text = _model.Coords.Collection.ToString();
            seriesID.Text = _model.Coords.Series.ToString();
            shelfID.Text = _model.Coords.Shelf.ToString();
            libraryID.Text = _model.Coords.Library.ToString();
            loadScroll();
            _checkout = new Coordinates(_model.Coords);
        }

        private void SyncEditorState()
        {
            SaveCurrentFile(false, false);
            _settings.Coords = _model.Terse.Coords.ToString();
            _settings.ZoomFactor = textBox.ZoomFactor;
            _settings.WordWrap = textBox.WordWrap;
            _settings.TreeView = treeView.Visible;
            _settings.Theme = darkModeMenuItem.Checked ? "Dark" : "Light";
            _settings.Save();
        }

        private void TerseForm_FormClosing(object sender, FormClosingEventArgs e)
        {
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

        private void libraryID_KeyUp(object sender, KeyEventArgs e)
        {
            HandleHotkeys(sender, e);
            UpDownHandler(libraryID, e);
            jumpButton_Click(sender, e);
        }

        private void shelfID_KeyUp(object sender, KeyEventArgs e)
        {
            HandleHotkeys(sender, e);
            UpDownHandler(shelfID, e);
            jumpButton_Click(sender, e);
        }

        private void seriesID_KeyUp(object sender, KeyEventArgs e)
        {
            HandleHotkeys(sender, e);
            UpDownHandler(seriesID, e);
            jumpButton_Click(sender, e);
        }

        private void collectionID_KeyUp(object sender, KeyEventArgs e)
        {
            HandleHotkeys(sender, e);
            UpDownHandler(collectionID, e);
            jumpButton_Click(sender, e);
        }

        private void volumeID_KeyUp(object sender, KeyEventArgs e)
        {
            HandleHotkeys(sender, e);
            UpDownHandler(volumeID, e);
            jumpButton_Click(sender, e);
        }

        private void bookID_KeyUp(object sender, KeyEventArgs e)
        {
            HandleHotkeys(sender, e);
            UpDownHandler(bookID, e);
            jumpButton_Click(sender, e);
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

        private void BumpCoordinate(TextBox box, int amount)
        {
            var value = int.Parse(box.Text) + amount;
            if (value < 1) { value = 1; }
            if (value > 9999) { value = 9999; }
            box.Text = value.ToString();
        }

        private void UpDownHandler(TextBox box, KeyEventArgs e)
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
    }
}