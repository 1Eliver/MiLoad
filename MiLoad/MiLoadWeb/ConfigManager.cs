using Newtonsoft.Json;

namespace MiLoadWeb;

public class ConfigManager
{
    private readonly Models.Config _configFirst;
    
    public Models.Config Config { get; set; }
    
    public ConfigManager()
    {
        InitConfigFile();

        _configFirst =
            JsonConvert.DeserializeObject<Models.Config>(File.ReadAllText(Path.Join(GetConfigPath(), "conf.json"))) ??
            new Models.Config
            {
                Plugins = new List<Models.PluginItem>(),
                Libs = new List<Models.LibItem>(),
                Poor = new Models.DownloadPoor()
            };

        Config = _configFirst;
    }
    
    private static void InitConfigFile()
    {
        // 先创建配置文件目录
        Directory.CreateDirectory(GetConfigPath());
        
        // 检测配置文件是否存在，不存在则创建
        if (!File.Exists(Path.Join(GetConfigPath(), "conf.json")))
        {
            File.Create(Path.Join(GetConfigPath(), "conf.json"));
        }
    }

    private static string GetConfigPath()
    {
        return Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "miL");
    }
    
    ~ConfigManager()
    {
        if (_configFirst.Equals(Config)) return;
        Config.Poor?.Poor?.Clear();
        Save();
    }

    private void Save()
    {
        File.WriteAllText(Path.Join(GetConfigPath(), "conf.json"), JsonConvert.SerializeObject(Config));
    }
}