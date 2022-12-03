using System;
using static System.Collections.Specialized.BitVector32;

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
        public ChapterNode Chapter = new();
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
        public class Coordinates
        {
            public uint Column { get; set; } = 1;
            public uint Line { get; set; } = 1;
            public uint Scroll { get; set; } = 1;
            public uint Section { get; set; } = 1;
            public uint Chapter { get; set; } = 1;
            public uint Book { get; set; } = 1;
            public uint Volume { get; set; } = 1;
            public uint Collection { get; set; } = 1;
            public uint Series { get; set; } = 1;
            public uint Shelf { get; set; } = 1;
            public uint Library { get; set; } = 1;            

            public override string ToString()
            {
                return $"{Scroll}-{Section}-{Chapter}";
            }

            public string EditorSummary(string action = "")
            {
                var result = $"Ln: {Line}, Col: {Column}";
                if (action.Length > 0)
                {
                    result += $" {action}";
                }
                return result;
            }

            public void Reset()
            {
                Column = 1;
                Line = 1;
                Scroll = 1;
                Section = 1;
                Chapter = 1;
                Book = 1;
                Volume = 1;
                Collection = 1;
                Series = 1;
                Shelf = 1;
                Library = 1;
            }

            public void Load(string coordinates)
            {
                // TODO: Support Book through Library
                var parts = coordinates.Split('-', 3);
                if (parts.Length != 3)
                {
                    return;
                }
                Scroll = uint.Parse(parts[0]);
                Section = uint.Parse(parts[1]);
                Chapter = uint.Parse(parts[2]);
            }
        }
        public Coordinates Coords = new Coordinates();

        public void setScroll(string text, TreeNode? node = null)
        {
            if (text.Length > 0)
            {
                if (!Scroll.Children.ContainsKey(Coords.Scroll))
                {
                    Scroll.Children[Coords.Scroll] = new();
                }
                Scroll.Children[Coords.Scroll].Text = text;
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

        internal void setSectionNode(TreeNode sectionNode, uint chapter_index, uint section_index)
        {
            Chapter[chapter_index].Children[section_index].Node = sectionNode;
        }

        internal void setChapterNode(TreeNode chapterNode, uint chapter_index)
        {
            Chapter[chapter_index].Node = chapterNode;
        }
    }
}