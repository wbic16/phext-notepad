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

        public const char WORD_BREAK = '\x20';
        public const char LINE_BREAK = '\n';
        public const char SCROLL_BREAK = '\x17';
        public const char SECTION_BREAK = '\x18';
        public const char CHAPTER_BREAK = '\x19';
        public const char BOOK_BREAK = '\x1A';
        public const char VOLUME_BREAK = '\x1C';
        public const char COLLECTION_BREAK = '\x1D';
        public const char SERIES_BREAK = '\x1E';
        public const char SHELF_BREAK = '\x1F';
        public const char LIBRARY_BREAK = '\x01';

        public void Load(string data, TreeView? treeView = null)
        {
            var charStream = data.ToCharArray();
            Terse = new();
            Coordinates local = new(true);
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
                int dimensions_broken = 0;
                switch (next)
                {
                    case LIBRARY_BREAK:
                        dimensions_broken = 9;
                        break;
                    case SHELF_BREAK:
                        dimensions_broken = 8;
                        break;
                    case SERIES_BREAK:
                        dimensions_broken = 7;
                        break;
                    case COLLECTION_BREAK:
                        dimensions_broken = 6;
                        break;
                    case VOLUME_BREAK:
                        dimensions_broken = 5;
                        break;
                    case BOOK_BREAK:
                        dimensions_broken = 4;
                        break;
                    case CHAPTER_BREAK:
                        dimensions_broken = 3;
                        break;
                    case SECTION_BREAK:
                        dimensions_broken = 2;
                        break;
                    case SCROLL_BREAK:
                        dimensions_broken = 1;
                        break;
                    default:
                        stage.Append(next);
                        break;
                }

                if (dimensions_broken == 0)
                {
                    continue;
                }

                if (dimensions_broken >= 1)
                {
                    if (stage.Length > 0)
                    {
                        insertScroll(stage, local, sectionNode);
                        stage.Clear();
                    }
                    if (next == SCROLL_BREAK)
                    {
                        ++local.Scroll;
                    }
                }

                if (dimensions_broken >= 2)
                {
                    if (sectionNode.Nodes.Count > 0)
                    {
                        chapterNode.Nodes.Add(sectionNode);
                        Terse.SetSectionNode(sectionNode, local.GetSectionRoot());
                    }
                    sectionNode = Terse.GetSectionTreeRoot(local);
                    if (next == SECTION_BREAK)
                    {
                        ++local.Section;
                        local.Scroll = 1;
                    }
                }

                if (dimensions_broken >= 3)
                {
                    if (chapterNode.Nodes.Count > 0)
                    {
                        bookNode.Nodes.Add(chapterNode);
                        Terse.SetChapterNode(chapterNode, local.GetChapterRoot());
                    }
                    chapterNode = Terse.GetChapterTreeRoot(local);
                    if (next == CHAPTER_BREAK)
                    {
                        ++local.Chapter;
                        local.Section = 1;
                        local.Scroll = 1;
                    }
                }

                if (dimensions_broken >= 4)
                {
                    if (bookNode.Nodes.Count > 0)
                    {
                        volumeNode.Nodes.Add(bookNode);
                        Terse.SetBookNode(bookNode, local.GetBookRoot());
                    }
                    bookNode = Terse.GetBookTreeRoot(local);
                    if (next == BOOK_BREAK)
                    {
                        ++local.Book;
                        local.Chapter = 1;
                        local.Section = 1;
                        local.Scroll = 1;
                    }
                }

                if (dimensions_broken >= 5)
                {
                    if (volumeNode.Nodes.Count > 0)
                    {
                        collectionNode.Nodes.Add(volumeNode);
                        Terse.SetVolumeNode(volumeNode, local.GetVolumeRoot());
                    }
                    volumeNode = Terse.GetVolumeTreeRoot(local);
                    if (next == VOLUME_BREAK)
                    {
                        ++local.Volume;
                        local.Book = 1;
                        local.Chapter = 1;
                        local.Section = 1;
                        local.Scroll = 1;
                    }
                }

                if (dimensions_broken >= 6)
                {
                    if (collectionNode.Nodes.Count > 0)
                    {
                        seriesNode.Nodes.Add(collectionNode);
                        Terse.SetCollectionNode(collectionNode, local.GetCollectionRoot());
                    }
                    collectionNode = Terse.GetCollectionTreeRoot(local);
                    if (next == COLLECTION_BREAK)
                    {
                        ++local.Collection;
                        local.Volume = 1;
                        local.Book = 1;
                        local.Chapter = 1;
                        local.Section = 1;
                        local.Scroll = 1;
                    }
                }

                if (dimensions_broken >= 7)
                {
                    if (seriesNode.Nodes.Count > 0)
                    {
                        shelfNode.Nodes.Add(seriesNode);
                        Terse.SetSeriesNode(seriesNode, local.GetSeriesRoot());
                    }
                    seriesNode = Terse.GetSeriesTreeRoot(local);
                    if (next == SERIES_BREAK)
                    {
                        ++local.Series;
                        local.Collection = 1;
                        local.Volume = 1;
                        local.Book = 1;
                        local.Chapter = 1;
                        local.Section = 1;
                        local.Scroll = 1;
                    }
                }

                if (dimensions_broken >= 8)
                {
                    if (shelfNode.Nodes.Count > 0)
                    {
                        libraryNode.Nodes.Add(shelfNode);
                        Terse.SetShelfNode(shelfNode, local.GetShelfRoot());
                    }
                    shelfNode = Terse.GetShelfTreeRoot(local);
                    if (next == SHELF_BREAK)
                    {
                        ++local.Shelf;
                        local.Series = 1;
                        local.Collection = 1;
                        local.Volume = 1;
                        local.Book = 1;
                        local.Chapter = 1;
                        local.Section = 1;
                        local.Scroll = 1;
                    }
                }

                if (dimensions_broken >= 9)
                {
                    if (libraryNode.Nodes.Count > 0)
                    {
                        treeView?.Nodes.Add(libraryNode);
                        Terse.SetLibraryNode(libraryNode, local.GetLibraryRoot());
                    }
                    libraryNode = Terse.GetLibraryTreeRoot(local);
                    if (next == LIBRARY_BREAK)
                    {
                        ++local.Library;
                        local.Shelf = 1;
                        local.Series = 1;
                        local.Collection = 1;
                        local.Volume = 1;
                        local.Book = 1;
                        local.Chapter = 1;
                        local.Section = 1;
                        local.Scroll = 1;
                    }
                }
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
                bookNode.Nodes.Add(chapterNode);
                Terse.SetChapterNode(chapterNode, local);
            }
            if (bookNode.Nodes.Count > 0)
            {
                volumeNode.Nodes.Add(bookNode);
                Terse.SetBookNode(bookNode, local);
            }
            if (volumeNode.Nodes.Count > 0)
            {
                collectionNode.Nodes.Add(volumeNode);
                Terse.SetVolumeNode(volumeNode, local);
            }
            if (collectionNode.Nodes.Count > 0)
            {
                seriesNode.Nodes.Add(collectionNode);
                Terse.SetCollectionNode(collectionNode, local);
            }
            if (seriesNode.Nodes.Count > 0)
            {
                shelfNode.Nodes.Add(seriesNode);
                Terse.SetSeriesNode(seriesNode, local);
            }
            if (shelfNode.Nodes.Count > 0)
            {
                libraryNode.Nodes.Add(shelfNode);
                Terse.SetShelfNode(shelfNode, local);
            }
            if (libraryNode.Nodes.Count > 0)
            {
                treeView?.Nodes.Add(libraryNode);
                Terse.SetLibraryNode(libraryNode, local);
            }
            Coords.Reset();
        }

        private void insertScroll(StringBuilder stage, Coordinates local, TreeNode? node)
        {
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
            StringBuilder collector = new();
            Coordinates local = new(true);
            foreach (var library_index in Terse.Root.Library.Keys)
            {
                while (local.Library < library_index)
                {
                    collector.Append(LIBRARY_BREAK);
                    ++local.Library;
                    local.Shelf = 1;
                    local.Series = 1;
                    local.Collection = 1;
                    local.Volume = 1;
                    local.Book = 1;
                    local.Chapter = 1;
                    local.Section = 1;
                    local.Scroll = 1;
                }
                Serialize_Library(collector, Terse.Root.Library[library_index], local);
            }
            return collector.ToString();
        }
        private void Serialize_Library(StringBuilder collector, LibraryNode library, Coordinates local)
        {
            foreach (var shelf_index in library.Shelf.Keys)
            {
                while (local.Shelf < shelf_index)
                {
                    collector.Append(SHELF_BREAK);
                    ++local.Shelf;
                    local.Series = 1;
                    local.Collection = 1;
                    local.Volume = 1;
                    local.Book = 1;
                    local.Chapter = 1;
                    local.Section = 1;
                    local.Scroll = 1;
                }
                Serialize_Shelf(collector, library.Shelf[shelf_index], local);
            }
        }

        private void Serialize_Shelf(StringBuilder collector, ShelfNode shelf, Coordinates local)
        {
            foreach (var series_index in shelf.Series.Keys)
            {
                while (local.Series < series_index)
                {
                    collector.Append(SERIES_BREAK);
                    ++local.Series;
                    local.Collection = 1;
                    local.Volume = 1;
                    local.Book = 1;
                    local.Chapter = 1;
                    local.Section = 1;
                    local.Scroll = 1;
                }
                Serialize_Series(collector, shelf.Series[series_index], local);
            }
        }
        private void Serialize_Series(StringBuilder collector, SeriesNode series, Coordinates local)
        {
            foreach (var collection_index in series.Collection.Keys)
            {
                while (local.Collection < collection_index)
                {
                    collector.Append(COLLECTION_BREAK);
                    ++local.Collection;
                    local.Volume = 1;
                    local.Book = 1;
                    local.Chapter = 1;
                    local.Section = 1;
                    local.Scroll = 1;
                }
                Serialize_Collection(collector, series.Collection[collection_index], local);
            }
        }

        private void Serialize_Collection(StringBuilder collector, CollectionNode collection, Coordinates local)
        {
            foreach (var volume_index in collection.Volume.Keys)
            {
                while (local.Volume < volume_index)
                {
                    collector.Append(VOLUME_BREAK);
                    ++local.Volume;
                    local.Book = 1;
                    local.Chapter = 1;
                    local.Section = 1;
                    local.Scroll = 1;
                }
                Serialize_Volume(collector, collection.Volume[volume_index], local);
            }
        }

        private void Serialize_Volume(StringBuilder collector, VolumeNode volume, Coordinates local)
        {
            foreach (var book_index in volume.Book.Keys)
            {
                while (local.Book < book_index)
                {
                    collector.Append(BOOK_BREAK);
                    ++local.Book;
                    local.Chapter = 1;
                    local.Section = 1;
                    local.Scroll = 1;
                }
                Serialize_Book(collector, volume.Book[book_index], local);
            }
        }

        private void Serialize_Book(StringBuilder collector, BookNode book, Coordinates local)
        {
            foreach (var chapter_index in book.Chapter.Keys)
            {
                while (local.Chapter < chapter_index)
                {
                    collector.Append(CHAPTER_BREAK);
                    ++local.Chapter;
                    local.Section = 1;
                    local.Scroll = 1;
                }
                Serialize_Chapter(collector, book.Chapter[chapter_index], local);
            }
        }

        private void Serialize_Chapter(StringBuilder collector, ChapterNode chapter, Coordinates local)
        {
            foreach (var section_index in chapter.Section.Keys)
            {
                while (local.Section < section_index)
                {
                    collector.Append(SECTION_BREAK);
                    ++local.Section;
                    local.Scroll = 1;
                }
                Serialize_Section(collector, chapter.Section[section_index], local);
            }
        }

        private void Serialize_Section(StringBuilder collector, SectionNode section, Coordinates local)
        {
            foreach (var scroll_index in section.Scroll.Keys)
            {
                while (local.Scroll < scroll_index)
                {
                    collector.Append(SCROLL_BREAK);
                    ++local.Scroll;
                }
                Serialize_Scroll(collector, section.Scroll[scroll_index]);
            }
        }

        private void Serialize_Scroll(StringBuilder collector, ScrollNode scroll)
        {
            collector.Append(scroll.Text);
        }

        public static bool IsBreakCharacter(char ch)
        {
            return ch == WORD_BREAK || ch == LINE_BREAK ||
                   ch == SCROLL_BREAK || ch == SECTION_BREAK || ch == CHAPTER_BREAK ||
                   ch == BOOK_BREAK || ch == VOLUME_BREAK || ch == COLLECTION_BREAK ||
                   ch == SERIES_BREAK || ch == SHELF_BREAK || ch == LIBRARY_BREAK;
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
