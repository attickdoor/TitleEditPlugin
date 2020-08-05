using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            int nSize,
            out IntPtr lpNumberOfBytesWritten);

        public delegate ulong OnGetTitleMapString(long param1);

        private IntPtr HighestExpac;
        private IntPtr HasExpacBeenSet;
        private byte LastCall;
        private bool overwrittenCutsceneCounter;

        private byte[] arrlobbybuf = Encoding.UTF8.GetBytes("ffxiv/zon_z1/chr/z1c1/level/z1c1");
        private byte[] hwlobbybuf = Encoding.UTF8.GetBytes("ex1/05_zon_z2/chr/z2c1/level/z2c1");
        private byte[] sblobbybuf = Encoding.UTF8.GetBytes("ex2/05_zon_z3/chr/z3c1/level/z3c1");
        private byte[] shblobbybuf = Encoding.UTF8.GetBytes("ex3/05_zon_z4/chr/z4c1/level/z4c1");

        private List<IntPtr> TitleScreenNames;    

        private readonly TitleEditAddressResolver Address;

        private Hook<OnGetTitleMapString> GetTitleMapStringHook;

        private sbyte ExpacNum = -1;

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

            HighestExpac = scanner.GetStaticAddressFromSig("74 0B 8B 05 ?? ?? ?? ??", 0);
            HasExpacBeenSet = scanner.GetStaticAddressFromSig("E8 ?? ?? ?? ?? 83 F8 03 77 15", 0x4);

            Log.Verbose("===== T I T L E E D I T =====");
            Log.Verbose("GetTitleMapString address {0}", Address.GetLobbyMapString);
            Log.Verbose("IncrementTitleScreenCutsceneTimer address {0}", Address.IncrementTitleScreenCutsceneTimer);

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
            EnableCutscene();
        }

        public void DisableCutscene()
        {
            WriteProcessMemory(Process.GetCurrentProcess().Handle, Address.IncrementTitleScreenCutsceneTimer + 1, new byte[] { 0x89, 0xB7 }, 1, out _);
            overwrittenCutsceneCounter = true;
        }

        public void EnableCutscene()
        {
            if (!overwrittenCutsceneCounter) return;
            WriteProcessMemory(Process.GetCurrentProcess().Handle, Address.IncrementTitleScreenCutsceneTimer + 1, new byte[] { 0x01, 0x8F }, 1, out _);
            overwrittenCutsceneCounter = false;
        }

        private ulong HandleGetTitleMapString(long param1)
        {
            if (ExpacNum == -1) return GetTitleMapStringHook.Original(param1);
            byte tmp = (byte)ExpacNum;
            if (tmp == 4)
                tmp = (byte)new Random().Next(0, 4);
            if (param1 == LastCall)
                return GetTitleMapStringHook.Original(param1);
            if (Marshal.ReadByte(HasExpacBeenSet) != 1)
            {
                GetTitleMapStringHook.Original(param1);
            }
            Marshal.WriteByte(HighestExpac, tmp);
            LastCall = (byte)param1;
            if (param1 == 1) return (ulong)TitleScreenNames[0];
            return (ulong)TitleScreenNames[tmp];
        }

        public void SetExpac(sbyte b)
        {
            ExpacNum = b;
        }
    }
}