using System.IO.Hashing;
using System.Text;
using Phext;

namespace PhextNotepad
{
    // The document store, rebuilt on libphext-cs. A phext document is
    // a set of scrolls addressed by coordinate; PhextModel parses a
    // buffer with the library's PhextEngine.Phokenize and serializes
    // with Dephokenize, and this class keeps the scrolls in one
    // coordinate-sorted list between those two calls.
    //
    // The vendored implementation (the deleted PhextText.cs) kept
    // nine nested sorted dictionaries — a tree of node objects whose
    // accessors materialized every level they walked. Two observable
    // consequences of that design do not carry over, deliberately:
    // navigating to a coordinate created its (empty) group chain and
    // an empty scroll node, so the tree could show groups with no
    // scrolls beneath them, and Serialize() emitted the extra breaks
    // needed to walk past a probed-but-never-written scroll — the
    // saved bytes depended on the user's navigation history. Here a
    // group exists exactly when a scroll exists beneath it, reading a
    // missing scroll changes nothing, and the serialized form is a
    // pure function of the stored scrolls. For every document the old
    // code loaded and saved without such probes, Serialize(),
    // enumeration, and the hierarchical checksum are byte-identical
    // (verified against the old implementation's dumps).
    //
    // What stays app-side, because the library has no equivalent:
    // - the current-coordinate cursor and its editing operations
    //   (setScroll / getScroll / processDelta, with the same delta
    //   cascade the editor has always used — the library's AddressBy
    //   carries across dimensions; the editor's never has);
    // - the word/byte counts, maintained incrementally because the
    //   status bar reads them on every cursor move;
    // - the hierarchical checksum: a per-scroll XxHash128 manifest
    //   this app exchanges with SQ during sync. The library's
    //   Checksum is an MD5 stand-in over a whole document — a
    //   different tool, not a substitute.
    //
    // One counting change to know about: LeafCount is now the exact
    // number of stored scrolls. The vendored setScroll incremented it
    // on every non-empty set — including re-setting a scroll that
    // already had text — so after the first edit it no longer counted
    // anything real. On a freshly loaded document (the number the
    // status flow reports) the two agree.
    public class PhextDocument
    {
        public Coordinates Coords = new();
        public SortedList<Coordinate, string> Scrolls = new();
        public int LeafCount { get; private set; } = 0;
        public int WordCount { get; private set; } = 0;
        private int _byteCount = 0;
        private readonly XxHash128 _hasher = new();

        public int ByteCount
        {
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

        public string HierarchicalChecksum
        {
            get
            {
                var result = new StringBuilder();
                foreach (var (coord, text) in Scrolls)
                {
                    if (text.Length == 0)
                    {
                        continue;
                    }
                    _hasher.Reset();
                    _hasher.Append(Encoding.UTF8.GetBytes(text));
                    result.Append(coord.ToString());
                    result.Append(": ");
                    result.Append(_hasher.GetCurrentHashAsUInt128().ToString("x32"));
                    result.Append('\n');
                }
                return result.ToString();
            }
        }

        public string GetAt(Coordinate coord)
        {
            return Scrolls.TryGetValue(coord, out var text) ? text : "";
        }

        public bool ExistsAt(Coordinate coord)
        {
            return Scrolls.ContainsKey(coord);
        }

        // Stores `text` at `coord`; empty text removes the scroll.
        // The key is snapshotted: callers pass live cursors (Coords)
        // whose components keep moving after the call.
        public void SetAt(Coordinate coord, string text)
        {
            var key = new Coordinates(coord);
            if (text.Length == 0)
            {
                if (Scrolls.TryGetValue(key, out var prior))
                {
                    Scrolls.Remove(key);
                    --LeafCount;
                    WordCount -= GetWordCount(prior);
                    _byteCount -= prior.Length;
                }
                return;
            }
            if (Scrolls.TryGetValue(key, out var old))
            {
                WordCount += GetWordCount(text) - GetWordCount(old);
                _byteCount += text.Length - old.Length;
                Scrolls[key] = text;
            }
            else
            {
                Scrolls[key] = text;
                ++LeafCount;
                WordCount += GetWordCount(text);
                _byteCount += text.Length;
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
            SetAt(Coords, text);
        }

        public string getScroll()
        {
            return GetAt(Coords);
        }

        // The editor's cursor cascade, unchanged from the vendored
        // implementation: the first non-zero delta component wins, the
        // moved component floors at 1 (the Section and Scroll cases
        // historically do not floor), and every lower component
        // resets to 1. Nothing carries into a higher dimension.
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

        public string EditorSummary(string action = "")
        {
            return Coords.EditorSummary(action);
        }
    }
}
