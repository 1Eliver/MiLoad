using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rocket.Core;
using Rocket.Core.Plugins;
using Rocket.Core.Utils;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace MiLoad
{
    public static class UrlLoadTool
    {
        public static void LoadLib(byte[] lib)
        {
            var libAssembly = Assembly.Load(lib);
            Logger.ExternalLog($"[MiLoad] Loading dependency called {libAssembly.GetName().Name}......", ConsoleColor.Cyan);
        }

        public static void LoadPlugin(byte[] plugin)
        {
            var pluginAssembly = Assembly.Load(plugin);

            var pluginTypes = RocketHelper.GetTypesFromInterface(pluginAssembly, "IRocketPlugin")
                .FindAll(x => !x.IsAbstract);

            if (pluginTypes.Count == 1)
            {
                // 通过反射获取插件程序集
                Logger.ExternalLog($"[MiLoad] Loading {pluginAssembly.GetName().Name}......", ConsoleColor.Cyan);
            
                // 将插件生成游戏对象
                var type = pluginTypes[0];
                var target = new GameObject(type.Name);
                target.AddComponent(type);
                UnityEngine.Object.DontDestroyOnLoad(target);
                
                // 通过反射将插件程序集加到插件管理器实例中
                var pluginsInfo = typeof(RocketPluginManager).GetProperty("plugins");
                if (pluginsInfo != null)
                {
                    var oldValue = (List<GameObject>)pluginsInfo.GetValue(R.Plugins, null);
                    oldValue.Add(target);
                    pluginsInfo.SetValue(R.Plugins, oldValue);
                    
                    // 通过反射Invoke插件加载事件
                    var eventDelegate = (MulticastDelegate)R.Plugins.GetType().GetField("OnPluginsLoaded", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(R.Plugins);
                    if (eventDelegate == null) return;
                    foreach (var handler in eventDelegate.GetInvocationList())
                    {
                        handler.Method.Invoke(handler.Target, new object[] { R.Plugins, EventArgs.Empty });
                    }
                }
                else
                {
                    Logger.ExternalLog($"[MiLoad] Some errors occurred while loading {pluginAssembly.GetName().Name}!", ConsoleColor.Cyan);
                }
            }
            else
            {
                Logger.ExternalLog($"[MiLoad] Could not load {pluginAssembly.GetName().Name}!", ConsoleColor.Cyan);
            }
        }

        public static async Task<byte[]> GetFile(string id, HttpClient client, string url)
        {
            var res = await client.GetAsync(url + $"unturnedPlugins/{id}");
            if (res.IsSuccessStatusCode)
            {
                return await res.Content.ReadAsByteArrayAsync();
            }

            return null;
        }

        public static async Task<List<List<byte[]>>> GetPluginAndLibs(HttpClient client, bool isAll, string id, string url)
        {
            // 初始化返回数组
            var re = new List<List<byte[]>>
            {
                new(),
                new()
            };
            
            async void GetPlugin(string s)
            {
                re[0].Add(await GetFile(s, client, url));
            }

            async void GetLib(string s)
            {
                re[1].Add(await GetFile(s, client, url));
            }
            
            // 分支确定需要下载的程序
            if (isAll)
            {
                var res = await client.GetAsync(url + "unturnedPlugins/pluginGet/all");
                if (!res.IsSuccessStatusCode) return re;
                var resJson =
                    JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(
                        await res.Content.ReadAsStringAsync());
                    
                Parallel.ForEach(resJson["plugin"], GetPlugin);
                Parallel.ForEach(resJson["libs"], GetLib);
            }
            else
            {
                var res = await client.GetAsync(url + $"unturnedPlugins/pluginGet/{id}");
                if (!res.IsSuccessStatusCode) return re;
                var resJson =
                    JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(
                        await res.Content.ReadAsStringAsync());
                Parallel.ForEach(resJson["plugin"], GetPlugin);
                Parallel.ForEach(resJson["libs"], GetLib);
            }

            return re;
        }
    }
}