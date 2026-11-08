using System.IO.Hashing;
using System.Text;
using static PhextNotepad.Coordinates;
using static System.Collections.Specialized.BitVector32;

namespace PhextNotepad
{
    public class ScrollNode
    {
        public string Text { get; set; } = string.Empty;
    };
    public interface IPhextNode<T, S>
        where T : notnull
    {
        public SortedDictionary<T, S> Children { get; set; }
        public char Delimiter { get; }
    };
    public class SectionNode : IPhextNode<ScrollIndex, ScrollNode>
    {
        public SortedDictionary<ScrollIndex, ScrollNode> Scroll { get; set; } = new();
        public SortedDictionary<ScrollIndex, ScrollNode> Children
        {
            get { return Scroll; }
            set { Scroll = value; }
        }
        public char Delimiter { get { return PhextModel.SECTION_BREAK; } }
    };
    public class ChapterNode : IPhextNode<SectionIndex, SectionNode>
    {
        public SortedDictionary<SectionIndex, SectionNode> Section { get; set; } = new();
        public SortedDictionary<SectionIndex, SectionNode> Children
        {
            get { return Section; }
            set { Section = value; }
        }
        public char Delimiter { get { return PhextModel.CHAPTER_BREAK; } }
    };
    public class BookNode : IPhextNode<ChapterIndex, ChapterNode>
    {
        public SortedDictionary<ChapterIndex, ChapterNode> Chapter { get; set; } = new();
        public SortedDictionary<ChapterIndex, ChapterNode> Children
        {
            get { return Chapter; }
            set { Chapter = value; }
        }
        public char Delimiter { get { return PhextModel.BOOK_BREAK; } }
    };

    public class VolumeNode : IPhextNode<BookIndex, BookNode>
    {
        public SortedDictionary<BookIndex, BookNode> Book { get; set; } = new();
        public SortedDictionary<BookIndex, BookNode> Children
        {
            get { return Book; }
            set { Book = value; }
        }
        public char Delimiter { get { return PhextModel.VOLUME_BREAK; } }
    };

    public class CollectionNode : IPhextNode<VolumeIndex, VolumeNode>
    {
        public SortedDictionary<VolumeIndex, VolumeNode> Volume { get; set; } = new();
        public SortedDictionary<VolumeIndex, VolumeNode> Children
        {
            get { return Volume; }
            set { Volume = value; }
        }
        public char Delimiter { get { return PhextModel.COLLECTION_BREAK; } }
    };

    public class SeriesNode : IPhextNode<CollectionIndex, CollectionNode>
    {
        public SortedDictionary<CollectionIndex, CollectionNode> Collection { get; set; } = new();
        public SortedDictionary<CollectionIndex, CollectionNode> Children
        {
            get { return Collection; }
            set { Collection = value; }
        }
        public char Delimiter { get { return PhextModel.SERIES_BREAK; } }
    };

    public class ShelfNode : IPhextNode<SeriesIndex, SeriesNode>
    {
        public SortedDictionary<SeriesIndex, SeriesNode> Series { get; set; } = new();
        public SortedDictionary<SeriesIndex, SeriesNode> Children
        {
            get { return Series; }
            set { Series = value; }
        }
        public char Delimiter { get { return PhextModel.SHELF_BREAK; } }
    };

    public class LibraryNode : IPhextNode<ShelfIndex, ShelfNode>
    {
        public SortedDictionary<ShelfIndex, ShelfNode> Shelf { get; set; } = new();
        public SortedDictionary<ShelfIndex, ShelfNode> Children
        {
            get { return Shelf; }
            set { Shelf = value; }
        }
        public char Delimiter { get { return PhextModel.LIBRARY_BREAK; } }
    };

    public class RootNode
    {
        public SortedDictionary<LibraryIndex, LibraryNode> Library { get; set; } = new();
    };

    public class PhextText
    {
        public Coordinates Coords = new();
        public RootNode Root = new();
        public int LeafCount { get; private set; } = 0;
        public int WordCount { get; private set; } = 0;
        private XxHash128 _hasher = new XxHash128();

        public string HierarchicalChecksum
        {
            get
            {
                string result = "";
                Coordinates walker = new();
                foreach (var lib_key in Root.Library.Keys)
                {
                    var library = Root.Library[lib_key];
                    while (walker.Library < lib_key)
                    {
                        walker.LibraryBreak();
                    }
                    foreach (var shelf_key in library.Shelf.Keys)
                    {
                        while (walker.Shelf < shelf_key)
                        {
                            walker.ShelfBreak();
                        }
                        var shelf = library.Shelf[shelf_key];
                        foreach (var series_key in shelf.Series.Keys)
                        {
                            while (walker.Series < series_key)
                            {
                                walker.SeriesBreak();
                            }
                            var series = shelf.Series[series_key];
                            foreach (var collection_key in series.Collection.Keys)
                            {
                                while (walker.Collection < collection_key)
                                {
                                    walker.CollectionBreak();
                                }
                                var collection = series.Collection[collection_key];
                                foreach (var volume_key in collection.Volume.Keys)
                                {
                                    while (walker.Volume < volume_key)
                                    {
                                        walker.VolumeBreak();
                                    }
                                    var volume = collection.Volume[volume_key];
                                    foreach (var book_key in volume.Book.Keys)
                                    {
                                        while (walker.Book < book_key)
                                        {
                                            walker.BookBreak();
                                        }
                                        var book = volume.Book[book_key];
                                        foreach (var chapter_key in book.Chapter.Keys)
                                        {
                                            while (walker.Chapter < chapter_key)
                                            {
                                                walker.ChapterBreak();
                                            }
                                            var chapter = book.Chapter[chapter_key];
                                            foreach (var section_key in chapter.Section.Keys)
                                            {
                                                while (walker.Section < section_key)
                                                {
                                                    walker.SectionBreak();
                                                }
                                                var section = chapter.Section[section_key];
                                                foreach (var scroll_key in section.Scroll.Keys)
                                                {
                                                    while (walker.Scroll < scroll_key)
                                                    {
                                                        walker.ScrollBreak();
                                                    }
                                                    var scroll = section.Scroll[scroll_key];
                                                    if (scroll.Text.Length == 0)
                                                    {
                                                        continue;
                                                    }
                                                    _hasher.Reset();
                                                    _hasher.Append(Encoding.UTF8.GetBytes(scroll.Text));

                                                    result += walker.ToString() + ": ";
                                                    result += _hasher.GetCurrentHashAsUInt128().ToString("x32");
                                                    result += "\n";
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return result;
            }
        }
        // Maintained incrementally in setScroll (the only place a scroll's
        // text ever changes), the same way WordCount is. This getter used
        // to walk all nine levels of the tree summing every scroll on
        // every access — and UpdateUI reads it on every cursor move and
        // navigation, so working in a large phext paid a full-document
        // traversal per keystroke-adjacent event.
        private int _byteCount = 0;
        public int ByteCount {
            get
            {
                return _byteCount;
            }
        }

        public int ScrollWordCount
        {
            get
            {
                return GetWordCount(getScroll());
            }
        }
        // The accessors below resolve the node chain for the current
        // Coords, creating any missing level (the behavior they have
        // always had). Each accessor used to call the one above it, so
        // resolving Scroll re-walked every dictionary above it — 36
        // dictionary walks per access, and setScroll accesses Scroll
        // twice per call. On a several-hundred-thousand-scroll
        // document that access pattern, not the parse, was the load
        // time (~28 us per setScroll, most of it re-walking). The
        // resolvers walk the chain iteratively: one dictionary walk
        // per level.
        private LibraryNode ResolveLibrary()
        {
            if (!Root.Library.TryGetValue(Coords.Library, out var library))
            {
                library = new();
                Root.Library[Coords.Library] = library;
            }
            return library;
        }

        private ShelfNode ResolveShelf()
        {
            var library = ResolveLibrary();
            if (!library.Shelf.TryGetValue(Coords.Shelf, out var shelf))
            {
                shelf = new();
                library.Shelf[Coords.Shelf] = shelf;
            }
            return shelf;
        }

        private SeriesNode ResolveSeries()
        {
            var shelf = ResolveShelf();
            if (!shelf.Series.TryGetValue(Coords.Series, out var series))
            {
                series = new();
                shelf.Series[Coords.Series] = series;
            }
            return series;
        }

        private CollectionNode ResolveCollection()
        {
            var series = ResolveSeries();
            if (!series.Collection.TryGetValue(Coords.Collection, out var collection))
            {
                collection = new();
                series.Collection[Coords.Collection] = collection;
            }
            return collection;
        }

        private VolumeNode ResolveVolume()
        {
            var collection = ResolveCollection();
            if (!collection.Volume.TryGetValue(Coords.Volume, out var volume))
            {
                volume = new();
                collection.Volume[Coords.Volume] = volume;
            }
            return volume;
        }

        private BookNode ResolveBook()
        {
            var volume = ResolveVolume();
            if (!volume.Book.TryGetValue(Coords.Book, out var book))
            {
                book = new();
                volume.Book[Coords.Book] = book;
            }
            return book;
        }

        private ChapterNode ResolveChapter()
        {
            var book = ResolveBook();
            if (!book.Chapter.TryGetValue(Coords.Chapter, out var chapter))
            {
                chapter = new();
                book.Chapter[Coords.Chapter] = chapter;
            }
            return chapter;
        }

        private SectionNode ResolveSection()
        {
            var chapter = ResolveChapter();
            if (!chapter.Section.TryGetValue(Coords.Section, out var section))
            {
                section = new();
                chapter.Section[Coords.Section] = section;
            }
            return section;
        }

        public LibraryNode Library
        {
            get
            {
                return ResolveLibrary();
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
                return ResolveShelf();
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
                return ResolveSeries();
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
                return ResolveCollection();
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
                return ResolveVolume();
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
                return ResolveBook();
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
                return ResolveChapter();
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
                return ResolveSection();
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
                var section = ResolveSection();
                if (!section.Scroll.TryGetValue(Coords.Scroll, out var scroll))
                {
                    scroll = new();
                    section.Scroll[Coords.Scroll] = scroll;
                }
                return scroll;
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
                if (PhextModel.IsBreakCharacter(ch))
                {
                    breaking = true;
                }
                if (PhextModel.IsTextCharacter(ch) && breaking)
                {
                    breaking = false;
                    ++words;
                }
            }
            return words;
        }

        public void setScroll(string text)
        {
            if (text.Length > 0)
            {
                var priorText = Scroll.Text;
                var priorCount = GetWordCount(priorText);
                Scroll.Text = text;
                _byteCount += text.Length - priorText.Length;
                ++LeafCount;
                var count = GetWordCount(text);
                WordCount += (count - priorCount);
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
                _byteCount -= Section.Scroll[Coords.Scroll].Text.Length;
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
            return Scroll.Text;
        }

        public string EditorSummary(string action = "")
        {
            return Coords.EditorSummary(action);
        }
    }
}
