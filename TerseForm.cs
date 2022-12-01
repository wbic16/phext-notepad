using System.Security.Policy;
using System.Text;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace TerseNotepad
{
    public partial class TerseForm : Form
    {
        private static readonly string TERSE_FILTER = "Terse File (*.t)|*.t|All files (*.*)|*.*";
        private SortedDictionary<uint, SortedDictionary<uint, SortedDictionary<uint, string>>> _set = new();
        private SortedDictionary<uint, SortedDictionary<uint, string>> _group
        {
            get
            {
                if (!_set.ContainsKey(_coords.Set))
                {
                    _set[_coords.Set] = new();
                }
                return _set[_coords.Set];
            }
            set
            {
                _set[_coords.Set] = value;
            }
        }
        private SortedDictionary<uint, string> _data
        {
            get
            {
                if (!_group.ContainsKey(_coords.Group))
                {
                    _group[_coords.Group] = new();
                }
                return _group[_coords.Group];
            }
            set
            {
                _group[_coords.Group] = value;
            }
        }

        // Editor State
        private uint _priorLine = 1;
        private uint _priorColumn = 1;
        private string _filename = "";

        // Coordinates
        private class Coordinates
        {
            public uint Column { get; set; } = 1;
            public uint Line { get; set; } = 1;
            public uint Page { get; set; } = 1;
            public uint Group { get; set; } = 1;
            public uint Set { get; set; } = 1;
            public uint Volume { get; set; } = 1;
            public uint Branch { get; set; } = 1;
            public uint Language { get; set; } = 1;
            public uint World { get; set; } = 1;
            public uint Galaxy { get; set; } = 1;
            public uint Multiverse { get; set; } = 1;

            public override string ToString()
            {
                return $"Multiverse: {Multiverse},  Galaxy: {Galaxy},  World: {World},  Language: {Language},  Branch: {Branch},  Volume: {Volume},  Set: {Set},  Group:  {Group},  Page: {Page},  Line: {Line},  Column: {Column}";
            }

            public string EditorSummary()
            {
                return $"Line: {Line},  Column: {Column}";
            }

            public void Reset()
            {
                Column = 1;
                Line = 1;
                Page = 1;
                Group = 1;
                Set = 1;
                Volume = 1;
                Branch = 1;
                Language = 1;
                World = 1;
                Galaxy = 1;
                Multiverse = 1;
            }
        };
        private Coordinates _coords = new Coordinates();
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
            _data[_coords.Page] = textBox.Text;
        }

        private void loadPage()
        {
            if (!_data.ContainsKey(_coords.Page))
            {
                _data[_coords.Page] = "";
            }
            textBox.Text = _data[_coords.Page];
            _coords.Line = 1;
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
                Filter = TERSE_FILTER
            };
            dialog.FileName = _filename;
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
            var sets = data.Split("\x19\n");
            _coords.Set = 1;
            _set = new();
            foreach (var set in sets)
            {
                var groups = set.Split("\x18\n");
                _coords.Group = 1;
                foreach (var group in groups)
                {
                    var pages = group.Split("\x17\n");
                    _coords.Page = 1;
                    foreach (var page in pages)
                    {
                        _data[_coords.Page] = page;
                        ++_coords.Page;
                    }
                    ++_coords.Group;
                }
                ++_coords.Set;
            }
            jumpToOrigin();            
            loadPage();
        }

        private void jumpToOrigin()
        {
            _coords.Reset();
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
            var text = "";
            var group_break_counter = 0;
            var set_break_counter = 0;
            foreach (var set_index in _set.Keys)
            {
                _coords.Set = set_index;
                foreach (var group_index in _group.Keys)
                {
                    _coords.Group = group_index;
                    foreach (var page in _data)
                    {
                        text += page.Value;
                        text += "\x17\n";
                    }
                    while (group_break_counter++ < _coords.Group)
                    {
                        text += "\x18\n";
                    }
                }
                while (set_break_counter++ < _coords.Set)
                {
                    text += "\x19\n";
                }
            }
            var saver = new SaveFileDialog
            {
                Filter = TERSE_FILTER
            };
            var result = saver.ShowDialog();
            if (result == DialogResult.OK)
            {
                File.WriteAllText(saver.FileName, text);
            }
        }

        private void vScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            _coords.Line = (uint)vScrollBar1.Value;
        }

        private void vScrollBar2_ValueChanged(object sender, EventArgs e)
        {

        }

        private void textBox_SelectionChanged(object sender, EventArgs e)
        {
            _coords.Line = 1;
            var total = 0;            
            var offset = textBox.SelectionStart;
            foreach (var line in textBox.Lines)
            {
                var delta = line.Length + 1;                
                if ((total + delta) <= offset)
                {
                    total += delta;
                    ++_coords.Line;
                    _coords.Column = 1;
                }
                else
                {
                    _coords.Column = (uint)(offset - total) + 1;
                    break;
                }
            }
            UpdateStatusBar();
        }

        private void UpdateStatusBar()
        {
            multiverseID.Text = _coords.Multiverse.ToString();
            galaxyID.Text = _coords.Galaxy.ToString();
            worldID.Text = _coords.World.ToString();
            languageID.Text = _coords.Language.ToString();
            branchID.Text = _coords.Branch.ToString();
            volumeID.Text = _coords.Volume.ToString();
            setID.Text = _coords.Set.ToString();
            groupID.Text = _coords.Group.ToString();
            pageID.Text = _coords.Page.ToString();
            status.Text = _coords.EditorSummary();
        }

        private void textBox_KeyUp(object sender, KeyEventArgs e)
        {
            int page_delta = 0;
            int group_delta = 0;
            int set_delta = 0;
            bool arrowShifted = false;

            // Ctrl-S
            if (e.Control && e.KeyCode == Keys.S)
            {
                SaveCurrentFile();
                e.Handled = true;
                return;
            }
            // Ctrl-O
            if (e.Control && e.KeyCode == Keys.O)
            {
                OpenNewFile();
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
            if (!e.Shift && e.KeyCode == Keys.Down && _priorLine == _coords.Line)
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
                if (page_delta > 0) { ++_coords.Page; }
                if (page_delta < 0) { --_coords.Page; }
                if (group_delta > 0) { ++_coords.Group; }
                if (group_delta < 0) { --_coords.Group; }
                if (set_delta > 0) { ++_coords.Set; }
                if (set_delta < 0) { --_coords.Set; }
                if (_coords.Page < 1) { _coords.Page = 1; }
                if (_coords.Group < 1) { _coords.Group = 1; }
                if (_coords.Set < 1) { _coords.Set = 1; }
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
            if (_priorLine != _coords.Line)
            {
                _priorLine = _coords.Line;
            }
            if (_priorColumn != _coords.Column)
            {
                _priorColumn = _coords.Column;
            }
        }

        private void dimensionReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var result = new StringBuilder();
            foreach (var s in _set.Keys)
            {
                foreach (var g in _set[s].Keys)
                {
                    foreach (var p in _set[s][g].Keys)
                    {
                        var count = _set[s][g][p].Length;
                        if (count > 0)
                        {
                            result.AppendLine($"Set: {s}, Group: {g}, Page: {p} has {count} bytes.");
                        }
                    }
                }
            }

            textBox.AppendText(result.ToString());
        }
    }
}