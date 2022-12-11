using System.Runtime;

namespace TerseNotepad
{
    public class TerseConfig
    {
        private readonly string _configFilename = "";

        public string IniFilePath
        {
            get
            {
                return _configFilename;
            }
        }
        public TerseConfig()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var terseFolder = Path.Combine(appData, "TerseNotepad");
            if (!Directory.Exists(terseFolder))
            {
                Directory.CreateDirectory(terseFolder);
            }
            _configFilename = Path.Combine(terseFolder, "terse.ini");
            Reload();
        }

        public void Reload()
        {
            if (File.Exists(_configFilename))
            {
                var text = File.ReadAllText(_configFilename);
                Deserialize(text);                
            }
        }
        public string Format { get; set; } = "TerseConfig";
        public string Version { get; private set; } = "4";
        private string _filename = "";
        public string Filename
        {
            get
            {
                return _filename;
            }
            set
            {
                var index = -1;
                foreach (var ii in RecentFile.Keys)
                {
                    if (RecentFile[ii] == value)
                    {
                        index = ii;
                        break;
                    }
                }
                if (index > 0)
                {
                    RecentFile.Remove(index);
                }
                index = RecentFile.Keys.Count > 0 ? RecentFile.Keys.Max() : -1;
                RecentFile[index + 1] = value;
                _filename = value;
            }
        }
        public string Coords { get; set; } = "";
        public bool TreeView { get; set; } = true;
        public string Font { get; set; } = "Cascadia Code";
        public int FontSize { get; set; } = 11;
        public string LastError { get; set; } = "";
        public string Dimension1 { get; set; } = "Column";
        public string Dimension2 { get; set; } = "Line";
        public string Dimension3 { get; set; } = "Scroll";
        public string Dimension4 { get; set; } = "Section";
        public string Dimension5 { get; set; } = "Chapter";
        public string Dimension6 { get; set; } = "Book";
        public string Dimension7 { get; set; } = "Volume";
        public string Dimension8 { get; set; } = "Collection";
        public string Dimension9 { get; set; } = "Series";
        public string Dimension10 { get; set; } = "Shelf";
        public string Dimension11 { get; set; } = "Library";
        public bool WordWrap { get; set; } = true;
        public float ZoomFactor { get; set; } = 1.0f;
        public SortedDictionary<int, string> RecentFile { get; set; } = new();
        public string Theme { get; set; } = "Dark";
        public bool DarkMode { get { return Theme == "Dark"; } }
        public bool LightMode { get { return Theme == "Light"; } }

        public bool VimMode { get; set; } = false;

        public string Serialize()
        {
            var result = $"[TerseConfig]\n"
                 + $"Format = {Format}\n"
                 + $"Version = {Version}\n"
                 + $"Filename = {Filename}\n"
                 + $"TreeView = {TreeView}\n"
                 + $"Coords = {Coords}\n"
                 + $"Font = {Font}\n"
                 + $"FontSize = {FontSize}\n"
                 + $"LastError = {LastError}\n"
                 + $"Dimension1 = {Dimension1}\n"
                 + $"Dimension2 = {Dimension2}\n"
                 + $"Dimension3 = {Dimension3}\n"
                 + $"Dimension4 = {Dimension4}\n"
                 + $"Dimension5 = {Dimension5}\n"
                 + $"Dimension6 = {Dimension6}\n"
                 + $"Dimension7 = {Dimension7}\n"
                 + $"Dimension8 = {Dimension8}\n"
                 + $"Dimension9 = {Dimension9}\n"
                 + $"Dimension10 = {Dimension10}\n"
                 + $"Dimension11 = {Dimension11}\n"
                 + $"WordWrap = {WordWrap}\n"
                 + $"ZoomFactor = {ZoomFactor}\n"
                 + $"Theme = {Theme}\n"
                 + $"VimMode = {VimMode}\n";
            foreach (var key in RecentFile.Keys.OrderByDescending(q => q))
            {
                var file = RecentFile[key];
                result += $"RecentFile = {file}\n";
            }
            return result;
        }

        public void Deserialize(string ini)
        {
            var lines = ini.Split('\n');
            var fileOrdering = 0;
            if (lines[0] == "[TerseConfig]")
            {
                foreach (var line in lines)
                {
                    var parts = line.Split(" = ");
                    switch (parts[0])
                    {
                        case "Format":
                            Format = parts[1];
                            break;
                        case "Version":
                            Version = parts[1];
                            break;
                        case "Filename":
                            Filename = parts[1];
                            break;
                        case "TreeView":
                            TreeView = parts[1] == "True";
                            break;
                        case "Coords":
                            Coords = parts[1];
                            break;
                        case "Font":
                            Font = parts[1];
                            break;
                        case "FontSize":
                            try
                            {
                                FontSize = int.Parse(parts[1]);
                            }
                            catch { }
                            break;
                        case "LastError":
                            LastError = parts[1];
                            break;
                        case "Dimension1":
                            Dimension1 = parts[1];
                            break;
                        case "Dimension2":
                            Dimension2 = parts[1];
                            break;
                        case "Dimension3":
                            Dimension3 = parts[1];
                            break;
                        case "Dimension4":
                            Dimension4 = parts[1];
                            break;
                        case "Dimension5":
                            Dimension5 = parts[1];
                            break;
                        case "Dimension6":
                            Dimension6 = parts[1];
                            break;
                        case "Dimension7":
                            Dimension7 = parts[1];
                            break;
                        case "Dimension8":
                            Dimension8 = parts[1];
                            break;
                        case "Dimension9":
                            Dimension9 = parts[1];
                            break;
                        case "Dimension10":
                            Dimension10 = parts[1];
                            break;
                        case "Dimension11":
                            Dimension11 = parts[1];
                            break;
                        case "WordWrap":
                            WordWrap = parts[1] == "True";
                            break;
                        case "ZoomFactor":
                            try
                            {
                                ZoomFactor = float.Parse(parts[1]);
                            }
                            catch { }
                            break;
                        case "RecentFile":
                            if (!RecentFile.ContainsValue(parts[1]))
                            {
                                RecentFile[fileOrdering++] = parts[1];
                            }
                            break;
                        case "Theme":
                            Theme = parts[1];
                            break;
                        case "VimMode":
                            VimMode = parts[1] == "True";
                            break;
                    }
                }
            }
        }

        public void Save()
        {
            var ini = Serialize();
            File.WriteAllText(_configFilename, ini);
        }
    }
}
