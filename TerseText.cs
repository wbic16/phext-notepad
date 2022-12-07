namespace TerseNotepad
{
    public class TextNode
    {
        public TreeNode Node { get; set; } = new();
        public string Text { get; set; } = string.Empty;
    };
    public class ScrollNode
    {
        public TreeNode Node { get; set; } = new();
        public SortedDictionary<uint, TextNode> Children { get; set; } = new();
    };
    public class SectionNode
    {
        public TreeNode Node { get; set; } = new();
        public SortedDictionary<uint, ScrollNode> Children { get; set; } = new();
    };
    public class ChapterNode : SortedDictionary<uint, SectionNode>
    {
    };

    public class TerseText
    {
        public Coordinates Coords = new();
        public ChapterNode Chapter = new();
        public int NodeCount { get; private set; } = 0;
        public int WordCount { get; private set; } = 0;

        public int ScrollWordCount
        {
            get
            {
                return GetWordCount(getScroll());
            }
        }
        public SectionNode Section
        {
            get
            {
                if (!Chapter.ContainsKey(Coords.Chapter))
                {
                    Chapter[Coords.Chapter] = new();
                }
                return Chapter[Coords.Chapter];
            }
            set
            {
                Chapter[Coords.Chapter] = value;
            }
        }
        public ScrollNode Scroll
        {
            get
            {
                if (!Section.Children.ContainsKey(Coords.Section))
                {
                    Section.Children[Coords.Section] = new();
                }
                return Section.Children[Coords.Section];
            }
            set
            {
                Section.Children[Coords.Section] = value;
            }
        }

        public static int GetWordCount(string text)
        {
            var array = text.ToCharArray();
            int words = 0;
            bool breaking = true;
            for (int i = 0; i < array.Length; ++i)
            {
                var ch = array[i];
                if (ch == ' ' || ch == '\n' || ch == '\x17' || ch == '\x18' || ch == '\x19')
                {
                    breaking = true;
                }
                if (ch >= 0x20 && breaking)
                {
                    breaking = false;
                    ++words;
                }
            }
            return words;
        }

        public void setScroll(string text, TreeNode? node = null)
        {
            if (text.Length > 0)
            {
                if (!Scroll.Children.ContainsKey(Coords.Scroll))
                {
                    Scroll.Children[Coords.Scroll] = new();
                    ++NodeCount;
                }
                var priorText = Scroll.Children[Coords.Scroll].Text;
                var priorCount = GetWordCount(priorText);
                Scroll.Children[Coords.Scroll].Text = text;
                var count = GetWordCount(text);
                WordCount += (count - priorCount);
                if (node != null)
                {
                    Scroll.Children[Coords.Scroll].Node = node;
                }
            }
            // note: the key checks here optimize performance on sparse files
            if (text.Length == 0 &&
                Chapter.ContainsKey(Coords.Chapter) &&
                Section.Children.ContainsKey(Coords.Section) &&
                Scroll.Children.ContainsKey(Coords.Scroll))
            {
                Scroll.Children.Remove(Coords.Scroll);
                --NodeCount;
            }
        }

        public void processDelta(int chapter_delta, int section_delta, int scroll_delta)
        {
            if (scroll_delta > 0) { ++Coords.Scroll; }
            if (scroll_delta < 0) { --Coords.Scroll; }
            if (section_delta > 0) { ++Coords.Section; }
            if (section_delta < 0) { --Coords.Section; }
            if (chapter_delta > 0) { ++Coords.Chapter; }
            if (chapter_delta < 0) { --Coords.Chapter; }
            if (Coords.Scroll < 1) { Coords.Scroll = 1; }
            if (Coords.Section < 1) { Coords.Section = 1; }
            if (Coords.Chapter < 1) { Coords.Chapter = 1; }
        }

        public string getScroll()
        {
            return Scroll.Children.ContainsKey(Coords.Scroll) ? Scroll.Children[Coords.Scroll].Text : "";
        }

        public string EditorSummary(string action = "")
        {
            return Coords.EditorSummary(action);
        }

        public void SetSectionNode(TreeNode sectionNode, uint chapter_index, uint section_index)
        {
            if (!Chapter.ContainsKey(chapter_index))
            {
                Chapter[chapter_index] = new();
            }
            if (!Chapter[chapter_index].Children.ContainsKey(section_index))
            {
                Chapter[chapter_index].Children[section_index] = new();
            }
            Chapter[chapter_index].Children[section_index].Node = sectionNode;
        }

        public void SetChapterNode(TreeNode chapterNode, uint chapter_index)
        {
            if (!Chapter.ContainsKey(chapter_index))
            {
                Chapter[chapter_index] = new();
            }
            Chapter[chapter_index].Node = chapterNode;
        }
    }
}