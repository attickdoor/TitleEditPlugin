using System;
using Dalamud.Configuration;

namespace TitleEditPlugin
{
    [Serializable]
    public class TitleEditConfiguration : IPluginConfiguration
    {

        public byte ExpacNum{ get; set; }
        int IPluginConfiguration.Version { get; set; }

        public TitleEditConfiguration()
        {
            ExpacNum = 0;
        }
    }
}
