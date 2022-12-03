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
            if (!File.Exists(_configFilename))
            {
                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                Filename = Path.Combine(desktop, "Terse.t");
            }
            else
            {
                var text = File.ReadAllText(_configFilename);
                Deserialize(text);                
            }
        }
        public string Format { get; set; } = "TerseConfig";
        public string Version { get; set; } = "1";
        public string Filename { get; set; } = "";
        public string Coords { get; set; } = "";
        public bool TreeView { get; set; } = true;

        public string Serialize()
        {
            return $"[TerseConfig]\n"
                 + $"Format = {Format}\n"
                 + $"Version = {Version}\n"
                 + $"Filename = {Filename}\n"
                 + $"TreeView = {TreeView}\n"
                 + $"Coords = {Coords}\n";
        }

        public void Deserialize(string ini)
        {
            var lines = ini.Split('\n');
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
