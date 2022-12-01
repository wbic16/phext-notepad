using System.Text;

namespace TerseNotepad
{
    public partial class TerseForm : Form
    {
        private static readonly string TERSE_FILTER = "Terse File (*.t)|*.t|All files (*.*)|*.*";
        private TerseText _terse = new();

        // Editor State
        private uint _priorLine = 1;
        private uint _priorColumn = 1;
        private string _filename = "";

        public TerseForm()
        {
            InitializeComponent();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(@"Terse Notepad
Version 1.0

A reference editor for multi-dimensional text.

Use F2 - F11 to access additional dimensions.
", "Terse Notepad v1.0");
        }

        private void textBox_TextChanged(object sender, EventArgs e)
        {
            collectPage();
        }

        private void collectPage()
        {
            vScrollBar1.Maximum = textBox.Lines.Length;
            _terse.setPageText(textBox.Text);
        }

        private void loadPage()
        {
            textBox.Text = _terse.getPage();
            _terse.Coords.Line = 1;
            _priorLine = 1;
            _priorColumn = 1;
            // todo: enable the remaining dimensions
            // multiverseID.Enabled = true;
            // multiverseLabel.Enabled = true;
            // galaxyID.Enabled = true;
            // galaxyLabel.Enabled = true;
            // worldID.Enabled = true;
            // worldLabel.Enabled = true;
            // languageID.Enabled = true;
            // languageLabel.Enabled = true;
            // branchID.Enabled = true;
            // branchLabel.Enabled = true;
            // volumeID.Enabled = true;
            // volumeLabel.Enabled = true;
            setID.Enabled = true;
            setLabel.Enabled = true;
            groupID.Enabled = true;
            groupLabel.Enabled = true;
            pageID.Enabled = true;
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
                FileName = _filename
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
            _filename = filename;
            LoadData(data);
        }

        private void LoadData(string data)
        {
            var dimensionStream = data.Split("\x19\n");
            _terse.Set = new();
            uint set_index = 1;
            foreach (var set in dimensionStream)
            {                
                uint group_index = 1;
                var groups = set.Split("\x18\n");
                foreach (var group in groups)
                {                    
                    uint page_index = 1;
                    var pages = group.Split("\x17\n");
                    foreach (var page in pages)
                    {
                        _terse.Coords.Page = page_index;
                        _terse.Coords.Group = group_index;
                        _terse.Coords.Set = set_index;
                        _terse.setPageText(page);
                        ++page_index;
                    }
                    ++group_index;
                }
                ++set_index;
            }
            jumpToOrigin();            
            loadPage();
        }

        private void jumpToOrigin()
        {
            _terse.Coords.Reset();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _filename = "";
            jumpToOrigin();
            LoadData("");
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveCurrentFile();
        }

        private void SaveCurrentFile()
        {
            var collector = new StringBuilder();
            uint set_break_counter = 0;            
            foreach (var set_index in _terse.Set.Keys)
            {
                while (++set_break_counter < set_index)
                {
                    collector.Append("\x19\n");
                }
                uint group_break_counter = 0;
                foreach (var group_index in _terse.Set[set_index].Keys)
                {
                    while (++group_break_counter < group_index)
                    {
                        collector.Append("\x18\n");
                    }
                    uint page_break_counter = 0;
                    foreach (var page_index in _terse.Set[set_index][group_index].Keys)
                    {
                        while (++page_break_counter < page_index)
                        {
                            collector.Append("\x17\n");
                        }
                        collector.Append(_terse.Set[set_index][group_index][page_index]);
                        collector.Append("\x17\n");
                    }
                    collector.Append("\x18\n");
                }
                collector.Append("\x19\n");
            }
            var saver = new SaveFileDialog
            {
                Filter = TERSE_FILTER
            };
            var result = saver.ShowDialog();
            if (result == DialogResult.OK)
            {
                File.WriteAllText(saver.FileName, collector.ToString());
            }
        }

        private void vScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            _terse.Coords.Line = (uint)vScrollBar1.Value;
        }

        private void vScrollBar2_ValueChanged(object sender, EventArgs e)
        {

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
            multiverseID.Text = _terse.Coords.Multiverse.ToString();
            galaxyID.Text = _terse.Coords.Galaxy.ToString();
            worldID.Text = _terse.Coords.World.ToString();
            languageID.Text = _terse.Coords.Language.ToString();
            branchID.Text = _terse.Coords.Branch.ToString();
            volumeID.Text = _terse.Coords.Volume.ToString();
            setID.Text = _terse.Coords.Set.ToString();
            groupID.Text = _terse.Coords.Group.ToString();
            pageID.Text = _terse.Coords.Page.ToString();
            status.Text = _terse.EditorSummary();
        }

        private void textBox_KeyUp(object sender, KeyEventArgs e)
        {
            int page_delta = 0;
            int group_delta = 0;
            int set_delta = 0;
            bool arrowShifted = false;

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
                collectPage();
                _terse.processDelta(set_delta, group_delta, page_delta);
                loadPage();
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
            foreach (var s in _terse.Set.Keys)
            {
                foreach (var g in _terse.Set[s].Keys)
                {
                    foreach (var p in _terse.Set[s][g].Keys)
                    {
                        var content = _terse.Set[s][g][p];
                        var count = content.Length;
                        var summary = content.Split("\n")[0];
                        if (summary.Length > 40) { summary = summary[..40]; }
                        result.AppendLine($"Set: {s}, Group: {g}, Page: {p} has {count} bytes = {summary}.");
                    }
                }
            }

            textBox.AppendText(result.ToString());
        }

        private void jumpButton_Click(object sender, EventArgs e)
        {
            collectPage();
            _terse.Coords.Set = uint.Parse(setID.Text);
            _terse.Coords.Group = uint.Parse(groupID.Text);
            _terse.Coords.Page = uint.Parse(pageID.Text);
            loadPage();
        }
    }
}