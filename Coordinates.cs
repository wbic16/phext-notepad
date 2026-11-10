using Phext;

namespace PhextNotepad
{
    // The editor's coordinate. The coordinate itself — the nine
    // components, the break arithmetic, ordering, equality, and the
    // canonical text form — is libphext-cs's Phext.Coordinate, consumed
    // from the NuGet package. What remains in this class is only what
    // the library does not carry because it belongs to this editor:
    //
    // - the text caret (Line/Column) and the coordinate box's
    //   Intermediate flag;
    // - group-root coordinates with zeroed lower dimensions, which the
    //   tree uses as node names. The library's ToString declines to
    //   format a coordinate with a component below 1 (it returns ""),
    //   so the override below formats every component unconditionally,
    //   exactly as the vendored implementation did. For any coordinate
    //   the library would format, the output is identical.
    // - IsValid with the editor's semantics: a group root (zeroed
    //   lower dimensions) is a valid navigation target, so components
    //   may be 0. The library's IsValid requires every component >= 1.
    // - the lenient Load the coordinate box has always used (extract
    //   the integers, require exactly nine), the status-line summaries,
    //   and Clamp.
    public class Coordinates : Coordinate
    {
        public Coordinates(bool reset = false)
        {
            if (reset) { Reset(); }
        }

        public Coordinates(string coordinates)
        {
            Load(coordinates);
        }

        public Coordinates(Coordinate other) : base(other)
        {
        }

        public Coordinates(Coordinates other) : base(other)
        {
            Column = other.Column;
            Line = other.Line;
            Intermediate = other.Intermediate;
        }

        public bool Intermediate { get; set; } = false;

        public static readonly int SZERO = 0;

        public int Column { get; set; } = 1;
        public int Line { get; set; } = 1;

        public override string ToString()
        {
            return ToString(this);
        }

        public static int CoordinateClamp(int value)
        {
            return (value < 1) ? 1 : value;
        }

        public static string ToString(Coordinates c)
        {
            return $"{c.Library}.{c.Shelf}.{c.Series}/{c.Collection}.{c.Volume}.{c.Book}/{c.Chapter}.{c.Section}.{c.Scroll}";
        }

        public Coordinates GetRoot()
        {
            return new Coordinates();
        }

        public Coordinates GetLibraryRoot()
        {
            return new Coordinates(this)
            {
                Scroll = 0,
                Section = 0,
                Chapter = 0,
                Book = 0,
                Volume = 0,
                Collection = 0,
                Series = 0,
                Shelf = 0
            };
        }

        public Coordinates GetShelfRoot()
        {
            return new Coordinates(this)
            {
                Scroll = 0,
                Section = 0,
                Chapter = 0,
                Book = 0,
                Volume = 0,
                Collection = 0,
                Series = 0
            };
        }

        public Coordinates GetSeriesRoot()
        {
            return new Coordinates(this)
            {
                Scroll = 0,
                Section = 0,
                Chapter = 0,
                Book = 0,
                Volume = 0,
                Collection = 0
            };
        }

        public Coordinates GetCollectionRoot()
        {
            return new Coordinates(this)
            {
                Scroll = 0,
                Section = 0,
                Chapter = 0,
                Book = 0,
                Volume = 0
            };
        }

        public Coordinates GetVolumeRoot()
        {
            return new Coordinates(this)
            {
                Scroll = 0,
                Section = 0,
                Chapter = 0,
                Book = 0
            };
        }

        public Coordinates GetBookRoot()
        {
            return new Coordinates(this)
            {
                Scroll = 0,
                Section = 0,
                Chapter = 0
            };
        }

        public Coordinates GetChapterRoot()
        {
            return new Coordinates(this)
            {
                Scroll = 0,
                Section = 0
            };
        }

        public Coordinates GetSectionRoot()
        {
            return new Coordinates(this)
            {
                Scroll = 0
            };
        }

        public Coordinates GetScrollRoot()
        {
            return new Coordinates(this);
        }

        public string GetNodeSummary()
        {
            return $"Scroll #{Scroll}";
        }

        public string EditorSummary(string action = "")
        {
            var result = $"LB: {Library}  SF: {Shelf}  SR: {Series}  /  CN: {Collection}  VM: {Volume}  BK: {Book}  /  CH: {Chapter}  SN: {Section}  SC: {Scroll}  -- Line: {Line}, Column: {Column}";
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

        private int[] ParseCoordinateString(string coordinates)
        {
            var strings = coordinates.Replace('.', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            var result = new List<int>();
            foreach (var s in strings)
            {
                if (int.TryParse(s, out int parsed))
                {
                    result.Add(parsed);
                }
            }
            return result.ToArray();
        }

        public void Load(string coordinates)
        {
            var parts = ParseCoordinateString(coordinates);
            if (parts.Length != 9)
            {
                Intermediate = true;
                return;
            }
            Library = parts[0];
            Shelf = parts[1];
            Series = parts[2];
            Collection = parts[3];
            Volume = parts[4];
            Book = parts[5];
            Chapter = parts[6];
            Section = parts[7];
            Scroll = parts[8];
            Intermediate = false;
        }

        public new bool IsValid()
        {
            return Chapter >= 0 && Section >= 0 && Scroll >= 0 &&
                   Book >= 0 && Volume >= 0 && Collection >= 0 &&
                   Series >= 0 && Shelf >= 0 && Library >= 0;
        }

        public bool HasDelta()
        {
            return Chapter != 0 || Section != 0 || Scroll != 0 ||
                   Book != 0 || Volume != 0 || Collection != 0 ||
                   Series != 0 || Shelf != 0 || Library != 0;
        }

        public Coordinates Clamp()
        {
            return new Coordinates()
            {
                Scroll = CoordinateClamp(Scroll),
                Section = CoordinateClamp(Section),
                Chapter = CoordinateClamp(Chapter),
                Book = CoordinateClamp(Book),
                Collection = CoordinateClamp(Collection),
                Volume = CoordinateClamp(Volume),
                Series = CoordinateClamp(Series),
                Shelf = CoordinateClamp(Shelf),
                Library = CoordinateClamp(Library)
            };
        }
    }
}
