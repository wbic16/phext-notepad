namespace TerseNotepad
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
        }
        public class ScrollIndex : TypedCoordinate
        {
            public ScrollIndex(short value) : base(value)
            {
            }
            public static implicit operator ScrollIndex(short value)
            {
                return new ScrollIndex(value);
            }
        }
        public class SectionIndex : TypedCoordinate
        {
            public SectionIndex(short value) : base(value)
            {
            }
            public static implicit operator SectionIndex(short value)
            {
                return new SectionIndex(value);
            }
        }
        public class ChapterIndex : TypedCoordinate
        {
            public ChapterIndex(short value) : base(value)
            {
            }

            public static implicit operator ChapterIndex(short value)
            {
                return new ChapterIndex(value);
            }
        }
        public class BookIndex : TypedCoordinate
        {
            public BookIndex(short value) : base(value)
            {
            }
            public static implicit operator BookIndex(short value)
            {
                return new BookIndex(value);
            }
        }
        public class VolumeIndex : TypedCoordinate
        {
            public VolumeIndex(short value) : base(value)
            {
            }
            public static implicit operator VolumeIndex(short value)
            {
                return new VolumeIndex(value);
            }
        }
        public class CollectionIndex : TypedCoordinate
        {
            public CollectionIndex(short value) : base(value)
            {
            }
            public static implicit operator CollectionIndex(short value)
            {
                return new CollectionIndex(value);
            }
        }
        public class SeriesIndex : TypedCoordinate
        {
            public SeriesIndex(short value) : base(value)
            {
            }
            public static implicit operator SeriesIndex(short value)
            {
                return new SeriesIndex(value);
            }
        }
        public class ShelfIndex : TypedCoordinate
        {
            public ShelfIndex(short value) : base(value)
            {
            }
            public static implicit operator ShelfIndex(short value)
            {
                return new ShelfIndex(value);
            }
        }
        public class LibraryIndex : TypedCoordinate
        {
            public LibraryIndex(short value) : base(value)
            {
            }
            public static implicit operator LibraryIndex(short value)
            {
                return new LibraryIndex(value);
            }
        }

        public static readonly short SZERO = 0;

        public int Column { get; set; } = 0;
        public int Line { get; set; } = 0;
        public ScrollIndex Scroll { get; set; } = 0;
        public SectionIndex Section { get; set; } = 0;
        public ChapterIndex Chapter { get; set; } = 0;
        public BookIndex Book { get; set; } = 0;
        public VolumeIndex Volume { get; set; } = 0;
        public CollectionIndex Collection { get; set; } = 0;
        public SeriesIndex Series { get; set; } = 0;
        public ShelfIndex Shelf { get; set; } = 0;
        public LibraryIndex Library { get; set; } = 0;

        public override string ToString()
        {
            return ToString(this);
        }
        public static string ToString(Coordinates c)
        {
            return $"p{c.Scroll}-g{c.Section}-s{c.Chapter}-y{c.Book}-h{c.Volume}-e{c.Collection}-w{c.Series}-i{c.Shelf}-m{c.Library}";
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
            var result = $"Ln: {Line}, Col: {Column}";
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

        public void Load(string coordinates)
        {
            var parts = coordinates.Split('-', 9);
            if (parts.Length != 9)
            {
                return;
            }
            Scroll = short.Parse(parts[0].Skip(1).ToArray());
            Section = short.Parse(parts[1].Skip(1).ToArray());
            Chapter = short.Parse(parts[2].Skip(1).ToArray());
            Book = short.Parse(parts[3].Skip(1).ToArray());
            Volume = short.Parse(parts[4].Skip(1).ToArray());
            Collection = short.Parse(parts[5].Skip(1).ToArray());
            Series = short.Parse(parts[6].Skip(1).ToArray());
            Shelf = short.Parse(parts[7].Skip(1).ToArray());
            Library = short.Parse(parts[8].Skip(1).ToArray());
        }

        public bool IsValid()
        {
            return Chapter >= 1 && Section >= 1 && Scroll >= 1 &&
                   Book >= 1 && Volume >= 1 && Collection >= 1 &&
                   Series >= 1 && Shelf >= 1 && Library >= 1;
        }

        public bool HasDelta()
        {
            return Chapter >= 1 || Section >= 1 || Scroll >= 1 ||
                   Book >= 1 || Volume >= 1 || Collection >= 1 ||
                   Series >= 1 || Shelf >= 1 || Library >= 1;
        }
    }
}
