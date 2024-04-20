using System.Reflection;

namespace MiLoadWeb;

public class PluginManager
{
    public PluginManager()
    {
        LoadPluginFolder();
        UseWatcher();
    }
    
    private FileSystemWatcher? _watcher;

    private ConfigManager _configManager = new ConfigManager();
    
    private static void LoadPluginFolder()
    {
        Directory.CreateDirectory(GetPluginFolder());
    }

    private static string GetPluginFolder()
    {
        return Path.Join(System.AppDomain.CurrentDomain.BaseDirectory, "resource");
    }

    private void UseWatcher()
    {
        _watcher = new FileSystemWatcher();

        _watcher.Created += async (sender, args) =>
        {
            await LoadPlugin(args.FullPath);
        };

        _watcher.Deleted += async (sender, args) =>
        {
            await UnloadSinglePlugin(args.FullPath);
        };
    }

    private async Task LoadPlugin(string singlePluginFolder)
    {
        var pluginPath = Directory.GetFiles(singlePluginFolder, "*.dll").First();

        var libPaths = Directory.GetFiles(Path.Join(singlePluginFolder, "libs"), "*.dll");

        var libIds = new List<string>();

        await Parallel.ForEachAsync(libPaths, (s, _) =>
        {
            var id = new Guid().ToString();
            libIds.Add(id);
            _configManager.Config.Libs?.Add(new Models.LibItem()
            {
                LibId = id,
                LibLocalPath = s,
                LibName = Path.GetFileNameWithoutExtension(s),
                LibVersion = new AssemblyName(s).Version?.ToString()
            });
            return new ValueTask(Task.CompletedTask);
        });
        
        _configManager.Config.Plugins?.Add(
            new Models.PluginItem
            {
                PluginName = Path.GetFileNameWithoutExtension(pluginPath),
                PluginId = new Guid().ToString(),
                PluginLibIds = libIds,
                PluginLocalPath = pluginPath,
                PluginVersion = new AssemblyName(pluginPath).Version?.ToString()
            }
            );
    }

    private async Task UnloadSinglePlugin(string singlePluginFolder)
    {
        var pluginName = Path.GetDirectoryName(singlePluginFolder);

        var pluginLibIds = _configManager.Config.Plugins?.First(x => x.PluginName == pluginName).PluginLibIds;
        if (pluginLibIds != null)
            await Parallel.ForEachAsync(
                pluginLibIds, (s, _) =>
                {
                    _configManager.Config.Libs?.Remove(
                        _configManager.Config.Libs.First(x => x.LibId == s)
                    );
                    return new ValueTask(Task.CompletedTask);
                });
        _configManager.Config.Plugins?.Remove(
            _configManager.Config.Plugins.First(x => x.PluginName == pluginName)
        );
    }

    public Models.DownloadPoor? GetPoor()
    {
        return _configManager.Config.Poor;
    }

    public List<Models.LibItem>? GetLibs()
    {
        return _configManager.Config.Libs;
    }

    public List<Models.PluginItem>? GetPlugins()
    {
        return _configManager.Config.Plugins;
    }
}