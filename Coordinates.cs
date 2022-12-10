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
        public uint Column { get; set; } = 1;
        public uint Line { get; set; } = 1;
        public uint Scroll { get; set; } = 1;
        public uint Section { get; set; } = 1;
        public uint Chapter { get; set; } = 1;
        public uint Book { get; set; } = 1;
        public uint Volume { get; set; } = 1;
        public uint Collection { get; set; } = 1;
        public uint Series { get; set; } = 1;
        public uint Shelf { get; set; } = 1;
        public uint Library { get; set; } = 1;

        public override string ToString()
        {
            return $"{Scroll}-{Section}-{Chapter}";
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
            // TODO: Support Book through Library
            var parts = coordinates.Split('-', 3);
            if (parts.Length != 3)
            {
                return;
            }
            Scroll = uint.Parse(parts[0]);
            Section = uint.Parse(parts[1]);
            Chapter = uint.Parse(parts[2]);
        }

        public bool IsValid()
        {
            return Chapter >= 1 && Section >= 1 && Scroll >= 1;
        }
    }
}
