namespace MiLoadWeb;

public class Models
{
    public class Config
    {
        public List<PluginItem>? Plugins { get; set; }
        public List<LibItem>? Libs { get; set; }
        public DownloadPoor? Poor { get; set; }
    }

    public class PluginItem
    {
        public string? PluginName { get; set; }
        public string? PluginVersion { get; set; }
        public string? PluginLocalPath { get; set; }
        public List<string>? PluginLibIds { get; set; }
        public string? PluginId { get; set; }
    }

    public class LibItem
    {
        public string? LibName { get; set; }
        public string? LibVersion { get; set; }
        public string? LibLocalPath { get; set; }
        public string? LibId { get; set; }
    }

    public class DownloadPoor
    {
        public Dictionary<string, string>? Poor;

        public string Push(string path)
        {
            var id = Guid.NewGuid().ToString();
            Poor?.Add(id, path);
            return id;
        }

        public string? Pop(string id)
        {
            if (Poor != null && Poor.Remove(id, out var outPath))
            {
                return outPath;
            }
            return null;
        }
    }
    
    
}