using Dalamud.Game;
using Dalamud.Game.Internal;
using System;

namespace TitleEditPlugin
{
    class TitleEditAddressResolver : BaseAddressResolver
    {
        public IntPtr GetLobbyMapString { get; private set; }

        public IntPtr IncrementTitleScreenCutsceneTimer { get; private set; }

        protected override void Setup64Bit(SigScanner sig)
        {
            this.GetLobbyMapString = sig.ScanText("48 83 EC 28 48 63 C1 48 8D 15 ?? ?? ?? ?? 48 8B 04 C2 85 C9 75 7B 38 0D ?? ?? ?? ?? 75 33 48 8B 0D ?? ?? ?? ?? 48 89 5C 24 ?? 0F BE 99 ?? ?? ?? ??");
            this.IncrementTitleScreenCutsceneTimer = sig.ScanText("48 01 8F C8 07 00 00 48 81 BF C8 07 00 00 60 EA 00 00");
        }
    }
}
