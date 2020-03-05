using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Hooking;
using Serilog;

namespace TitleEditPlugin
{
    public class TitleEdit
    {
        public delegate ulong OnGetTitleMapString(long param1);

        private readonly IntPtr byteBase;
        private IntPtr HighestExpac;
        private IntPtr HasExpacBeenSet;

        private readonly TitleEditAddressResolver Address;

        private Hook<OnGetTitleMapString> GetTitleMapStringHook;

        private byte ExpacNum = 0;

        public TitleEdit(SigScanner scanner, ClientState clientState, TitleEditConfiguration configuration)
        {
            Address = new TitleEditAddressResolver();
            Address.Setup(scanner);

            byteBase = scanner.Module.BaseAddress;
            HighestExpac = IntPtr.Add(byteBase, 0x1C48C00);
            HasExpacBeenSet = IntPtr.Add(byteBase, 0x1C48BFC);

            Log.Verbose("===== T I T L E E D I T =====");
            Log.Verbose("GetTitleMapString address {0}", Address.GetLobbyMapString);

            GetTitleMapStringHook = new Hook<OnGetTitleMapString>(Address.GetLobbyMapString, new OnGetTitleMapString(HandleGetTitleMapString), this);
        }

        public void Enable()
        {
            GetTitleMapStringHook.Enable();
        }

        public void Dispose()
        {
            GetTitleMapStringHook.Dispose();
        }

        private ulong HandleGetTitleMapString(long param1)
        {
            Log.Information("Function called! {0}", Marshal.ReadByte(HasExpacBeenSet));
            if (Marshal.ReadByte(HasExpacBeenSet) == 0)
            {
                Marshal.WriteByte(HasExpacBeenSet, 1);
            }
            Marshal.WriteByte(HighestExpac, ExpacNum);
            return GetTitleMapStringHook.Original(param1);
        }

        public void SetExpac(byte b)
        {
            ExpacNum = b;
        }
    }
}