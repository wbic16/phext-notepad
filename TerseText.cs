namespace TerseNotepad
{
    public class PagedText : SortedDictionary<uint, string> { };
    public class GroupedText : SortedDictionary<uint, PagedText> { };
    public class SettedText : SortedDictionary<uint, GroupedText> { };
    public class TerseText
    {
        public SettedText Set = new();
        public GroupedText Group
        {
            get
            {
                if (!Set.ContainsKey(Coords.Set))
                {
                    Set[Coords.Set] = new();
                }
                return Set[Coords.Set];
            }
            set
            {
                Set[Coords.Set] = value;
            }
        }
        public PagedText Page
        {
            get
            {
                if (!Group.ContainsKey(Coords.Group))
                {
                    Group[Coords.Group] = new();
                }
                return Group[Coords.Group];
            }
            set
            {
                Group[Coords.Group] = value;
            }
        }
        public class Coordinates
        {
            public uint Column { get; set; } = 1;
            public uint Line { get; set; } = 1;
            public uint Page { get; set; } = 1;
            public uint Group { get; set; } = 1;
            public uint Set { get; set; } = 1;
            public uint Volume { get; set; } = 1;
            public uint Branch { get; set; } = 1;
            public uint Language { get; set; } = 1;
            public uint World { get; set; } = 1;
            public uint Galaxy { get; set; } = 1;
            public uint Multiverse { get; set; } = 1;            

            public override string ToString()
            {
                return $"Multiverse: {Multiverse},  Galaxy: {Galaxy},  World: {World},  Language: {Language},  Branch: {Branch},  Volume: {Volume},  Set: {Set},  Group:  {Group},  Page: {Page},  Line: {Line},  Column: {Column}";
            }

            public string EditorSummary(int length)
            {
                return $"Line: {Line},  Column: {Column},  Bytes: {length}";
            }

            public void Reset()
            {
                Column = 1;
                Line = 1;
                Page = 1;
                Group = 1;
                Set = 1;
                Volume = 1;
                Branch = 1;
                Language = 1;
                World = 1;
                Galaxy = 1;
                Multiverse = 1;
            }
        };
        public Coordinates Coords = new Coordinates();

        public void setPageText(string text)
        {
            Page[Coords.Page] = text;
        }

        public void processDelta(int set_delta, int group_delta, int page_delta)
        {
            if (page_delta > 0) { ++Coords.Page; }
            if (page_delta < 0) { --Coords.Page; }
            if (group_delta > 0) { ++Coords.Group; }
            if (group_delta < 0) { --Coords.Group; }
            if (set_delta > 0) { ++Coords.Set; }
            if (set_delta < 0) { --Coords.Set; }
            if (Coords.Page < 1) { Coords.Page = 1; }
            if (Coords.Group < 1) { Coords.Group = 1; }
            if (Coords.Set < 1) { Coords.Set = 1; }
        }

        public string getPage()
        {
            return Page.ContainsKey(Coords.Page) ? Page[Coords.Page] : "";
        }

        public string EditorSummary()
        {
            return Coords.EditorSummary(getPage().Length);
        }
    }
}