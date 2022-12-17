using System.Text;

namespace TerseNotepad
{
    public class TerseModel
    {
        public TerseText Terse = new();
        private Dictionary<string, TreeNode> _nodeCache = new();
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

        public static readonly char WORD_BREAK = '\x20';
        public static readonly char LINE_BREAK = '\n';
        public static readonly char SCROLL_BREAK = '\x17';
        public static readonly char SECTION_BREAK = '\x18';
        public static readonly char CHAPTER_BREAK = '\x19';
        public static readonly char BOOK_BREAK = '\x1A';
        public static readonly char VOLUME_BREAK = '\x1C';
        public static readonly char COLLECTION_BREAK = '\x1D';
        public static readonly char SERIES_BREAK = '\x1E';
        public static readonly char SHELF_BREAK = '\x1F';
        public static readonly char LIBRARY_BREAK = '\x01';

        public void Load(string data, TreeView? treeView = null, ScrollBar? sectionScrollbar = null, ScrollBar? chapterScrollbar = null)
        {
            var charStream = data.ToCharArray();
            Terse = new();
            Coordinates local = new();
            var stage = new StringBuilder();
            /*var chapterNode = new TreeNode($"Chapter {local.Chapter}")
            {
                Name = $"{local.Chapter}-0-0"
            };
            var sectionNode = new TreeNode($"Section {local.Section}")
            {
                Name = $"{local.Chapter}-{local.Section}-0"
            };*/
            for (int i = 0; i < charStream.Length; ++i)
            {
                var next = charStream[i];
                if (next == SCROLL_BREAK || next == SECTION_BREAK || next == CHAPTER_BREAK)
                {
                    insertScroll(stage, local, null); // sectionNode);
                    stage.Clear();
                }

                if (next == SCROLL_BREAK)
                {
                    ++local.Scroll;
                    continue;
                }

                if (next == SECTION_BREAK)
                {
                    ++local.Section;
                    local.Scroll = 1;
                    /*if (sectionNode.Nodes.Count > 0)
                    {
                        chapterNode.Nodes.Add(sectionNode);
                        Terse.SetSectionNode(sectionNode, local.Chapter, local.Section);
                        _nodeCache[sectionNode.Name] = sectionNode;
                    }
                    sectionNode = new TreeNode($"Section {local.Section}")
                    {
                        Name = $"{local.Chapter}-{local.Section}-0"
                    };
                    _nodeCache[sectionNode.Name] = sectionNode;

                    if (sectionScrollbar != null)
                    {
                        sectionScrollbar.Value = sectionScrollbar.Minimum;
                        sectionScrollbar.Maximum = (int)local.Section + 1;
                    }*/
                    continue;
                }

                if (next == CHAPTER_BREAK)
                {
                    ++local.Chapter;
                    local.Section = 1;
                    local.Scroll = 1;
                    /*if (chapterNode != null && chapterNode.Nodes.Count > 0)
                    {
                        treeView?.Nodes.Add(chapterNode);
                        Terse.SetChapterNode(chapterNode, local.Chapter);
                        _nodeCache[chapterNode.Name] = chapterNode;
                    }
                    chapterNode = new TreeNode($"Chapter {local.Chapter}")
                    {
                        Name = $"{local.Chapter}-0-0"
                    };
                    _nodeCache[chapterNode.Name] = chapterNode;
                    if (chapterScrollbar != null)
                    {
                        chapterScrollbar.Value = chapterScrollbar.Minimum;
                        chapterScrollbar.Maximum = (int)local.Chapter + 1;
                    }*/
                    continue;
                }

                stage.Append(next);
            }

            if (stage.Length > 0)
            {
                insertScroll(stage, local, null); // sectionNode);
                stage.Clear();
            }
            /*if (sectionNode.Nodes.Count > 0)
            {
                chapterNode.Nodes.Add(sectionNode);
                Terse.SetSectionNode(sectionNode, local.Chapter, local.Section);
                _nodeCache[sectionNode.Name] = sectionNode;
            }
            if (chapterNode.Nodes.Count > 0)
            {
                treeView?.Nodes.Add(chapterNode);
                Terse.SetChapterNode(chapterNode, local.Chapter);
                _nodeCache[chapterNode.Name] = chapterNode;
            }*/
        }

        private void insertScroll(StringBuilder stage, Coordinates local, TreeNode? node)
        {
            if (stage.Length == 0) { return; }
            var scroll = stage.ToString();
            if (scroll.Length > 0)
            {
                var key = local.ToString();
                var line = GetScrollSummary(local, scroll);
                var scrollNode = node?.Nodes.Add(key, key + "! " + line);
                if (scrollNode != null)
                {
                    _nodeCache[local.ToString()] = scrollNode;
                }
                Terse.setScroll(scroll, scrollNode);
                Terse.Coords = local;
            }
        }

        public static string GetScrollSummary(Coordinates coords, string scroll)
        {
            var firstLine = scroll.TrimStart().Split("\n")[0];
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
                Coordinates local = new();
                local.Chapter = chapter_index;
                chapter_break_counter = Serialize_Chapters(collector, Terse.Chapter, chapter_break_counter, local);
            }

            return collector.ToString();
        }

        private uint Serialize_Chapters(StringBuilder collector, SortedDictionary<short, ChapterNode> chapters, uint chapter_break_counter, Coordinates local)
        {
            while (++chapter_break_counter < local.Chapter)
            {
                collector.Append(CHAPTER_BREAK);
            }
            uint section_break_counter = 0;
            foreach (var section_index in chapters[local.Chapter].Children.Keys)
            {
                local.Section = section_index;
                section_break_counter = Serialize_Sections(collector, chapters[local.Chapter].Children, section_break_counter, local);
            }

            return chapter_break_counter;
        }

        private uint Serialize_Sections(StringBuilder collector, SortedDictionary<short, SectionNode> sections, uint section_break_counter, Coordinates local)
        {
            while (++section_break_counter < local.Section)
            {
                collector.Append(SECTION_BREAK);
            }
            uint scroll_break_counter = 0;
            foreach (var scroll_index in sections[local.Section].Children.Keys)
            {
                local.Scroll = scroll_index;
                scroll_break_counter = Serialize_Scrolls(collector, sections[local.Section].Children, scroll_break_counter, local);
            }

            return section_break_counter;
        }

        private uint Serialize_Scrolls(StringBuilder collector, SortedDictionary<short, ScrollNode> scrolls, uint scroll_break_counter, Coordinates local)
        {
            while (++scroll_break_counter < local.Scroll)
            {
                collector.Append(SCROLL_BREAK);
            }
            collector.Append(scrolls[local.Scroll].Text);

            return scroll_break_counter;
        }

        public static bool IsBreakCharacter(char ch)
        {
            return ch == WORD_BREAK || ch == LINE_BREAK ||
                   ch == SCROLL_BREAK || ch == SECTION_BREAK || ch == CHAPTER_BREAK;
        }

        public static bool IsTextCharacter(char ch)
        {
            return ch >= WORD_BREAK;
        }

        public TreeNode? Find(Coordinates coordinates)
        {
            string test = coordinates.ToString();
            if (_nodeCache.ContainsKey(test))
            {
                return _nodeCache[test];
            }

            return null;
        }

        public TreeNode CreateNode(TreeNode sectionNode, string line)
        {
            var key = Coords.ToString();
            var scrollNode = sectionNode.Nodes.Add(key, key + ": " + line);
            Terse.Chapter[Coords.Chapter].Children[Coords.Section].Children[Coords.Scroll].Node = scrollNode;
            _nodeCache[key] = scrollNode;
            return scrollNode;
        }
    }
}
