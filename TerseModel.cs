using System.Text;
using System.Windows.Forms;

namespace TerseNotepad
{
    public class TerseModel
    {
        public TerseText Terse = new();
        public Coordinates Coords
        {
            get
            {
                return Terse.Coords;
            }
            set
            {
                Terse.Coords = value;
            }
        }

        public int NodeCount
        {
            get
            {
                return Terse.NodeCount;
            }
        }

        public int WordCount
        {
            get
            {
                return Terse.WordCount;
            }
        }

        public int ScrollWordCount
        {
            get
            {
                return Terse.ScrollWordCount;
            }
        }

        public static readonly char SCROLL_BREAK = '\x17';
        public static readonly char SECTION_BREAK = '\x18';
        public static readonly char CHAPTER_BREAK = '\x19';

        public void Load(string data, TreeView? treeView = null, ScrollBar? sectionScrollbar = null, ScrollBar? chapterScrollbar = null)
        {
            var charStream = data.ToCharArray();
            Terse = new();
            uint chapter_index = 1;
            uint section_index = 1;
            uint scroll_index = 1;
            var stage = new StringBuilder();
            var chapterNode = new TreeNode($"Chapter {chapter_index}")
            {
                Name = $"{chapter_index}-0-0"
            };
            var sectionNode = new TreeNode($"Section {section_index}")
            {
                Name = $"{chapter_index}-{section_index}-0"
            };
            for (int i = 0; i < charStream.Length; ++i)
            {
                var next = charStream[i];
                if (next == SCROLL_BREAK || next == SECTION_BREAK || next == CHAPTER_BREAK)
                {
                    insertScroll(stage, scroll_index, section_index, chapter_index, sectionNode);
                    stage.Clear();
                    ++scroll_index;
                }

                if (next == CHAPTER_BREAK)
                {
                    ++chapter_index;
                    section_index = 1;
                    scroll_index = 1;
                    if (chapterNode.Nodes.Count > 0)
                    {
                        treeView?.Nodes.Add(chapterNode);
                        Terse.SetChapterNode(chapterNode, chapter_index);
                        chapterNode = new TreeNode($"Chapter {chapter_index}")
                        {
                            Name = $"{chapter_index}-0-0"
                        };
                    }
                    if (chapterScrollbar != null)
                    {
                        chapterScrollbar.Maximum = (int)chapter_index;
                    }
                    continue;
                }
                if (next == SECTION_BREAK)
                {
                    ++section_index;
                    scroll_index = 1;
                    if (sectionNode.Nodes.Count > 0)
                    {
                        chapterNode.Nodes.Add(sectionNode);
                        Terse.SetSectionNode(sectionNode, chapter_index, section_index);
                        sectionNode = new TreeNode($"Section {section_index}")
                        {
                            Name = $"{chapter_index}-{section_index}-0"
                        };
                    }

                    if (sectionScrollbar != null)
                    {
                        sectionScrollbar.Maximum = (int)section_index;
                    }
                    continue;
                }

                if (next != CHAPTER_BREAK && next != SECTION_BREAK && next != SCROLL_BREAK)
                {
                    stage.Append(next);
                }
            }

            if (stage.Length > 0)
            {
                insertScroll(stage, scroll_index, section_index, chapter_index, sectionNode);
                stage.Clear();
                if (sectionNode.Nodes.Count > 0)
                {
                    chapterNode.Nodes.Add(sectionNode);
                    Terse.SetSectionNode(sectionNode, chapter_index, section_index);                   
                }
                if (chapterNode.Nodes.Count > 0)
                {
                    treeView?.Nodes.Add(chapterNode);
                    Terse.SetChapterNode(chapterNode, chapter_index);
                }
            }
        }

        private void insertScroll(StringBuilder stage, uint scroll_index, uint section_index, uint chapter_index, TreeNode? node)
        {
            if (stage.Length == 0) { return; }
            var scroll = stage.ToString().Trim();
            if (scroll.Length > 0)
            {
                Terse.Coords.Scroll = scroll_index;
                Terse.Coords.Section = section_index;
                Terse.Coords.Chapter = chapter_index;
                var key = Terse.Coords.ToString();
                var line = GetScrollSummary(Terse.Coords, scroll);
                var scrollNode = node?.Nodes.Add(key, line);
                Terse.setScroll(scroll, scrollNode);
            }
        }

        public static string GetScrollSummary(Coordinates coords, string scroll)
        {
            var firstLine = scroll.Split("\n")[0];
            var line = firstLine.Length > 40 ? firstLine[..40] : firstLine;
            if (line.Length > 0)
            {
                return line;
            }
            return coords.GetNodeSummary();
        }

        public string Serialize()
        {
            var collector = new StringBuilder();
            uint chapter_break_counter = 0;
            foreach (var chapter_index in Terse.Chapter.Keys)
            {
                while (++chapter_break_counter < chapter_index)
                {
                    collector.Append(CHAPTER_BREAK);
                }
                uint section_break_counter = 0;
                foreach (var section_index in Terse.Chapter[chapter_index].Children.Keys)
                {
                    while (++section_break_counter < section_index)
                    {
                        collector.Append(SECTION_BREAK);
                    }
                    uint scroll_break_counter = 0;
                    foreach (var scroll_index in Terse.Chapter[chapter_index].Children[section_index].Children.Keys)
                    {
                        while (++scroll_break_counter < scroll_index)
                        {
                            collector.Append(SCROLL_BREAK);
                        }
                        collector.Append(Terse.Chapter[chapter_index].Children[section_index].Children[scroll_index].Text);
                        collector.Append(SCROLL_BREAK);
                    }
                    collector.Append(SECTION_BREAK);
                }
                collector.Append(CHAPTER_BREAK);
            }

            return collector.ToString();
        }
    }
}
