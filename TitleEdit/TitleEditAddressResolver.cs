using Dalamud.Game;
using Dalamud.Game.Internal;
using System;

namespace TitleEditPlugin
{
    class TitleEditAddressResolver : BaseAddressResolver
    {
        public IntPtr GetLobbyMapString { get; private set; }
        public IntPtr CreateScene { get; private set; }
        public IntPtr FixOn { get; private set; }
        public IntPtr CameraStruct { get; private set; }
        public IntPtr PlayMusic { get; private set; }
        public IntPtr newHook { get; private set; }
        public IntPtr SetClientWeather { get; private set; }
        public IntPtr WeatherStruct { get; private set; }
        public IntPtr SetServerWeather { get; private set; }

        protected override void Setup64Bit(SigScanner sig)
        {
            //this.GetLobbyMapString = sig.ScanText("48 83 EC 28 48 63 C1 48 8D 15 ?? ?? ?? ?? 48 8B 04 C2 85 C9 75 7B 38 0D ?? ?? ?? ?? 75 33 48 8B 0D ?? ?? ?? ?? 48 89 5C 24 ?? 0F BE 99 ?? ?? ?? ??");
            this.newHook = sig.ScanText("E8 ?? ?? ?? ?? 33 D2 48 8D 4B D8");
            this.newHook = sig.ScanText("48 63 C1 48 8D 15 ?? ?? ?? ?? 48 8B 04 C2");
            this.CreateScene= sig.ScanText("E8 ?? ?? ?? ?? 66 89 1D ?? ?? ?? ?? E9 ?? ?? ?? ??");
            this.FixOn = sig.ScanText("C6 81 ?? ?? ?? ?? ?? 8B 02 89 41 60");
            this.CameraStruct = sig.GetStaticAddressFromSig("48 39 1D ?? ?? ?? ?? 0F 84 ?? ?? ?? ??");
            this.PlayMusic = sig.ScanText("E8 ?? ?? ?? ?? 48 89 47 18 89 5F 20");
            //this.SetClientWeather = sig.ScanText("E8 ?? ?? ?? ?? EB 4D 80 3B 00");
            this.SetClientWeather = sig.Module.BaseAddress + 0xB39900;
            //this.WeatherStruct = sig.GetStaticAddressFromSig("E8 ?? ?? ?? ?? 83 BF ?? ?? ?? ?? ?? 0F 84 ?? ?? ?? ?? 48 8D 4C 24 ??", 0x29B);
            this.WeatherStruct = sig.Module.BaseAddress + 0x1CBFB00;
            this.SetServerWeather = sig.Module.BaseAddress + 0xB38A90;
            //this.newHook = sig.ScanText("E8 ?? ?? ?? 00 48 8B 05 ?? ?? ?? 01 48 8D 1D ?? ?? ?? 01");
            //this.newHook = sig.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 40 48 8B 7C 24 ?? 48 8B DA");
        }
    }
}
