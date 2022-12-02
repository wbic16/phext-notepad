using static System.Collections.Specialized.BitVector32;

namespace TerseNotepad
{
    public class ScrollText : SortedDictionary<uint, string> { };
    public class SectionText : SortedDictionary<uint, ScrollText> { };
    public class ChapterText : SortedDictionary<uint, SectionText> { };
    public class TerseText
    {
        public ChapterText Chapter = new();
        public SectionText Section
        {
            get
            {
                if (!Chapter.ContainsKey(Coords.Chapter))
                {
                    Chapter[Coords.Chapter] = new();
                }
                return Chapter[Coords.Chapter];
            }
            set
            {
                Chapter[Coords.Chapter] = value;
            }
        }
        public ScrollText Scroll
        {
            get
            {
                if (!Section.ContainsKey(Coords.Section))
                {
                    Section[Coords.Section] = new();
                }
                return Section[Coords.Section];
            }
            set
            {
                Section[Coords.Section] = value;
            }
        }
        public class Coordinates
        {
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
                return $"Library: {Library},  Shelf: {Shelf},  Series: {Series}," +
                    $"  Collection: {Collection},  Volume: {Volume},  Book: {Book}," +
                    $"  Chapter: {Chapter},  Section:  {Section},  Scroll: {Scroll}," +
                    $"  Line: {Line},  Column: {Column}";
            }

            public string EditorSummary()
            {
                return $"Ln: {Line}, Col: {Column}";
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
        }
        public Coordinates Coords = new Coordinates();

        public void setScrollText(string text)
        {
            if (text.Length > 0)
            {
                Scroll[Coords.Scroll] = text;
            }
            // note: the key checks here optimize performance on sparse files
            if (text.Length == 0 &&
                Chapter.ContainsKey(Coords.Chapter) &&
                Section.ContainsKey(Coords.Section) &&
                Scroll.ContainsKey(Coords.Scroll))
            {
                Scroll.Remove(Coords.Scroll);
            }
        }

        public void processDelta(int chapter_delta, int section_delta, int scroll_delta)
        {
            if (scroll_delta > 0) { ++Coords.Scroll; }
            if (scroll_delta < 0) { --Coords.Scroll; }
            if (section_delta > 0) { ++Coords.Section; }
            if (section_delta < 0) { --Coords.Section; }
            if (chapter_delta > 0) { ++Coords.Chapter; }
            if (chapter_delta < 0) { --Coords.Chapter; }
            if (Coords.Scroll < 1) { Coords.Scroll = 1; }
            if (Coords.Section < 1) { Coords.Section = 1; }
            if (Coords.Chapter < 1) { Coords.Chapter = 1; }
        }

        public string getScroll()
        {
            return Scroll.ContainsKey(Coords.Scroll) ? Scroll[Coords.Scroll] : "";
        }

        public string EditorSummary()
        {
            return Coords.EditorSummary();
        }
    }
}