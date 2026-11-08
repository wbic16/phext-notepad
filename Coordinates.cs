namespace PhextNotepad
{
    public class Coordinates
    {
        public Coordinates(bool reset = false)
        {
            if (reset) { Reset(); }
        }

        public Coordinates(string coordinates)
        {
            Load(coordinates);
        }
        public Coordinates(Coordinates other)
        {
            Column = other.Column;
            Line = other.Line;
            Scroll = other.Scroll;
            Section = other.Section;
            Chapter = other.Chapter;
            Book = other.Book;
            Volume = other.Volume;
            Collection = other.Collection;
            Series = other.Series;
            Shelf = other.Shelf;
            Library = other.Library;
            Intermediate = other.Intermediate;
        }

        public void LibraryBreak()
        {
            Library++;
            Shelf = 1;
            Series = 1;
            Collection = 1;
            Volume = 1;
            Book = 1;
            Chapter = 1;
            Section = 1;
            Scroll = 1;
        }

        public void ShelfBreak()
        {
            Shelf++;
            Series = 1;
            Collection = 1;
            Volume = 1;
            Book = 1;
            Chapter = 1;
            Section = 1;
            Scroll = 1;
        }

        public void SeriesBreak()
        {
            Series++;
            Collection = 1;
            Volume = 1;
            Book = 1;
            Chapter = 1;
            Section = 1;
            Scroll = 1;
        }

        public void CollectionBreak()
        {
            Collection++;
            Volume = 1;
            Book = 1;
            Chapter = 1;
            Section = 1;
            Scroll = 1;
        }

        public void VolumeBreak()
        {
            Volume++;
            Book = 1;
            Chapter = 1;
            Section = 1;
            Scroll = 1;
        }

        public void BookBreak()
        {
            Book++;
            Chapter = 1;
            Section = 1;
            Scroll = 1;
        }

        public void ChapterBreak()
        {
            Chapter++;
            Section = 1;
            Scroll = 1;
        }

        public void SectionBreak()
        {
            Section++;
            Scroll = 1;
        }

        public void ScrollBreak()
        {
            Scroll++;
        }
        public bool Intermediate { get; set; } = false;
        public class ScrollIndex : TypedCoordinate
        {
            public ScrollIndex(int value) : base(value)
            {
            }
            public static implicit operator ScrollIndex(int value)
            {
                return new ScrollIndex(value);
            }
        }
        public class SectionIndex : TypedCoordinate
        {
            public SectionIndex(int value) : base(value)
            {
            }
            public static implicit operator SectionIndex(int value)
            {
                return new SectionIndex(value);
            }
        }
        public class ChapterIndex : TypedCoordinate
        {
            public ChapterIndex(int value) : base(value)
            {
            }

            public static implicit operator ChapterIndex(int value)
            {
                return new ChapterIndex(value);
            }
        }
        public class BookIndex : TypedCoordinate
        {
            public BookIndex(int value) : base(value)
            {
            }
            public static implicit operator BookIndex(int value)
            {
                return new BookIndex(value);
            }
        }
        public class VolumeIndex : TypedCoordinate
        {
            public VolumeIndex(int value) : base(value)
            {
            }
            public static implicit operator VolumeIndex(int value)
            {
                return new VolumeIndex(value);
            }
        }
        public class CollectionIndex : TypedCoordinate
        {
            public CollectionIndex(int value) : base(value)
            {
            }
            public static implicit operator CollectionIndex(int value)
            {
                return new CollectionIndex(value);
            }
        }
        public class SeriesIndex : TypedCoordinate
        {
            public SeriesIndex(int value) : base(value)
            {
            }
            public static implicit operator SeriesIndex(int value)
            {
                return new SeriesIndex(value);
            }
        }
        public class ShelfIndex : TypedCoordinate
        {
            public ShelfIndex(int value) : base(value)
            {
            }
            public static implicit operator ShelfIndex(int value)
            {
                return new ShelfIndex(value);
            }
        }
        public class LibraryIndex : TypedCoordinate
        {
            public LibraryIndex(int value) : base(value)
            {
            }
            public static implicit operator LibraryIndex(int value)
            {
                return new LibraryIndex(value);
            }
        }

        public static readonly int SZERO = 0;

        public int Column { get; set; } = 1;
        public int Line { get; set; } = 1;
        public ScrollIndex Scroll { get; set; } = 1;
        public SectionIndex Section { get; set; } = 1;
        public ChapterIndex Chapter { get; set; } = 1;
        public BookIndex Book { get; set; } = 1;
        public VolumeIndex Volume { get; set; } = 1;
        public CollectionIndex Collection { get; set; } = 1;
        public SeriesIndex Series { get; set; } = 1;
        public ShelfIndex Shelf { get; set; } = 1;
        public LibraryIndex Library { get; set; } = 1;

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
            var strings = coordinates.Replace('.','/').Split('/', StringSplitOptions.RemoveEmptyEntries);
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

        public bool IsValid()
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
