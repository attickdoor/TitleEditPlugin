using System;
using Dalamud.Configuration;

namespace TitleEditPlugin
{
    [Serializable]
    public class TitleEditConfiguration : IPluginConfiguration
    {

        public int ExpacNum{ get; set; }
        int IPluginConfiguration.Version { get; set; }

        public bool DisableCutscene { get; set; }

        public TitleEditConfiguration()
        {
            ExpacNum = -1;
            DisableCutscene = false;
        }
    }
}
