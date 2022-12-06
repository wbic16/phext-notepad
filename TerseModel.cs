using System.Text;

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

        public static readonly string SCROLL_BREAK = "\x17\n";
        public static readonly string SECTION_BREAK = "\x18\n";
        public static readonly string CHAPTER_BREAK = "\x19\n";

        public void Load(string data, TreeView? treeView = null, ScrollBar? sectionScrollbar = null, ScrollBar? chapterScrollbar = null)
        {
            var dimensionStream = data.Split(CHAPTER_BREAK);
            Terse.Chapter = new();
            uint chapter_index = 1;
            foreach (var chapter in dimensionStream)
            {
                uint section_index = 1;
                var sections = chapter.Split(SECTION_BREAK);
                var chapterNode = new TreeNode($"Chapter {chapter_index}")
                {
                    Name = $"{chapter_index}-0-0"
                };
                foreach (var section in sections)
                {
                    uint scroll_index = 1;
                    var scrolls = section.Split(SCROLL_BREAK);
                    var sectionNode = new TreeNode($"Section {section_index}")
                    {
                        Name = $"{chapter_index}-{section_index}-0"
                    };
                    foreach (var scroll in scrolls)
                    {
                        Terse.Coords.Scroll = scroll_index;
                        Terse.Coords.Section = section_index;
                        Terse.Coords.Chapter = chapter_index;
                        if (scroll.Length > 0)
                        {
                            var key = Terse.Coords.ToString();
                            var line = GetScrollSummary(Terse.Coords, scroll);
                            var scrollNode = sectionNode.Nodes.Add(key, line);
                            Terse.setScroll(scroll, scrollNode);
                        }
                        ++scroll_index;
                    }
                    if (sectionNode.Nodes.Count > 0)
                    {
                        chapterNode.Nodes.Add(sectionNode);
                        Terse.setSectionNode(sectionNode, chapter_index, section_index);
                    }
                    ++section_index;
                }
                if (sectionScrollbar != null)
                {
                    sectionScrollbar.Maximum = (int)section_index;
                }
                if (chapterNode.Nodes.Count > 0)
                {
                    treeView?.Nodes.Add(chapterNode);
                    Terse.setChapterNode(chapterNode, chapter_index);
                }
                ++chapter_index;
            }
            if (chapterScrollbar != null)
            {
                chapterScrollbar.Maximum = (int)chapter_index;
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
