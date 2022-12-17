namespace TerseNotepad
{
    public class ScrollNode
    {
        public TreeNode Node { get; set; } = new();
        public string Text { get; set; } = string.Empty;
    };
    public class SectionNode
    {
        public TreeNode Node { get; set; } = new();
        public SortedDictionary<short, ScrollNode> Scroll { get; set; } = new();
    };
    public class ChapterNode
    {
        public TreeNode Node { get; set; } = new();
        public SortedDictionary<short, SectionNode> Section { get; set; } = new();
    };
    public class BookNode
    {
        public TreeNode Node { get; set; } = new();
        public SortedDictionary<short, ChapterNode> Chapter { get; set; } = new();
    };

    public class VolumeNode
    {
        public TreeNode Node { get; set; } = new();
        public SortedDictionary<short, BookNode> Book { get; set; } = new();
    };

    public class CollectionNode
    {
        public TreeNode Node { get; set; } = new();
        public SortedDictionary<short, VolumeNode> Volume { get; set; } = new();
    };

    public class SeriesNode
    {
        public TreeNode Node { get; set; } = new();
        public SortedDictionary<short, CollectionNode> Collection { get; set; } = new();
    };

    public class ShelfNode
    {
        public TreeNode Node { get; set; } = new();
        public SortedDictionary<short, SeriesNode> Series { get; set; } = new();
    };

    public class LibraryNode
    {
        public TreeNode Node { get; set; } = new();
        public SortedDictionary<short, ShelfNode> Shelf { get; set; } = new();
    };

    public class RootNode
    {
        public SortedDictionary<short, LibraryNode> Library { get; set; } = new();
    };

    public class TerseText
    {
        public Coordinates Coords = new();
        public RootNode Root = new();
        public int LeafCount { get; private set; } = 0;
        public int WordCount { get; private set; } = 0;

        public int ScrollWordCount
        {
            get
            {
                return GetWordCount(getScroll());
            }
        }
        public LibraryNode Library
        {
            get
            {
                if (!Root.Library.ContainsKey(Coords.Library))
                {
                    Root.Library[Coords.Library] = new();
                }
                return Root.Library[Coords.Library];
            }
            set
            {
                Root.Library[Coords.Library] = value;
            }
        }

        public ShelfNode Shelf
        {
            get
            {
                if (!Library.Shelf.ContainsKey(Coords.Shelf))
                {
                    Library.Shelf[Coords.Shelf] = new();
                }
                return Library.Shelf[Coords.Shelf];
            }
            set
            {
                Library.Shelf[Coords.Shelf] = value;
            }
        }

        public SeriesNode Series
        {
            get
            {
                if (!Shelf.Series.ContainsKey(Coords.Series))
                {
                    Shelf.Series[Coords.Series] = new();
                }
                return Shelf.Series[Coords.Series];
            }
            set
            {
                Shelf.Series[Coords.Series] = value;
            }
        }

        public CollectionNode Collection
        {
            get
            {
                if (!Series.Collection.ContainsKey(Coords.Collection))
                {
                    Series.Collection[Coords.Collection] = new();
                }
                return Series.Collection[Coords.Collection];
            }
            set
            {
                Series.Collection[Coords.Collection] = value;
            }
        }

        public VolumeNode Volume
        {
            get
            {
                if (!Collection.Volume.ContainsKey(Coords.Volume))
                {
                    Collection.Volume[Coords.Volume] = new();
                }
                return Collection.Volume[Coords.Volume];
            }
            set
            {
                Collection.Volume[Coords.Volume] = value;
            }
        }

        public BookNode Book
        {
            get
            {
                if (!Volume.Book.ContainsKey(Coords.Book))
                {
                    Volume.Book[Coords.Book] = new();
                }
                return Volume.Book[Coords.Book];
            }
            set
            {
                Volume.Book[Coords.Book] = value;
            }
        }

        public ChapterNode Chapter
        {
            get
            {
                if (!Book.Chapter.ContainsKey(Coords.Chapter))
                {
                    Book.Chapter[Coords.Chapter] = new();
                }
                return Book.Chapter[Coords.Chapter];
            }
            set
            {
                Book.Chapter[Coords.Chapter] = value;
            }
        }

        public SectionNode Section
        {
            get
            {
                if (!Chapter.Section.ContainsKey(Coords.Section))
                {
                    Chapter.Section[Coords.Section] = new();
                }
                return Chapter.Section[Coords.Section];
            }
            set
            {
                Chapter.Section[Coords.Section] = value;
            }
        }
        public ScrollNode Scroll
        {
            get
            {
                if (!Section.Scroll.ContainsKey(Coords.Scroll))
                {
                    Section.Scroll[Coords.Scroll] = new();
                }
                return Section.Scroll[Coords.Scroll];
            }
            set
            {
                Section.Scroll[Coords.Scroll] = value;
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
                if (TerseModel.IsBreakCharacter(ch))
                {
                    breaking = true;
                }
                if (TerseModel.IsTextCharacter(ch) && breaking)
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
                var priorText = Scroll.Text;
                var priorCount = GetWordCount(priorText);
                Scroll.Text = text;
                var count = GetWordCount(text);
                WordCount += (count - priorCount);
                if (node != null)
                {
                    Scroll.Node = node;
                }
            }
            // note: the key checks here optimize performance on sparse files
            if (text.Length == 0 &&
                Root.Library.ContainsKey(Coords.Library) &&
                Library.Shelf.ContainsKey(Coords.Shelf) &&
                Shelf.Series.ContainsKey(Coords.Series) &&
                Series.Collection.ContainsKey(Coords.Collection) &&
                Collection.Volume.ContainsKey(Coords.Volume) &&
                Volume.Book.ContainsKey(Coords.Book) &&
                Book.Chapter.ContainsKey(Coords.Chapter) &&
                Chapter.Section.ContainsKey(Coords.Section) &&
                Section.Scroll.ContainsKey(Coords.Scroll))
            {
                Section.Scroll.Remove(Coords.Scroll);
                if (Section.Scroll.Count == 0)
                {
                    Chapter.Section.Remove(Coords.Section);
                }
                if (Chapter.Section.Count == 0)
                {
                    Book.Chapter.Remove(Coords.Chapter);
                }
                if (Book.Chapter.Count == 0)
                {
                    Volume.Book.Remove(Coords.Book);
                }
                if (Volume.Book.Count == 0)
                {
                    Collection.Volume.Remove(Coords.Volume);
                }
                if (Collection.Volume.Count == 0)
                {
                    Series.Collection.Remove(Coords.Collection);
                }
                if (Series.Collection.Count == 0)
                {
                    Shelf.Series.Remove(Coords.Series);
                }
                if (Shelf.Series.Count == 0)
                {
                    Library.Shelf.Remove(Coords.Shelf);
                }
                if (Root.Library.Count == 0)
                {
                    Root.Library.Remove(Coords.Library);
                }
                --LeafCount;
            }
        }

        public void processDelta(Coordinates delta)
        {
            if (delta.Library != 0)
            {
                Coords.Library += delta.Library;
                if (Coords.Library < 1) { Coords.Library = 1; }
                Coords.Shelf = 1;
                Coords.Series = 1;
                Coords.Collection = 1;
                Coords.Volume = 1;
                Coords.Book = 1;
                Coords.Chapter = 1;
                Coords.Section = 1;
                Coords.Scroll = 1;
                return;
            }
            if (delta.Shelf != 0)
            {
                Coords.Shelf += delta.Shelf;
                if (Coords.Shelf < 1) { Coords.Shelf = 1; }
                Coords.Series = 1;
                Coords.Collection = 1;
                Coords.Volume = 1;
                Coords.Book = 1;
                Coords.Chapter = 1;
                Coords.Section = 1;
                Coords.Scroll = 1;
                return;
            }
            if (delta.Series != 0)
            {
                Coords.Series += delta.Series;
                if (Coords.Series < 1) { Coords.Series = 1; }
                Coords.Collection = 1;
                Coords.Volume = 1;
                Coords.Book = 1;
                Coords.Chapter = 1;
                Coords.Section = 1;
                Coords.Scroll = 1;
                return;
            }
            if (delta.Collection != 0)
            {
                Coords.Collection += delta.Collection;
                if (Coords.Collection < 1) { Coords.Collection = 1; }
                Coords.Volume = 1;
                Coords.Book = 1;
                Coords.Chapter = 1;
                Coords.Section = 1;
                Coords.Scroll = 1;
                return;
            }
            if (delta.Volume != 0)
            {
                Coords.Volume += delta.Volume;
                if (Coords.Volume < 1) { Coords.Volume = 1; }
                Coords.Book = 1;
                Coords.Chapter = 1;
                Coords.Section = 1;
                Coords.Scroll = 1;
                return;
            }
            if (delta.Book != 0)
            {
                Coords.Book += delta.Book;
                if (Coords.Book < 1) { Coords.Book = 1; }
                Coords.Chapter = 1;
                Coords.Section = 1;
                Coords.Scroll = 1;
                return;
            }

            if (delta.Chapter != 0)
            {
                Coords.Chapter += delta.Chapter;
                if (Coords.Chapter < 1) { Coords.Chapter = 1; }
                Coords.Section = 1;
                Coords.Scroll = 1;
                return;
            }

            if (delta.Section != 0)
            {
                Coords.Section += delta.Section;
                Coords.Scroll = 1;
                return;
            }

            Coords.Scroll += delta.Scroll;
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