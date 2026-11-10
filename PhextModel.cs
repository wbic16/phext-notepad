using Phext;

namespace PhextNotepad
{
    public class InsertResult
    {
        public bool PushToSQ { get; set; } = false;
        public Coordinates Coord { get; set; } = new();
        public string Scroll { get; set; } = string.Empty;
    }

    // The editor's facade over PhextDocument. Parsing and
    // serialization are libphext-cs's work now (PhextEngine.Phokenize
    // / Dephokenize); this class keeps the surface the form and the
    // tree have always used — Load with its SQ missing-scroll patch,
    // Serialize, the counters, and the break constants (aliases of
    // the library's, identical values).
    public class PhextModel
    {
        public PhextModel()
        {
            Coords.Reset();
        }

        public PhextDocument Phext = new();
        public Dictionary<Coordinates, string> PendingScrolls = new();

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
        public const char LINE_BREAK = PhextConstants.LINE_BREAK;
        public const char SCROLL_BREAK = PhextConstants.SCROLL_BREAK;
        public const char SECTION_BREAK = PhextConstants.SECTION_BREAK;
        public const char CHAPTER_BREAK = PhextConstants.CHAPTER_BREAK;
        public const char BOOK_BREAK = PhextConstants.BOOK_BREAK;
        public const char VOLUME_BREAK = PhextConstants.VOLUME_BREAK;
        public const char COLLECTION_BREAK = PhextConstants.COLLECTION_BREAK;
        public const char SERIES_BREAK = PhextConstants.SERIES_BREAK;
        public const char SHELF_BREAK = PhextConstants.SHELF_BREAK;
        public const char LIBRARY_BREAK = PhextConstants.LIBRARY_BREAK;

        public string getHierarchicalChecksum()
        {
            return Phext.HierarchicalChecksum;
        }

        // Parses `data` into the document only; the tree stays a
        // lazy view (PhextTree) over the result. The parse is the
        // library's tokenizer: Phokenize walks the same breaks the
        // vendored loader walked, applies the same break semantics
        // (each break advances its own dimension and resets the lower
        // ones), and reports each run of text at the coordinate the
        // walker occupied when the run began — the vendored loader's
        // insert points exactly. Empty runs store nothing, as before,
        // and the SQ missing-scroll patch is preserved verbatim: a
        // scroll whose whole text is the marker is not stored; its
        // coordinate and the text already held there are queued in
        // PendingScrolls for the form to push back to SQ.
        public void Load(string data, bool resetPhext = true)
        {
            if (resetPhext)
            {
                Phext = new();
            }
            PendingScrolls.Clear();
            foreach (var token in PhextEngine.Phokenize(data))
            {
                if (token.Scroll.Length == 0)
                {
                    continue;
                }
                if (token.Scroll == "---sq:Scroll-Missing---")
                {
                    // hack to patch missing scrolls back to SQ
                    var coord = new Coordinates(token.Coord);
                    PendingScrolls[coord] = Phext.GetAt(token.Coord);
                    continue;
                }
                Phext.SetAt(token.Coord, token.Scroll);
            }
            Coords.Reset();
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

        // The library's serializer over the document's scrolls in
        // coordinate order: Dephokenize emits the same minimal break
        // runs the vendored nested walk emitted (advance the highest
        // dimension that is behind, one break at a time), so the
        // output is byte-identical for the same stored scrolls.
        public string Serialize()
        {
            var tokens = new List<PositionedScroll>(Phext.Scrolls.Count);
            foreach (var (coord, text) in Phext.Scrolls)
            {
                tokens.Add(new PositionedScroll(coord, text));
            }
            return PhextEngine.Dephokenize(tokens);
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
    }
}
