using System.Text;

namespace PhextNotepad
{
    public class PhextModel
    {
        public PhextModel()
        {
            Coords.Reset();
        }
        public PhextText Phext = new();
        public Coordinates Coords
        {
            get
            {
                return Phext.Coords;
            }
            set
            {
                Phext.Coords = value;
            }
        }

        public int LeafCount
        {
            get
            {
                return Phext.LeafCount;
            }
        }

        public int WordCount
        {
            get
            {
                return Phext.WordCount;
            }
        }

        public int ByteCount
        {
            get
            {
                return Phext.ByteCount;
            }
        }

        public int ScrollWordCount
        {
            get
            {
                return Phext.ScrollWordCount;
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

        public void Load(string data, bool showCoordinates, TreeView? treeView = null)
        {
            var charStream = data.ToCharArray();
            Phext = new();
            Coordinates local = new(true);
            var stage = new StringBuilder();
            var sectionNode = Phext.GetSectionTreeRoot(local);
            var chapterNode = Phext.GetChapterTreeRoot(local);
            var bookNode = Phext.GetBookTreeRoot(local);
            var volumeNode = Phext.GetVolumeTreeRoot(local);
            var collectionNode = Phext.GetCollectionTreeRoot(local);
            var seriesNode = Phext.GetSeriesTreeRoot(local);
            var shelfNode = Phext.GetShelfTreeRoot(local);
            var libraryNode = Phext.GetLibraryTreeRoot(local);
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
                        insertScroll(stage, local, sectionNode, showCoordinates);
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
                        Phext.SetSectionNode(sectionNode, local.GetSectionRoot());
                    }
                    if (next == SECTION_BREAK)
                    {
                        ++local.Section;
                        local.Scroll = 1;
                    }
                    sectionNode = Phext.GetSectionTreeRoot(local);
                }

                if (dimensions_broken >= 3)
                {
                    if (chapterNode.Nodes.Count > 0)
                    {
                        bookNode.Nodes.Add(chapterNode);
                        Phext.SetChapterNode(chapterNode, local.GetChapterRoot());
                    }
                    if (next == CHAPTER_BREAK)
                    {
                        ++local.Chapter;
                        local.Section = 1;
                        local.Scroll = 1;
                    }
                    sectionNode = Phext.GetSectionTreeRoot(local);
                    chapterNode = Phext.GetChapterTreeRoot(local);
                }

                if (dimensions_broken >= 4)
                {
                    if (bookNode.Nodes.Count > 0)
                    {
                        volumeNode.Nodes.Add(bookNode);
                        Phext.SetBookNode(bookNode, local.GetBookRoot());
                    }
                    if (next == BOOK_BREAK)
                    {
                        ++local.Book;
                        local.Chapter = 1;
                        local.Section = 1;
                        local.Scroll = 1;
                    }
                    sectionNode = Phext.GetSectionTreeRoot(local);
                    chapterNode = Phext.GetChapterTreeRoot(local);
                    bookNode = Phext.GetBookTreeRoot(local);
                }

                if (dimensions_broken >= 5)
                {
                    if (volumeNode.Nodes.Count > 0)
                    {
                        collectionNode.Nodes.Add(volumeNode);
                        Phext.SetVolumeNode(volumeNode, local.GetVolumeRoot());
                    }
                    if (next == VOLUME_BREAK)
                    {
                        ++local.Volume;
                        local.Book = 1;
                        local.Chapter = 1;
                        local.Section = 1;
                        local.Scroll = 1;
                    }
                    sectionNode = Phext.GetSectionTreeRoot(local);
                    chapterNode = Phext.GetChapterTreeRoot(local);
                    bookNode = Phext.GetBookTreeRoot(local);
                    volumeNode = Phext.GetVolumeTreeRoot(local);
                }

                if (dimensions_broken >= 6)
                {
                    if (collectionNode.Nodes.Count > 0)
                    {
                        seriesNode.Nodes.Add(collectionNode);
                        Phext.SetCollectionNode(collectionNode, local.GetCollectionRoot());
                    }
                    if (next == COLLECTION_BREAK)
                    {
                        ++local.Collection;
                        local.Volume = 1;
                        local.Book = 1;
                        local.Chapter = 1;
                        local.Section = 1;
                        local.Scroll = 1;
                    }
                    sectionNode = Phext.GetSectionTreeRoot(local);
                    chapterNode = Phext.GetChapterTreeRoot(local);
                    bookNode = Phext.GetBookTreeRoot(local);
                    volumeNode = Phext.GetVolumeTreeRoot(local);
                    collectionNode = Phext.GetCollectionTreeRoot(local);
                }

                if (dimensions_broken >= 7)
                {
                    if (seriesNode.Nodes.Count > 0)
                    {
                        shelfNode.Nodes.Add(seriesNode);
                        Phext.SetSeriesNode(seriesNode, local.GetSeriesRoot());
                    }
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
                    sectionNode = Phext.GetSectionTreeRoot(local);
                    chapterNode = Phext.GetChapterTreeRoot(local);
                    bookNode = Phext.GetBookTreeRoot(local);
                    volumeNode = Phext.GetVolumeTreeRoot(local);
                    collectionNode = Phext.GetCollectionTreeRoot(local);
                    seriesNode = Phext.GetSeriesTreeRoot(local);
                }

                if (dimensions_broken >= 8)
                {
                    if (shelfNode.Nodes.Count > 0)
                    {
                        libraryNode.Nodes.Add(shelfNode);
                        Phext.SetShelfNode(shelfNode, local.GetShelfRoot());
                    }
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
                    sectionNode = Phext.GetSectionTreeRoot(local);
                    chapterNode = Phext.GetChapterTreeRoot(local);
                    bookNode = Phext.GetBookTreeRoot(local);
                    volumeNode = Phext.GetVolumeTreeRoot(local);
                    collectionNode = Phext.GetCollectionTreeRoot(local);
                    seriesNode = Phext.GetSeriesTreeRoot(local);
                    shelfNode = Phext.GetShelfTreeRoot(local);
                }

                if (dimensions_broken >= 9)
                {
                    if (libraryNode.Nodes.Count > 0)
                    {
                        treeView?.Nodes.Add(libraryNode);
                        Phext.SetLibraryNode(libraryNode, local.GetLibraryRoot());
                    }
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
                    sectionNode = Phext.GetSectionTreeRoot(local);
                    chapterNode = Phext.GetChapterTreeRoot(local);
                    bookNode = Phext.GetBookTreeRoot(local);
                    volumeNode = Phext.GetVolumeTreeRoot(local);
                    collectionNode = Phext.GetCollectionTreeRoot(local);
                    seriesNode = Phext.GetSeriesTreeRoot(local);
                    shelfNode = Phext.GetShelfTreeRoot(local);
                    libraryNode = Phext.GetLibraryTreeRoot(local);
                }
            }

            if (stage.Length > 0)
            {
                insertScroll(stage, local, sectionNode, showCoordinates);
                stage.Clear();
            }
            if (sectionNode.Nodes.Count > 0)
            {
                chapterNode.Nodes.Add(sectionNode);
                Phext.SetSectionNode(sectionNode, local);
            }
            if (chapterNode.Nodes.Count > 0)
            {
                bookNode.Nodes.Add(chapterNode);
                Phext.SetChapterNode(chapterNode, local);
            }
            if (bookNode.Nodes.Count > 0)
            {
                volumeNode.Nodes.Add(bookNode);
                Phext.SetBookNode(bookNode, local);
            }
            if (volumeNode.Nodes.Count > 0)
            {
                collectionNode.Nodes.Add(volumeNode);
                Phext.SetVolumeNode(volumeNode, local);
            }
            if (collectionNode.Nodes.Count > 0)
            {
                seriesNode.Nodes.Add(collectionNode);
                Phext.SetCollectionNode(collectionNode, local);
            }
            if (seriesNode.Nodes.Count > 0)
            {
                shelfNode.Nodes.Add(seriesNode);
                Phext.SetSeriesNode(seriesNode, local);
            }
            if (shelfNode.Nodes.Count > 0)
            {
                libraryNode.Nodes.Add(shelfNode);
                Phext.SetShelfNode(shelfNode, local);
            }
            if (libraryNode.Nodes.Count > 0)
            {
                treeView?.Nodes.Add(libraryNode);
                Phext.SetLibraryNode(libraryNode, local);
            }
            Coords.Reset();
        }

        private void insertScroll(StringBuilder stage, Coordinates local, TreeNode? node, bool showCoordinates)
        {
            var scroll = stage.ToString();
            if (scroll.Length > 0)
            {
                var key = local.ToString();
                var line = GetScrollSummary(local, scroll);
                var scrollNode = node?.Nodes.Add(key, (showCoordinates ? $"{key}! {line}" : line));
                Phext.Coords = local;
                Phext.setScroll(scroll, scrollNode);
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
            foreach (var library_index in Phext.Root.Library.Keys)
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
                Serialize_Library(collector, Phext.Root.Library[library_index], local);
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
            return Phext.Find(coordinates);
        }

        public TreeNode CreateNode(TreeNode sectionNode, string line, bool showKey)
        {
            var key = Coords.ToString();
            var scrollNode = sectionNode.Nodes.Add(key, (showKey ? $"{key}: {line}" : line));
            Phext.Section.Node = scrollNode;
            Phext.Cache[scrollNode.Name] = scrollNode;
            return scrollNode;
        }
    }
}
