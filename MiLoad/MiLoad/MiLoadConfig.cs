using System.Collections.Generic;
using Rocket.API;

namespace MiLoad
{
    public class MiLoadConfig : IRocketPluginConfiguration
    {
        public string ServerPath { get; set; }
        public List<string> LoadIds { get; set; }
        public bool LoadAll { get; set; }

        public void LoadDefaults()
        {
            ServerPath = "http://127.0.0.1/:5000";
            LoadIds =
            [
                "1",
                "5"
            ];
            LoadAll = true;
        }
    }
}