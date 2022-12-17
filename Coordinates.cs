namespace TerseNotepad
{
    public class Coordinates
    {
        public Coordinates()
        {
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
        public int Column { get; set; } = 0;
        public int Line { get; set; } = 0;
        public short Scroll { get; set; } = 0;
        public short Section { get; set; } = 0;
        public short Chapter { get; set; } = 0;
        public short Book { get; set; } = 0;
        public short Volume { get; set; } = 0;
        public short Collection { get; set; } = 0;
        public short Series { get; set; } = 0;
        public short Shelf { get; set; } = 0;
        public short Library { get; set; } = 0;

        public override string ToString()
        {
            return ToString(this);
        }
        public static string ToString(Coordinates c)
        {
            return $"p{c.Scroll}-g{c.Section}-s{c.Chapter}-y{c.Book}-h{c.Volume}-e{c.Collection}-w{c.Series}-i{c.Shelf}-m{c.Library}";
        }
        public Coordinates GetLibraryRoot()
        {
            Coordinates view = this;
            view.Library = 0;
            return view;
        }
        public Coordinates GetShelfRoot()
        {
            Coordinates view = this;
            view.Library = 0;
            view.Shelf = 0;
            return view;
        }
        public Coordinates GetSeriesRoot()
        {
            Coordinates view = this;
            view.Library = 0;
            view.Shelf = 0;
            view.Series = 0;
            return view;
        }
        public Coordinates GetVolumeRoot()
        {
            Coordinates view = this;
            view.Library = 0;
            view.Shelf = 0;
            view.Series = 0;
            view.Collection = 0;
            return view;
        }
        public Coordinates GetBookRoot()
        {
            Coordinates view = this;
            view.Library = 0;
            view.Shelf = 0;
            view.Series = 0;
            view.Collection = 0;
            view.Volume = 0;
            return view;
        }
        public Coordinates GetChapterRoot()
        {
            Coordinates view = this;
            view.Library = 0;
            view.Shelf = 0;
            view.Series = 0;
            view.Collection = 0;
            view.Volume = 0;
            view.Book = 0;
            return view;
        }
        public Coordinates GetSectionRoot()
        {
            Coordinates view = this;
            view.Library = 0;
            view.Shelf = 0;
            view.Series = 0;
            view.Collection = 0;
            view.Volume = 0;
            view.Book = 0;
            view.Chapter = 0;
            return view;
        }
        public Coordinates GetScrollRoot()
        {
            Coordinates view = this;
            view.Library = 0;
            view.Shelf = 0;
            view.Series = 0;
            view.Collection = 0;
            view.Volume = 0;
            view.Book = 0;
            view.Chapter = 0;
            view.Section = 0;
            return view;
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
            Scroll = short.Parse(parts[0]);
            Section = short.Parse(parts[1]);
            Chapter = short.Parse(parts[2]);
            Book = short.Parse(parts[3]);
            Volume = short.Parse(parts[4]);
            Collection = short.Parse(parts[5]);
            Series = short.Parse(parts[6]);
            Shelf = short.Parse(parts[7]);
            Library = short.Parse(parts[8]);
        }

        public bool IsValid()
        {
            return Chapter >= 1 && Section >= 1 && Scroll >= 1 &&
                   Book >= 1 && Volume >= 1 && Collection >= 1 &&
                   Series >= 1 && Shelf >= 1 && Library >= 1;
        }
    }
}
