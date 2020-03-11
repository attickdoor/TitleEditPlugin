using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
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
        private byte LastCall;

        private byte[] arrlobbybuf = Encoding.UTF8.GetBytes("ffxiv/zon_z1/chr/z1c1/level/z1c1");
        private byte[] hwlobbybuf = Encoding.UTF8.GetBytes("ex1/05_zon_z2/chr/z2c1/level/z2c1");
        private byte[] sblobbybuf = Encoding.UTF8.GetBytes("ex2/05_zon_z3/chr/z3c1/level/z3c1");
        private byte[] shblobbybuf = Encoding.UTF8.GetBytes("ex3/05_zon_z4/chr/z4c1/level/z4c1");

        private List<IntPtr> TitleScreenNames;    

        private readonly TitleEditAddressResolver Address;

        private Hook<OnGetTitleMapString> GetTitleMapStringHook;

        private byte ExpacNum = 0;

        private IntPtr AllocateString(byte[] buf)
        {
            IntPtr toReturn;
            toReturn = Marshal.AllocHGlobal(buf.Length + 1);
            Marshal.Copy(buf, 0, toReturn, buf.Length);
            Marshal.WriteByte(IntPtr.Add(toReturn, buf.Length), 0);
            return toReturn;
        }

        public TitleEdit(SigScanner scanner, ClientState clientState, TitleEditConfiguration configuration)
        {
            TitleScreenNames = new List<IntPtr>();
            LastCall = 200;

            Address = new TitleEditAddressResolver();
            Address.Setup(scanner);

            byteBase = scanner.Module.BaseAddress;
            HighestExpac = IntPtr.Add(byteBase, 0x1C48B80);
            HasExpacBeenSet = IntPtr.Add(byteBase, 0x1C48B7C);

            Log.Verbose("===== T I T L E E D I T =====");
            Log.Verbose("GetTitleMapString address {0}", Address.GetLobbyMapString);

            GetTitleMapStringHook = new Hook<OnGetTitleMapString>(Address.GetLobbyMapString, new OnGetTitleMapString(HandleGetTitleMapString), this);

            TitleScreenNames.Add(AllocateString(arrlobbybuf));
            TitleScreenNames.Add(AllocateString(hwlobbybuf));
            TitleScreenNames.Add(AllocateString(sblobbybuf));
            TitleScreenNames.Add(AllocateString(shblobbybuf));
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
            byte tmp = ExpacNum;
            if (tmp == 4)
                tmp = (byte)new Random().Next(0, 4);
            if (param1 == LastCall)
                return GetTitleMapStringHook.Original(param1);
            if (param1 == 0)
                Marshal.WriteByte(HighestExpac, tmp);
            LastCall = (byte)param1;
            return GetTitleMapStringHook.Original(param1);
        }

        public void SetExpac(byte b)
        {
            ExpacNum = b;
        }
    }
}