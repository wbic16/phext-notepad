using System.IO.Hashing;
using System.Text;
using static PhextNotepad.Coordinates;
using static System.Collections.Specialized.BitVector32;

namespace PhextNotepad
{
    public class ScrollNode
    {
        public TreeNode Node { get; set; } = new();
        public string Text { get; set; } = string.Empty;
    };
    public interface IPhextNode<T, S>
        where T : notnull
    {
        public TreeNode Node { get; set; }
        public SortedDictionary<T, S> Children { get; set; }
        public char Delimiter { get; }
    };
    public class SectionNode : IPhextNode<ScrollIndex, ScrollNode>
    {
        public TreeNode Node { get; set; } = new();
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
        public TreeNode Node { get; set; } = new();
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
        public TreeNode Node { get; set; } = new();
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
        public TreeNode Node { get; set; } = new();
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
        public TreeNode Node { get; set; } = new();
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
        public TreeNode Node { get; set; } = new();
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
        public TreeNode Node { get; set; } = new();
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
        public TreeNode Node { get; set; } = new();
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
        public Dictionary<string, TreeNode> Cache = new();
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
                        result += PhextModel.LIBRARY_BREAK;
                    }
                    foreach (var shelf_key in library.Shelf.Keys)
                    {
                        while (walker.Shelf < shelf_key)
                        {
                            walker.ShelfBreak();
                            result += PhextModel.SHELF_BREAK;
                        }
                        var shelf = library.Shelf[shelf_key];
                        foreach (var series_key in shelf.Series.Keys)
                        {
                            while (walker.Series < series_key)
                            {
                                walker.SeriesBreak();
                                result += PhextModel.SERIES_BREAK;
                            }
                            var series = shelf.Series[series_key];
                            foreach (var collection_key in series.Collection.Keys)
                            {
                                while (walker.Collection < collection_key)
                                {
                                    walker.CollectionBreak();
                                    result += PhextModel.COLLECTION_BREAK;
                                }
                                var collection = series.Collection[collection_key];
                                foreach (var volume_key in collection.Volume.Keys)
                                {
                                    while (walker.Volume < volume_key)
                                    {
                                        walker.VolumeBreak();
                                        result += PhextModel.VOLUME_BREAK;
                                    }
                                    var volume = collection.Volume[volume_key];
                                    foreach (var book_key in volume.Book.Keys)
                                    {
                                        while (walker.Book < book_key)
                                        {
                                            walker.BookBreak();
                                            result += PhextModel.BOOK_BREAK;
                                        }
                                        var book = volume.Book[book_key];
                                        foreach (var chapter_key in book.Chapter.Keys)
                                        {
                                            while (walker.Chapter < chapter_key)
                                            {
                                                walker.ChapterBreak();
                                                result += PhextModel.CHAPTER_BREAK;
                                            }
                                            var chapter = book.Chapter[chapter_key];
                                            foreach (var section_key in chapter.Section.Keys)
                                            {
                                                while (walker.Section < section_key)
                                                {
                                                    walker.SectionBreak();
                                                    result += PhextModel.SECTION_BREAK;
                                                }
                                                var section = chapter.Section[section_key];
                                                foreach (var scroll_key in section.Scroll.Keys)
                                                {
                                                    while (walker.Scroll < scroll_key)
                                                    {
                                                        walker.ScrollBreak();
                                                        result += PhextModel.SCROLL_BREAK;
                                                    }
                                                    var scroll = section.Scroll[scroll_key];
                                                    _hasher.Reset();
                                                    _hasher.Append(Encoding.UTF8.GetBytes(scroll.Text));

                                                    result += _hasher.GetCurrentHashAsUInt128().ToString("x32");
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
        public int ByteCount {
            get
            {
                int total = 0;
                foreach (var lib_key in Root.Library.Keys)
                {
                    var library = Root.Library[lib_key];
                    foreach (var shelf_key in library.Shelf.Keys)
                    {
                        var shelf = library.Shelf[shelf_key];
                        foreach (var series_key in shelf.Series.Keys)
                        {
                            var series = shelf.Series[series_key];
                            foreach (var collection_key in series.Collection.Keys)
                            {
                                var collection = series.Collection[collection_key];
                                foreach (var volume_key in collection.Volume.Keys)
                                {
                                    var volume = collection.Volume[volume_key];
                                    foreach (var book_key in volume.Book.Keys)
                                    {
                                        var book = volume.Book[book_key];
                                        foreach (var chapter_key in book.Chapter.Keys)
                                        {
                                            var chapter = book.Chapter[chapter_key];
                                            foreach (var section_key in chapter.Section.Keys)
                                            {
                                                var section = chapter.Section[section_key];
                                                foreach (var scroll_key in section.Scroll.Keys)
                                                {
                                                    var scroll = section.Scroll[scroll_key];
                                                    total += scroll.Text.Length;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return total;
            }
        }

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

        public void setScroll(string text, TreeNode? node = null)
        {
            if (text.Length > 0)
            {
                var priorText = Scroll.Text;
                var priorCount = GetWordCount(priorText);
                Scroll.Text = text;
                ++LeafCount;
                var count = GetWordCount(text);
                WordCount += (count - priorCount);
                if (node != null)
                {
                    Scroll.Node = node;
                    Cache[node.Name] = node;
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
            return Scroll.Text;
        }

        public string EditorSummary(string action = "")
        {
            return Coords.EditorSummary(action);
        }

        public void SetLibraryNode(TreeNode libraryNode, Coordinates local)
        {
            if (!Root.Library.ContainsKey(local.Library))
            {
                Root.Library[local.Library] = new()
                {
                    Node = libraryNode
                };
            }
            Cache[libraryNode.Name] = libraryNode;
        }

        public void SetShelfNode(TreeNode shelfNode, Coordinates local)
        {
            if (!Library.Shelf.ContainsKey(local.Shelf))
            {
                Library.Shelf[local.Shelf] = new()
                {
                    Node = shelfNode
                };
            }
            Cache[shelfNode.Name] = shelfNode;
        }

        public void SetSeriesNode(TreeNode seriesNode, Coordinates local)
        {
            if (!Shelf.Series.ContainsKey(local.Series))
            {
                Shelf.Series[local.Series] = new()
                {
                    Node = seriesNode
                };
            }
            Cache[seriesNode.Name] = seriesNode;
        }

        public void SetCollectionNode(TreeNode collectionNode, Coordinates local)
        {
            if (!Series.Collection.ContainsKey(local.Collection))
            {
                Series.Collection[local.Collection] = new()
                {
                    Node = collectionNode
                };
            }
            Cache[collectionNode.Name] = collectionNode;
        }

        public void SetVolumeNode(TreeNode volumeNode, Coordinates local)
        {
            if (!Collection.Volume.ContainsKey(local.Volume))
            {
                Collection.Volume[local.Volume] = new()
                {
                    Node = volumeNode
                };
            }
            Cache[volumeNode.Name] = volumeNode;
        }

        public void SetBookNode(TreeNode bookNode, Coordinates local)
        {
            if (!Volume.Book.ContainsKey(local.Book))
            {
                Volume.Book[local.Book] = new()
                {
                    Node = bookNode
                };
            }
            Cache[bookNode.Name] = bookNode;
        }

        public void SetChapterNode(TreeNode chapterNode, Coordinates local)
        {
            if (!Book.Chapter.ContainsKey(local.Chapter))
            {
                Book.Chapter[local.Chapter] = new()
                {
                    Node = chapterNode
                };
            }
            Cache[chapterNode.Name] = chapterNode;
        }

        public void SetSectionNode(TreeNode sectionNode, Coordinates local)
        {
            if (!Book.Chapter.ContainsKey(local.Chapter))
            {
                Book.Chapter[local.Chapter] = new();
            }
            if (!Book.Chapter[local.Chapter].Section.ContainsKey(local.Section))
            {
                Book.Chapter[local.Chapter].Section[local.Section] = new()
                {
                    Node = sectionNode
                };
            }
            Cache[sectionNode.Name] = sectionNode;
        }

        private TreeNode CreateNamedRootNode(string text, Coordinates local)
        {
            var name = local.ToString();
            var node = new TreeNode(text)
            {
                Name = name
            };
            Cache[name] = node;
            return node;
        }

        public TreeNode GetSectionTreeRoot(Coordinates local)
        {
            return CreateNamedRootNode($"Section {local.Section}", local.GetSectionRoot());
        }

        public TreeNode GetChapterTreeRoot(Coordinates local)
        {
            return CreateNamedRootNode($"Chapter {local.Chapter}", local.GetChapterRoot());
        }

        public TreeNode GetBookTreeRoot(Coordinates local)
        {
            return CreateNamedRootNode($"Book {local.Book}", local.GetBookRoot());
        }

        public TreeNode GetVolumeTreeRoot(Coordinates local)
        {
            return CreateNamedRootNode($"Volume {local.Volume}", local.GetVolumeRoot());
        }

        public TreeNode GetCollectionTreeRoot(Coordinates local)
        {
            return CreateNamedRootNode($"Collection {local.Collection}", local.GetCollectionRoot());
        }

        public TreeNode GetSeriesTreeRoot(Coordinates local)
        {
            return CreateNamedRootNode($"Series {local.Series}", local.GetSeriesRoot());
        }

        public TreeNode GetShelfTreeRoot(Coordinates local)
        {
            return CreateNamedRootNode($"Shelf {local.Shelf}", local.GetShelfRoot());
        }

        public TreeNode GetLibraryTreeRoot(Coordinates local)
        {
            return CreateNamedRootNode($"Library {local.Library}", local.GetLibraryRoot());
        }

        public TreeNode? Find(Coordinates coordinates)
        {
            string test = coordinates.ToString();
            if (Cache.ContainsKey(test))
            {
                return Cache[test];
            }

            return null;
        }
    }
}