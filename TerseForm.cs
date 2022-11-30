using System.Security.Policy;
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
                if (!_set.ContainsKey(_s))
                {
                    _set[_s] = new();
                }
                return _set[_s];
            }
            set
            {
                _set[_s] = value;
            }
        }
        private SortedDictionary<uint, string> _data
        {
            get
            {
                if (!_group.ContainsKey(_g))
                {
                    _group[_g] = new();
                }
                return _group[_g];
            }
            set
            {
                _group[_g] = value;
            }
        }

        // Editor State
        private uint _priorLine = 1;
        private uint _priorColumn = 1;
        private string _filename = "";

        // Coordinates
        private uint _x = 1; // x = Column
        private uint _n = 1; // n = Line
        private uint _p = 1; // p = Page
        private uint _g = 1; // g = Group
        private uint _s = 1; // s = Set
        private uint _y = 1; // y = Volume
        private uint _h = 1; // h = Branch
        private uint _e = 1; // e = Language
        private uint _w = 1; // w = World
        private uint _i = 1; // i = Galaxy
        private uint _m = 1; // m = Multiverse
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
            _data[_p] = textBox.Text;
        }

        private void loadPage()
        {
            if (!_data.ContainsKey(_p))
            {
                _data[_p] = "";
            }
            textBox.Text = _data[_p];
            _n = 1;
            _priorLine = 1;
            _priorColumn = 1;
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
            _s = 1;
            _set = new();
            foreach (var set in sets)
            {
                var groups = set.Split("\x18\n");
                _g = 1;
                foreach (var group in groups)
                {
                    var pages = group.Split("\x17\n");
                    _p = 1;
                    foreach (var page in pages)
                    {
                        _data[_p] = page;
                        ++_p;
                    }
                    ++_g;
                }
                ++_s;
            }
            jumpToOrigin();            
            loadPage();
        }

        private void jumpToOrigin()
        {
            _p = 1;
            _g = 1;
            _s = 1;
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
                _s = set_index;
                foreach (var group_index in _group.Keys)
                {
                    _g = group_index;
                    foreach (var page in _data)
                    {
                        text += page.Value;
                        text += "\x17\n";
                    }
                    while (group_break_counter++ < _g)
                    {
                        text += "\x18\n";
                    }
                }
                while (set_break_counter++ < _s)
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
            _n = (uint)vScrollBar1.Value;
        }

        private void vScrollBar2_ValueChanged(object sender, EventArgs e)
        {

        }

        private void textBox_SelectionChanged(object sender, EventArgs e)
        {
            _n = 1;
            var total = 0;            
            var offset = textBox.SelectionStart;
            foreach (var line in textBox.Lines)
            {
                var delta = line.Length + 1;                
                if ((total + delta) <= offset)
                {
                    total += delta;
                    ++_n;
                    _x = 1;
                }
                else
                {
                    _x = (uint)(offset - total) + 1;
                    break;
                }
            }
            UpdateStatusBar();
        }

        private void UpdateStatusBar()
        {
            status.Text = $"Line: {_n}  Column: {_x}  Page: {_p}  Group: {_g}  Set: {_s}";
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
            if (!e.Shift && e.KeyCode == Keys.Down && _priorLine == _n)
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
                if (page_delta > 0) { ++_p; }
                if (page_delta < 0) { --_p; }
                if (group_delta > 0) { ++_g; }
                if (group_delta < 0) { --_g; }
                if (set_delta > 0) { ++_s; }
                if (set_delta < 0) { --_s; }
                if (_p < 1) { _p = 1; }
                if (_g < 1) { _g = 1; }
                if (_s < 1) { _s = 1; }
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
            if (_priorLine != _n)
            {
                _priorLine = _n;
            }
            if (_priorColumn != _x)
            {
                _priorColumn = _x;
            }
        }
    }
}