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
                if (next == SCROLL_BREAK ||
                    next == SECTION_BREAK ||
                    next == CHAPTER_BREAK ||
                    next == BOOK_BREAK ||
                    next == VOLUME_BREAK ||
                    next == COLLECTION_BREAK ||
                    next == SERIES_BREAK ||
                    next == SHELF_BREAK ||
                    next == LIBRARY_BREAK)
                {
                    if (stage.Length > 0)
                    {
                        insertScroll(stage, local, sectionNode);
                        stage.Clear();
                    }
                }

                if (next == SCROLL_BREAK)
                {
                    ++local.Scroll;
                    continue;
                }

                int dimensions_broken = 0;
                switch (next)
                {
                    case CHAPTER_BREAK:
                        ++dimensions_broken;
                        goto case SECTION_BREAK;
                    case SECTION_BREAK:
                        ++dimensions_broken;
                        goto case SCROLL_BREAK;
                    case SCROLL_BREAK:
                        ++dimensions_broken;
                        break;
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
                        bookNode.Nodes.Add(chapterNode);
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
                        volumeNode.Nodes.Add(bookNode);
                        Terse.SetBookNode(bookNode, local);
                    }
                    bookNode = Terse.GetBookTreeRoot(local);
                    continue;
                }

                if (next == VOLUME_BREAK)
                {
                    ++local.Volume;
                    local.Book = 1;
                    local.Chapter = 1;
                    local.Section = 1;
                    local.Scroll = 1;
                    if (volumeNode.Nodes.Count > 0)
                    {
                        collectionNode.Nodes.Add(volumeNode);
                        Terse.SetVolumeNode(volumeNode, local);
                    }
                    volumeNode = Terse.GetVolumeTreeRoot(local);
                    continue;
                }

                if (next == COLLECTION_BREAK)
                {
                    ++local.Collection;
                    local.Volume = 1;
                    local.Book = 1;
                    local.Chapter = 1;
                    local.Section = 1;
                    local.Scroll = 1;
                    if (collectionNode.Nodes.Count > 0)
                    {
                        seriesNode.Nodes.Add(collectionNode);
                        Terse.SetCollectionNode(collectionNode, local);
                    }
                    collectionNode = Terse.GetCollectionTreeRoot(local);
                    continue;
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
                    if (seriesNode.Nodes.Count > 0)
                    {
                        shelfNode.Nodes.Add(seriesNode);
                        Terse.SetSeriesNode(seriesNode, local);
                    }
                    seriesNode = Terse.GetSeriesTreeRoot(local);
                    continue;
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
                    if (shelfNode.Nodes.Count > 0)
                    {
                        libraryNode.Nodes.Add(shelfNode);
                        Terse.SetShelfNode(shelfNode, local);
                    }
                    shelfNode = Terse.GetShelfTreeRoot(local);
                    continue;
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
                    if (libraryNode.Nodes.Count > 0)
                    {
                        treeView?.Nodes.Add(libraryNode);
                        Terse.SetLibraryNode(libraryNode, local);
                    }
                    libraryNode = Terse.GetLibraryTreeRoot(local);
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
