using System.Reflection;
using System.Runtime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

        public TerseForm()
        {
            InitializeComponent();
            treeViewToolStripMenuItem.Checked = _settings.TreeView;
            treeView.Visible = _settings.TreeView;
            coordinateJump(_settings.Coords);
            if (File.Exists(_settings.Filename))
            {
                LoadFile(_settings.Filename);
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
            _terse.setScrollText(textBox.Text);
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
            chapterID.Enabled = true;
            chapterLabel.Enabled = true;
            sectionID.Enabled = true;
            sectionLabel.Enabled = true;
            scrollID.Enabled = true;
            pageLabel.Enabled = true;
            UpdateStatusBar();
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

        private void LoadFile(string filename)
        {
            var data = File.ReadAllText(filename);
            _settings.Filename = filename;
            LoadData(data);
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
                foreach (var section in sections)
                {
                    uint scroll_index = 1;
                    var scrolls = section.Split(SCROLL_BREAK);
                    var scrollNode = new TreeNode($"Section {scroll_index}");
                    foreach (var scroll in scrolls)
                    {
                        _terse.Coords.Chapter = chapter_index;
                        _terse.Coords.Section = section_index;
                        _terse.Coords.Scroll = scroll_index;
                        if (scroll.Length > 0)
                        {
                            _terse.setScrollText(scroll);
                            var key = $"{scroll_index}-{section_index}-{chapter_index}";
                            var line = getScrollSummary(_terse.Coords, scroll);
                            scrollNode.Nodes.Add(key, line);
                        }
                        ++scroll_index;
                    }
                    if (scrollNode.Nodes.Count > 0)
                    {
                        chapterNode.Nodes.Add(scrollNode);
                    }
                    ++section_index;
                }
                if (chapterNode.Nodes.Count > 0)
                {
                    treeView.Nodes.Add(chapterNode);
                }
                ++chapter_index;
            }
            jumpToOrigin();
            loadScroll();
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
            treeView.ExpandAll();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _settings.Filename = "";
            jumpToOrigin();
            LoadData("");
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _settings.Save();
            Close();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveCurrentFile();
        }

        private void SaveCurrentFile()
        {
            var collector = new StringBuilder();
            uint chapter_break_counter = 0;
            foreach (var chapter_index in _terse.Chapter.Keys)
            {
                while (++chapter_break_counter < chapter_index)
                {
                    collector.Append(CHAPTER_BREAK);
                }
                uint section_break_counter = 0;
                foreach (var section_index in _terse.Chapter[chapter_index].Keys)
                {
                    while (++section_break_counter < section_index)
                    {
                        collector.Append(SECTION_BREAK);
                    }
                    uint scroll_break_counter = 0;
                    foreach (var scroll_index in _terse.Chapter[chapter_index][section_index].Keys)
                    {
                        while (++scroll_break_counter < scroll_index)
                        {
                            collector.Append(SCROLL_BREAK);
                        }
                        collector.Append(_terse.Chapter[chapter_index][section_index][scroll_index]);
                        collector.Append(SCROLL_BREAK);
                    }
                    collector.Append(SECTION_BREAK);
                }
                collector.Append(CHAPTER_BREAK);
            }

            bool saved = false;
            try
            {
                File.WriteAllText(_settings.Filename, collector.ToString());
            }
            catch (System.ArgumentException)
            {
                ChooseSaveFilename();
            }

            if (!saved)
            {
                try
                {
                    File.WriteAllText(_settings.Filename, collector.ToString());
                }
                catch (System.ArgumentException)
                {
                    MessageBox.Show("Unable to save - please choose a different filename.", "Save Error");
                }
            }
        }

        private void textBox_SelectionChanged(object sender, EventArgs e)
        {
            _terse.Coords.Line = 1;
            var total = 0;
            var offset = textBox.SelectionStart;
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

        private void UpdateStatusBar()
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
            status.Text = _terse.EditorSummary();
        }

        private void ChooseSaveFilename()
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
            }
        }

        private void textBox_KeyUp(object sender, KeyEventArgs e)
        {
            int page_delta = 0;
            int group_delta = 0;
            int set_delta = 0;
            bool arrowShifted = false;

            // Ctrl-Shift-S: Save As File
            if (e.Control && e.Shift && e.KeyCode == Keys.S)
            {
                ChooseSaveFilename();
                SaveCurrentFile();
                e.Handled = true;
                return;
            }
            // Ctrl-S: Save File
            if (e.Control && e.KeyCode == Keys.S)
            {
                SaveCurrentFile();
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

            // Set Shifts
            if (!e.Shift && e.KeyCode == Keys.F4)
            {
                set_delta = 1;
                e.Handled = true;
            }
            if (e.Shift && e.KeyCode == Keys.F4)
            {
                set_delta = -1;
                e.Handled = true;
            }
            if (e.Control && e.KeyCode == Keys.PageDown)
            {
                set_delta = 1;
                e.Handled = true;
            }
            if (e.Control && e.KeyCode == Keys.PageUp)
            {
                set_delta = -1;
                e.Handled = true;
            }

            // Group Shifts
            if (!e.Shift && e.KeyCode == Keys.F3)
            {
                group_delta = 1;
                e.Handled = true;
            }
            if (e.Shift && e.KeyCode == Keys.F3)
            {
                group_delta = -1;
                e.Handled = true;
            }
            if (e.Shift && e.KeyCode == Keys.PageDown)
            {
                group_delta = 1;
                e.Handled = true;
            }
            if (e.Shift && e.KeyCode == Keys.PageUp)
            {
                group_delta = -1;
                e.Handled = true;
            }

            // Page Shifts
            if (!e.Shift && e.KeyCode == Keys.F2)
            {
                page_delta = 1;
                e.Handled = true;
            }
            if (e.Shift && e.KeyCode == Keys.F2)
            {
                page_delta = -1;
                e.Handled = true;
            }
            if (!e.Shift && !e.Control && e.KeyCode == Keys.PageDown)
            {
                page_delta = 1;
                e.Handled = true;
            }
            if (!e.Shift && !e.Control && e.KeyCode == Keys.PageUp)
            {
                page_delta = -1;
                e.Handled = true;
            }
            var reachedEnd = textBox.SelectionStart == textBox.TextLength &&
                textBox.Lines.Length > 0 &&
                _priorColumn > textBox.Lines.Last().Length;
            if (!e.Shift && e.KeyCode == Keys.Right && reachedEnd)
            {
                page_delta = 1;
                arrowShifted = true;
                e.Handled = true;
            }
            var reachedStart = textBox.SelectionStart == 0 &&
                               _priorColumn == 1;
            if (!e.Shift && e.KeyCode == Keys.Left && reachedStart)
            {
                page_delta = -1;
                arrowShifted = true;
                e.Handled = true;
            }
            if (!e.Shift && e.KeyCode == Keys.Down && _priorLine == _terse.Coords.Line)
            {
                page_delta = 1;
                arrowShifted = true;
                e.Handled = true;
            }
            if (!e.Shift && e.KeyCode == Keys.Up && _priorLine == 1)
            {
                page_delta = -1;
                arrowShifted = true;
                e.Handled = true;
            }
            if (page_delta != 0 || group_delta != 0 || set_delta != 0)
            {
                collectScroll();
                _terse.processDelta(set_delta, group_delta, page_delta);
                loadScroll();
                if (arrowShifted)
                {
                    if (page_delta < 0)
                    {
                        textBox.SelectionStart = textBox.TextLength > 1 ? textBox.TextLength - 1 : 0;
                    }
                    if (page_delta > 0)
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
                foreach (var section_index in _terse.Chapter[chapter_index].Keys)
                {
                    foreach (var scroll_index in _terse.Chapter[chapter_index][section_index].Keys)
                    {
                        var content = _terse.Chapter[chapter_index][section_index][scroll_index];
                        var count = content.Length;
                        var summary = content.Split("\n")[0];
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
            _terse.Coords.Chapter = uint.Parse(chapterID.Text);
            _terse.Coords.Section = uint.Parse(sectionID.Text);
            _terse.Coords.Scroll = uint.Parse(scrollID.Text);
            loadScroll();
        }

        private void treeViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeView.Visible = treeViewToolStripMenuItem.Checked;
            _settings.TreeView = treeView.Visible;
        }

        private void treeView_DoubleClick(object sender, EventArgs e)
        {
            var coordinates = treeView.SelectedNode.Name;
            if (coordinates.Length == 0)
            {
                return;
            }
            collectScroll();
            coordinateJump(coordinates);
        }

        private void coordinateJump(string coordinates)
        {
            if (!coordinates.Contains('-'))
            {
                return;
            }
            _terse.Coords.Load(coordinates);
            chapterID.Text = _terse.Coords.Chapter.ToString();
            sectionID.Text = _terse.Coords.Section.ToString();
            scrollID.Text = _terse.Coords.Scroll.ToString();
            loadScroll();
        }

        private void TerseForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _settings.Save();
        }
    }
}