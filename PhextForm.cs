using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Windows.Forms;

namespace PhextNotepad
{
    public partial class PhextForm : Form
    {
        private static readonly string PHEXT_FILTER = "Phext (*.phext)|*.phext|All files (*.*)|*.*";
        private PhextModel _model = new();
        private static readonly HttpClient _http = new HttpClient();
        private bool _syncing = false;
        private int _applyingTreeView = 0;
        private bool _splitterReady = false;
        private int _treeSplitDistance = 642;

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
            treeView.BeforeExpand += treeView_BeforeExpand;

            try
            {
                editorSplit.Panel1MinSize = 120;
                editorSplit.Panel2MinSize = 160;
            }
            catch (ArgumentOutOfRangeException)
            {
                // Min sizes are retried once the split has a real width.
            }
            _treeSplitDistance = _settings.TreeSplitDistance;
            ApplySplitterDistance();

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
            var stream = currentAssembly.GetManifestResourceStream("PhextNotepad.Baseline.phext");
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

A reference editor for Plain Hypertext.

Contact me (Will Bickford) at x.com/wbic16 for more info!
", $"Phext Notepad {version.Major}.{version.Minor}.{version.Build}");
        }

        private void textBox_TextChanged(object sender, EventArgs e)
        {
            collectScroll();
        }

        private void collectScroll()
        {
            if (_loading || _checkout == null || _checkout.ToString() != _model.Coords.ToString())
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
            var node = FindMaterializedNode(_model.Phext.Coords.ToString());
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
                // The scroll's node is not in the tree yet (a new
                // scroll, or a section never expanded). setScroll
                // above has already created the scroll in the model,
                // so materializing its path brings the node in with
                // its siblings; if it is still missing afterwards,
                // add it directly under its section.
                MaterializeNodePath(_model.Phext.Coords, out bool found);
                node = found ? FindMaterializedNode(_model.Phext.Coords.ToString()) : null;
                if (node == null)
                {
                    var section = FindMaterializedNode(_model.Phext.Coords.GetSectionRoot().ToString());
                    if (section != null)
                    {
                        var line = PhextModel.GetScrollSummary(_model.Phext.Coords, textBox.Text);
                        var key = _model.Phext.Coords.ToString();
                        node = section.Nodes.Add(key, _settings.ShowCoordinates ? $"{key}: {line}" : line);
                        node.Tag = PhextTree.ScrollEntry(_model.Phext, _model.Phext.Coords, _settings.ShowCoordinates);
                    }
                }
                if (node != null)
                {
                    node.NodeFont = SCROLL_NODE_FONT;
                    treeView.SelectedNode = node;
                    textBox.SelectionStart = textBox.Text.Length;
                    // Only the new node needs to become visible; this
                    // used to ExpandAll(), walking every node in the
                    // tree on the keystroke that created a scroll.
                    node.EnsureVisible();
                }
            }

            treeView.ResumeLayout();
            treeView.EndUpdate();
        }

        private void loadScroll()
        {
            textBox.SuspendLayout();
            _checkout = new Coordinates(_model.Coords);
            textBox.Text = _model.Phext.getScroll();
            String[] test = textBox.Text.Split("\n");
            if (test.Count() < _model.Phext.Coords.Line)
            {
                _model.Phext.Coords.Line = 1;
                _priorLine = 1;
                _priorColumn = 1;
            }
            phextCoordinate.Enabled = true;
            coordinateLabel.Enabled = true;
            UpdateUI($"Loaded {_settings.Filename}");

            if (treeView.Visible)
            {
                treeView.Enabled = true;
                SelectNodeInTree(_model.Coords);
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
            _treeSplitDistance = _settings.TreeSplitDistance;
            SetTreeViewEnabled(_settings.TreeView);
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

        private void LoadFile(string? filename, bool resetView)
        {
            if (filename == null) { return; }
            RefreshSettings();
            if (File.Exists(filename))
            {
                _checkout = null;
                var data = File.ReadAllText(filename);
                _settings.Filename = filename;
                syncRemoteFromFilename(filename);
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

        private bool _loading = false;

        private async void LoadData(string data, bool resetView, bool resetPhext = true)
        {
            if (_loading)
            {
                return;
            }
            _loading = true;
            try
            {
                if (resetPhext)
                {
                    treeView.Nodes.Clear();
                }
                status.Text = $"Loading {_settings.Filename}...";
                // The parse + model build runs off the UI thread: on
                // a large phext this used to block the window for the
                // entire load (Task Manager: Not Responding). Only the
                // model is touched on that thread; the tree is
                // populated back here, roots only — the rest of the
                // tree materializes as the user expands it.
                await Task.Run(() => _model.Load(data, resetPhext));
                if (_model.PendingScrolls.Count() > 0 && _syncing)
                {
                    var list = _model.PendingScrolls.ToList();
                    foreach (var (key, value) in list)
                    {
                        string syncKey = new(key.ToString());
                        string syncValue = new(value);
                        await pushScroll(syncKey, syncValue);
                    }
                }
                PopulateTreeRoots();
            }
            finally
            {
                _loading = false;
            }
            if (resetView)
            {
                jumpToOrigin();
            }
            else
            {
                coordinateJump(_settings.Coords, false);
            }
        }

        private TreeNode MakeTreeNode(PhextTreeEntry entry)
        {
            var node = new TreeNode(entry.Text)
            {
                Name = entry.Name,
                Tag = entry
            };
            if (entry.HasChildren)
            {
                // Placeholder child: keeps the [+] affordance without
                // paying for the real children until first expansion.
                node.Nodes.Add(new TreeNode(""));
            }
            return node;
        }

        // Replaces a node's placeholder with its real children,
        // described from the model as it stands right now. Nodes whose
        // children are already materialized (or that have none) are
        // left alone.
        private void PopulateTreeNode(TreeNode node)
        {
            if (node.Tag is not PhextTreeEntry entry || entry.IsScroll)
            {
                return;
            }
            if (node.Nodes.Count != 1 || node.Nodes[0].Tag != null)
            {
                return;
            }
            node.Nodes.Clear();
            foreach (var child in PhextTree.Children(_model.Phext, entry.Coord, entry.Level, _settings.ShowCoordinates))
            {
                node.Nodes.Add(MakeTreeNode(child));
            }
        }

        private void treeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node != null)
            {
                PopulateTreeNode(e.Node);
            }
        }

        private void PopulateTreeRoots()
        {
            treeView.BeginUpdate();
            treeView.Nodes.Clear();
            foreach (var entry in PhextTree.Roots(_model.Phext, _settings.ShowCoordinates))
            {
                treeView.Nodes.Add(MakeTreeNode(entry));
            }
            treeView.EndUpdate();
        }

        private TreeNode? FindMaterializedNode(string name)
        {
            foreach (TreeNode root in treeView.Nodes)
            {
                var found = FindMaterializedNode(root, name);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private TreeNode? FindMaterializedNode(TreeNode node, string name)
        {
            if (node.Name == name)
            {
                return node;
            }
            foreach (TreeNode child in node.Nodes)
            {
                if (child.Tag == null)
                {
                    continue;
                }
                var found = FindMaterializedNode(child, name);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        // Materializes the TreeNodes along the path to `target`,
        // expanding each ancestor. Returns the deepest node reached;
        // `targetFound` is true only when the target itself exists in
        // the model (for a scroll target: the scroll is present), which
        // is what distinguishes "select it" from the old full-tree
        // Find coming up empty.
        private TreeNode? MaterializeNodePath(Coordinates target, out bool targetFound)
        {
            targetFound = false;
            var path = PhextTree.PathTo(_model.Phext, target, _settings.ShowCoordinates);
            TreeNodeCollection level = treeView.Nodes;
            TreeNode? current = null;
            foreach (var entry in path)
            {
                TreeNode? next = null;
                foreach (TreeNode candidate in level)
                {
                    if (candidate.Name == entry.Name)
                    {
                        next = candidate;
                        break;
                    }
                }
                if (next == null)
                {
                    break;
                }
                current = next;
                if (entry.IsScroll)
                {
                    targetFound = true;
                }
                else
                {
                    PopulateTreeNode(current);
                    current.Expand();
                    level = current.Nodes;
                }
            }
            return current;
        }

        private void SelectNodeInTree(Coordinates target)
        {
            var node = MaterializeNodePath(target, out bool found);
            if (node != null && found)
            {
                treeView.SelectedNode = node;
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
            if (!_settings.Filename.EndsWith("PhextNotepad\\Phext.ini"))
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
            delta.Library = 0;
            delta.Shelf = 0;
            delta.Series = 0;
            delta.Collection = 0;
            delta.Volume = 0;
            delta.Book = 0;
            delta.Chapter = 0;
            delta.Section = 0;
            delta.Scroll = 0;
            delta.Line = 0;
            delta.Column = 0;

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
            var level = PhextTree.LevelOf(test);
            if (level < 0)
            {
                return;
            }
            if (requestConfirmation)
            {
                var count = PhextTree.CountSubtree(_model.Phext, test, level);
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

            // Clear every scroll in the subtree from the model
            // (setScroll("") removes it, pruning ancestors that become
            // empty). The old implementation recursed over the eager
            // tree's materialized children; the model walk reaches the
            // same scrolls whether or not they were ever expanded.
            foreach (var scrollCoord in PhextTree.SubtreeScrolls(_model.Phext, test, level))
            {
                if (scrollCoord.ToString() == test.ToString())
                {
                    continue;
                }
                _model.Phext.Coords = scrollCoord;
                _model.Phext.setScroll("");
            }
            _model.Phext.Coords = test;
            _checkout = test;
            FindMaterializedNode(coordinates)?.Remove();
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
            if (!editorSplit.Panel1Collapsed)
            {
                RememberSplitterDistance();
            }
            _settings.TreeView = !editorSplit.Panel1Collapsed;
            _settings.TreeSplitDistance = _treeSplitDistance;
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
                var key = treeView?.SelectedNode?.Name;
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
            var coordinates = treeView?.SelectedNode?.Name;
            if (coordinates == null || coordinates.Length == 0)
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
            bool show = !lockToScrollMenuItem.Checked;
            if (!show)
            {
                RememberSplitterDistance();
            }
            SetTreeViewEnabled(show);
        }

        private void SetTreeViewEnabled(bool show)
        {
            // Collapse and restore both raise SplitterMoved. Hold the guard across
            // the whole change so that event cannot replace the saved distance.
            _applyingTreeView++;
            try
            {
                if (show)
                {
                    editorSplit.Panel1Collapsed = false;
                    treeView.Visible = true;
                    treeView.Enabled = true;
                    ApplySplitterDistance();
                }
                else
                {
                    treeView.Enabled = false;
                    editorSplit.Panel1Collapsed = true;
                    treeView.Visible = false;
                }
                _settings.TreeView = show;

                if (showTreeViewToolStripMenuItem.Checked != show)
                {
                    showTreeViewToolStripMenuItem.Checked = show;
                }
            }
            finally
            {
                _applyingTreeView--;
            }
        }

        private void RememberSplitterDistance()
        {
            // Before the first show, SplitterDistance is still the designer default.
            // Keep the value loaded from settings until the user actually moves it.
            if (!_splitterReady || editorSplit.Panel1Collapsed || editorSplit.Width <= 0)
            {
                return;
            }
            _treeSplitDistance = editorSplit.SplitterDistance;
        }

        private void ApplySplitterDistance()
        {
            if (editorSplit.Width <= editorSplit.SplitterWidth)
            {
                return;
            }
            try
            {
                if (editorSplit.Panel1MinSize < 120)
                {
                    editorSplit.Panel1MinSize = 120;
                }
                if (editorSplit.Panel2MinSize < 160)
                {
                    editorSplit.Panel2MinSize = 160;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                return;
            }
            catch (InvalidOperationException)
            {
                return;
            }
            if (editorSplit.Panel1Collapsed)
            {
                return;
            }
            int available = editorSplit.Width - editorSplit.SplitterWidth;
            int min = editorSplit.Panel1MinSize;
            int max = available - editorSplit.Panel2MinSize;
            if (max < min)
            {
                return;
            }
            int distance = _treeSplitDistance;
            if (distance < min)
            {
                distance = min;
            }
            if (distance > max)
            {
                distance = max;
            }
            if (editorSplit.SplitterDistance == distance)
            {
                return;
            }
            _applyingTreeView++;
            try
            {
                editorSplit.SplitterDistance = distance;
            }
            catch (InvalidOperationException)
            {
                // The split container is not wide enough yet; OnShown retries.
            }
            finally
            {
                _applyingTreeView--;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplySplitterDistance();
            _splitterReady = true;
        }

        private void editorSplit_SplitterMoved(object sender, SplitterEventArgs e)
        {
            if (_applyingTreeView > 0 || !_splitterReady || editorSplit.Panel1Collapsed)
            {
                return;
            }
            RememberSplitterDistance();
            _settings.TreeSplitDistance = _treeSplitDistance;
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

        private string computePhextHierarchicalChecksum()
        {
            return _model.getHierarchicalChecksum();
        }
        private string composeRemoteUrl(string action, string coordinate, string content = "")
        {
            // http://127.0.0.1:1337/api/v2/select?p=choose-your-own-adventure&c=1.1.1/1.1.1/1.1.2
            // http://127.0.0.1:1337/api/v2/update?p=choose-your-own-adventure&c=1.1.1/1.1.1/1.1.2&s=content
            var endpoint = sqHost.Text;
            var phext = Uri.EscapeDataString(sqPhext.Text);
            var url = $"{endpoint}/{action}?p={phext}&c={coordinate}";
            if (content.Length > 0)
            {
                url += $"&s={Uri.EscapeDataString(content)}";
            }
            return url;
        }

        private void applyApiKey(HttpRequestMessage request)
        {
            var key = sqApiKey.Text.Trim();
            if (key.Length > 0)
            {
                request.Headers.Add("X-SQ-API-Key", key);
            }
        }

        private async Task<string> remoteGet(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            applyApiKey(request);
            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        private async Task<HttpResponseMessage> remotePost(string url, string body, string mediaType = "application/phext")
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            applyApiKey(request);
            request.Content = new StringContent(body, System.Text.Encoding.UTF8, mediaType);
            return await _http.SendAsync(request);
        }

        private void syncFilenameFromRemote()
        {
            var phextName = sqPhext.Text.Trim();
            if (phextName.Length == 0) { return; }

            // If we have a current local file, update it to match the remote phext name
            var dir = _settings.Filename.Length > 0
                ? Path.GetDirectoryName(_settings.Filename) ?? ""
                : "";
            var newFilename = phextName.EndsWith(".phext") ? phextName : $"{phextName}.phext";
            if (dir.Length > 0)
            {
                newFilename = Path.Combine(dir, newFilename);
            }
            _settings.Filename = newFilename;
        }

        private void syncRemoteFromFilename(string filename)
        {
            if (filename == null || filename.Length == 0) { return; }
            var stem = Path.GetFileNameWithoutExtension(filename);
            if (stem.Length > 0)
            {
                sqPhext.Text = stem;
            }
        }

        private async void pullButton_Click(object sender, EventArgs e)
        {
            try
            {
                var coordinate = phextCoordinate.Text;
                var url = composeRemoteUrl("select", coordinate);
                if (url != null && url.Length > 0)
                {
                    UpdateUI("Pulling...");
                    var content = await remoteGet(url);
                    textBox.Text = content;
                    SyncEditorState();
                    syncFilenameFromRemote();
                    UpdateUI("Pull OK");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error pulling from remote: {ex.Message}", "Pull Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateUI("Pull Failure");
            }
        }

        private async void pushButton_Click(object sender, EventArgs e)
        {
            try
            {
                UpdateUI("Pushing...");
                var coordinate = phextCoordinate.Text;
                var worked = await pushScroll(coordinate, textBox.Text);
                if (worked)
                {
                    UpdateUI("Push OK");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error pushing to remote: {ex.Message}", "Push Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateUI("Push Failure");
            }
        }

        private async Task<bool> pushScroll(string coordinate, string scroll)
        {
            try
            {
                var url = composeRemoteUrl("update", coordinate);
                if (url != null && url.Length > 0)
                {
                    var ignored = await remotePost(url, scroll);
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error pushing to remote: {ex.Message}", "Push Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return false;
        }

        private async void syncButton_Click(object sender, EventArgs e)
        {
            try
            {
                var hash = computePhextHierarchicalChecksum();
                var coordinate = phextCoordinate.Text;
                var url = composeRemoteUrl("delta", coordinate);
                if (url != null && url.Length > 0)
                {
                    UpdateUI("Syncing...");
                    _syncing = true;
                    var handle = await remotePost(url, hash);
                    var response = await handle.Content.ReadAsStringAsync();
                    LoadData(response, true, false);
                    _syncing = false;

                    // Hack to re-render the treeView
                    SaveCurrentFile(false, true);
                    syncFilenameFromRemote();
                    UpdateUI("Sync OK");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error pulling from remote: {ex.Message}", "Pull Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateUI("Sync Failure");
            }
        }

        private void labelApiKey_Click(object sender, EventArgs e)
        {
            MessageBox.Show("The API key is used to authenticate with the SQ API for remote synchronization. It should be kept secret and only shared with trusted parties. To generate an API key, use the 'Generate API Key' button in the SQ application. Once generated, copy the API key and paste it into this field to enable authenticated requests to the SQ API.", "API Key Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void showTreeViewToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (_applyingTreeView > 0)
            {
                return;
            }

            if (!showTreeViewToolStripMenuItem.Checked)
            {
                RememberSplitterDistance();
            }
            SetTreeViewEnabled(showTreeViewToolStripMenuItem.Checked);
            _settings.TreeSplitDistance = _treeSplitDistance;
            _settings.Save();
        }
    }
}