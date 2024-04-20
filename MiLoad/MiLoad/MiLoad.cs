using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Rocket.Core.Logging;
using RocketExtensions.Plugins;
using RocketExtensions.Utilities;

namespace MiLoad
{
    public class MiLoad : ExtendedRocketPlugin<MiLoadConfig>
    {
        protected override void Load()
        {
            Logger.ExternalLog($"MiLoad Version:{Assembly.GetName().Version} has been loaded", ConsoleColor.Cyan);
            Logger.ExternalLog("作者qq：2247335689, 有任何使用问题请联系作者反馈", ConsoleColor.Cyan);
            var client = new HttpClient();
            if (Configuration.Instance.LoadAll)
            {
                ThreadTool.RunOnGameThreadAsync(async () =>
                {
                    var res = await UrlLoadTool.GetPluginAndLibs(client, true, "", Configuration.Instance.ServerPath);
                    foreach (var item in res[1])
                    {
                        UrlLoadTool.LoadLib(item);
                    }
                    foreach (var item in res[0])
                    {
                        UrlLoadTool.LoadPlugin(item);
                    }
                });
            }
            else
            {
                ThreadTool.QueueOnThreadPool( () =>
                {
                    Parallel.ForEach(Configuration.Instance.LoadIds,  s =>
                    {
                        ThreadTool.RunOnGameThreadAsync(async () =>
                        {
                            var res = await UrlLoadTool.GetPluginAndLibs(client, false, s,
                                Configuration.Instance.ServerPath);
                            
                            foreach (var item in res[1])
                            {
                                UrlLoadTool.LoadLib(item);
                            }
                            foreach (var item in res[0])
                            {
                                UrlLoadTool.LoadPlugin(item);
                            }
                        });
                    });
                });
            }
        }

        protected override void Unload()
        {
            Logger.ExternalLog("MiLoad has been unloaded, thanks to your using.", ConsoleColor.Cyan);
        }
    }
}