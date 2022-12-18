using System.Text;

namespace TerseNotepad
{
    public class TerseModel
    {
        public TerseModel()
        {
            Coords.Reset();
        }
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

        public int LeafCount
        {
            get
            {
                return Terse.LeafCount;
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

        public void Load(string data, TreeView? treeView = null)
        {
            var charStream = data.ToCharArray();
            Terse = new();
            Coordinates local = new();
            local.Reset();
            var stage = new StringBuilder();
            var sectionNode = Terse.GetSectionTreeRoot(local);
            var chapterNode = Terse.GetChapterTreeRoot(local);
            var bookNode = Terse.GetBookTreeRoot(local);
            var volumeNode = Terse.GetVolumeTreeRoot(local);
            var collectionNode = Terse.GetCollectionTreeRoot(local);
            var seriesNode = Terse.GetSeriesTreeRoot(local);
            var shelfNode = Terse.GetShelfTreeRoot(local);
            var libraryNode = Terse.GetLibraryTreeRoot(local);
            for (int i = 0; i < charStream.Length; ++i)
            {
                var next = charStream[i];
                if (next == SCROLL_BREAK ||
                    next == SECTION_BREAK ||
                    next == CHAPTER_BREAK ||
                    next == BOOK_BREAK ||
                    next == COLLECTION_BREAK ||
                    next == VOLUME_BREAK ||
                    next == SERIES_BREAK ||
                    next == SHELF_BREAK ||
                    next == LIBRARY_BREAK)
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
                    if (sectionNode.Nodes.Count > 0)
                    {
                        chapterNode.Nodes.Add(sectionNode);
                        Terse.SetSectionNode(sectionNode, local);
                    }
                    sectionNode = Terse.GetSectionTreeRoot(local);
                    continue;
                }

                if (next == CHAPTER_BREAK)
                {
                    ++local.Chapter;
                    local.Section = 1;
                    local.Scroll = 1;
                    if (chapterNode.Nodes.Count > 0)
                    {
                        treeView?.Nodes.Add(chapterNode);
                        Terse.SetChapterNode(chapterNode, local);
                    }
                    chapterNode = Terse.GetChapterTreeRoot(local);
                    continue;
                }

                if (next == BOOK_BREAK)
                {
                    ++local.Book;
                    local.Chapter = 1;
                    local.Section = 1;
                    local.Scroll = 1;
                    if (bookNode.Nodes.Count > 0)
                    {
                        treeView?.Nodes.Add(bookNode);
                        Terse.SetBookNode(bookNode, local);
                    }
                    bookNode = Terse.GetBookTreeRoot(local);
                    continue;
                }

                stage.Append(next);
            }

            if (stage.Length > 0)
            {
                insertScroll(stage, local, sectionNode);
                stage.Clear();
            }
            if (sectionNode.Nodes.Count > 0)
            {
                chapterNode.Nodes.Add(sectionNode);
                Terse.SetSectionNode(sectionNode, local);
            }
            if (chapterNode.Nodes.Count > 0)
            {
                treeView?.Nodes.Add(chapterNode);
                Terse.SetChapterNode(chapterNode, local);
            }
            Coords.Reset();
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
                Terse.Coords = local;
                Terse.setScroll(scroll, scrollNode);
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
            uint library_break_counter = 0;
            var local = new Coordinates();
            foreach (var library_index in Terse.Root.Library.Keys)
            {
                local.Library = library_index;
                library_break_counter = Serialize_Library(collector, Terse.Root.Library[library_index], library_break_counter, local);
            }
            return collector.ToString();
        }
        private uint Serialize_Library(StringBuilder collector, LibraryNode library, uint library_break_counter, Coordinates local)
        {
            while (++library_break_counter < local.Library)
            {
                collector.Append(LIBRARY_BREAK);
            }
            uint shelf_break_counter = 0;
            foreach (var shelf_index in library.Shelf.Keys)
            {
                local.Shelf = shelf_index;
                shelf_break_counter = Serialize_Shelf(collector, library.Shelf[shelf_index], shelf_break_counter, local);
            }

            return shelf_break_counter;
        }

        private uint Serialize_Shelf(StringBuilder collector, ShelfNode shelf, uint shelf_break_counter, Coordinates local)
        {
            while (++shelf_break_counter < local.Shelf)
            {
                collector.Append(SHELF_BREAK);
            }
            uint series_break_counter = 0;
            foreach (var series_index in shelf.Series.Keys)
            {
                local.Series = series_index;
                series_break_counter = Serialize_Series(collector, shelf.Series[series_index], series_break_counter, local);
            }

            return series_break_counter;
        }
        private uint Serialize_Series(StringBuilder collector, SeriesNode series, uint series_break_counter, Coordinates local)
        {
            while (++series_break_counter < local.Series)
            {
                collector.Append(SERIES_BREAK);
            }
            uint collection_break_counter = 0;
            foreach (var collection_index in series.Collection.Keys)
            {
                local.Collection = collection_index;
                collection_break_counter = Serialize_Collection(collector, series.Collection[collection_index], collection_break_counter, local);
            }

            return collection_break_counter;
        }

        private uint Serialize_Collection(StringBuilder collector, CollectionNode collection, uint collection_break_counter, Coordinates local)
        {
            while (++collection_break_counter < local.Collection)
            {
                collector.Append(COLLECTION_BREAK);
            }
            uint volume_break_counter = 0;
            foreach (var volume_index in collection.Volume.Keys)
            {
                local.Volume = volume_index;
                volume_break_counter = Serialize_Volume(collector, collection.Volume[volume_index], volume_break_counter, local);
            }

            return volume_break_counter;
        }

        private uint Serialize_Volume(StringBuilder collector, VolumeNode volume, uint volume_break_counter, Coordinates local)
        {
            while (++volume_break_counter < local.Volume)
            {
                collector.Append(VOLUME_BREAK);
            }
            uint book_break_counter = 0;
            foreach (var book_index in volume.Book.Keys)
            {
                local.Book = book_index;
                book_break_counter = Serialize_Book(collector, volume.Book[book_index], book_break_counter, local);
            }

            return book_break_counter;
        }

        private uint Serialize_Book(StringBuilder collector, BookNode book, uint book_break_counter, Coordinates local)
        {
            while (++book_break_counter < local.Book)
            {
                collector.Append(BOOK_BREAK);
            }
            uint chapter_break_counter = 0;
            foreach (var chapter_index in book.Chapter.Keys)
            {
                local.Chapter = chapter_index;
                chapter_break_counter = Serialize_Chapter(collector, book.Chapter[chapter_index], chapter_break_counter, local);
            }

            return chapter_break_counter;
        }

        private uint Serialize_Chapter(StringBuilder collector, ChapterNode chapter, uint chapter_break_counter, Coordinates local)
        {
            while (++chapter_break_counter < local.Chapter)
            {
                collector.Append(CHAPTER_BREAK);
            }
            uint section_break_counter = 0;
            foreach (var section_index in chapter.Section.Keys)
            {
                local.Section = section_index;
                section_break_counter = Serialize_Section(collector, chapter.Section[section_index], section_break_counter, local);
            }

            return chapter_break_counter;
        }

        private uint Serialize_Section(StringBuilder collector, SectionNode section, uint section_break_counter, Coordinates local)
        {
            while (++section_break_counter < local.Section)
            {
                collector.Append(SECTION_BREAK);
            }
            uint scroll_break_counter = 0;
            foreach (var scroll_index in section.Scroll.Keys)
            {
                local.Scroll = scroll_index;
                scroll_break_counter = Serialize_Scroll(collector, section.Scroll[scroll_index], scroll_break_counter, local);
            }

            return section_break_counter;
        }

        private uint Serialize_Scroll(StringBuilder collector, ScrollNode scroll, uint scroll_break_counter, Coordinates local)
        {
            while (++scroll_break_counter < local.Scroll)
            {
                collector.Append(SCROLL_BREAK);
            }
            collector.Append(scroll.Text);

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
            return Terse.Find(coordinates);
        }

        public TreeNode CreateNode(TreeNode sectionNode, string line)
        {
            var key = Coords.ToString();
            var scrollNode = sectionNode.Nodes.Add(key, key + ": " + line);
            Terse.Section.Node = scrollNode;
            Terse.Cache[scrollNode.Name] = scrollNode;
            return scrollNode;
        }
    }
}
