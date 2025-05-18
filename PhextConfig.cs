using System.Runtime;

namespace PhextNotepad
{
    public class PhextConfig
    {
        private readonly string _configFilename = "";

        public string IniFilePath
        {
            get
            {
                return _configFilename;
            }
        }
        public PhextConfig()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var phextFolder = Path.Combine(appData, "PhextNotepad");
            if (!Directory.Exists(phextFolder))
            {
                Directory.CreateDirectory(phextFolder);
            }
            _configFilename = Path.Combine(phextFolder, "PhextNotepad.ini");
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
        public string Format { get; set; } = "PhextConfig";
        public string Version { get; private set; } = "5";
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

        public Color Color1 { get; set; } = Color.Black;
        public Color Color2 { get; set; } = Color.White;
        public Color Color3 { get; set; } = Color.DeepSkyBlue;
        public Color Color4 { get; set; } = Color.DarkGray;
        public bool ShowCoordinates { get; set; } = true;

        private string SerializeColor(Color color)
        {
            var result = ColorTranslator.ToHtml(color);
            return result;
        }
        private Color DeserializeColor(string color, Color fallback)
        {
            Color result;
            try
            {
                result = ColorTranslator.FromHtml(color);
            }
            catch
            {
                result = fallback;
            }
            return result;
        }
        public string Serialize()
        {
            var result = $"[PhextConfig]\n"
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
                 + $"Color1 = {SerializeColor(Color1)}\n"
                 + $"Color2 = {SerializeColor(Color2)}\n"
                 + $"Color3 = {SerializeColor(Color3)}\n"
                 + $"Color4 = {SerializeColor(Color4)}\n"
                 + $"ShowCoordinates = {ShowCoordinates}\n";
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
            if (lines[0] == "[PhextConfig]")
            {
                foreach (var line in lines)
                {
                    var parts = line.Split(" = ");
                    var value = parts.Length > 1 ? parts[1] : "";
                    switch (parts[0])
                    {
                        case "Format":
                            Format = value;
                            break;
                        case "Version":
                            Version = value;
                            break;
                        case "Filename":
                            Filename = value;
                            break;
                        case "TreeView":
                            TreeView = value == "True";
                            break;
                        case "Coords":
                            Coords = value;
                            break;
                        case "Font":
                            Font = value;
                            break;
                        case "FontSize":
                            try
                            {
                                FontSize = int.Parse(value);
                            }
                            catch { }
                            break;
                        case "LastError":
                            LastError = value;
                            break;
                        case "Dimension1":
                            Dimension1 = value;
                            break;
                        case "Dimension2":
                            Dimension2 = value;
                            break;
                        case "Dimension3":
                            Dimension3 = value;
                            break;
                        case "Dimension4":
                            Dimension4 = value;
                            break;
                        case "Dimension5":
                            Dimension5 = value;
                            break;
                        case "Dimension6":
                            Dimension6 = value;
                            break;
                        case "Dimension7":
                            Dimension7 = value;
                            break;
                        case "Dimension8":
                            Dimension8 = value;
                            break;
                        case "Dimension9":
                            Dimension9 = value;
                            break;
                        case "Dimension10":
                            Dimension10 = value;
                            break;
                        case "Dimension11":
                            Dimension11 = value;
                            break;
                        case "WordWrap":
                            WordWrap = value == "True";
                            break;
                        case "ZoomFactor":
                            try
                            {
                                ZoomFactor = float.Parse(value);
                            }
                            catch { }
                            break;
                        case "RecentFile":
                            if (!RecentFile.ContainsValue(value))
                            {
                                RecentFile[fileOrdering++] = value;
                            }
                            break;
                        case "Theme":
                            Theme = value;
                            break;
                        case "Color1":
                            Color1 = DeserializeColor(value, Color.Black);
                            break;
                        case "Color2":
                            Color2 = DeserializeColor(value, Color.White);
                            break;
                        case "Color3":
                            Color3 = DeserializeColor(value, Color.DeepSkyBlue);
                            break;
                        case "Color4":
                            Color4 = DeserializeColor(value, Color.DarkGray);
                            break;
                        case "ShowCoordinates":
                            ShowCoordinates = value == "True";
                            break;
                    }
                }
            }

            // Todo: validate color differences to prevent unreadable text
        }

        public void Save()
        {
            var ini = Serialize();
            File.WriteAllText(_configFilename, ini);
        }
    }
}
