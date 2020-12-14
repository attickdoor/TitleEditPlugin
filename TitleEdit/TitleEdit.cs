using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Hooking;
using Dalamud.Plugin;
using Serilog;

namespace TitleEditPlugin
{
    public class TitleEdit
    {
        public delegate ulong OnGetTitleMapString(long param1);
        public delegate int OnCreateScene(IntPtr p1, uint p2, IntPtr p3, uint p4, IntPtr p5, int p6, uint p7);
        public delegate IntPtr OnFixOn(IntPtr self, float[] cameraPos, float[] focusPos, float fovY);
        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        public delegate IntPtr OnPlayMusic(IntPtr self, string filename, float volume, uint fade_time);
        public delegate IntPtr OnSetWeather(IntPtr weatherStruct, int weatherEnum, byte weatherId);
        public delegate IntPtr SetWeather(IntPtr weatherStruct, int weatherEnum, byte weatherId);
        public delegate void SetServerWeather(IntPtr weatherStruct, byte weatherId, float transitionTime, byte idk);
        //public delegate long OnNewHook(long param1, long param2, long param3, long param4, char param5);

        private IntPtr HighestExpac;
        private IntPtr HasExpacBeenSet;
        private byte LastCall;

        private byte[] arrlobbybuf = Encoding.UTF8.GetBytes("ffxiv/zon_z1/chr/z1c1/level/z1c1");
        //private byte[] hwlobbybuf = Encoding.UTF8.GetBytes("ex1/05_zon_z2/chr/z2c1/level/z2c1");
        private byte[] hwlobbybuf = Encoding.UTF8.GetBytes("ex3/01_nvt_n4/fld/n4fe/level/n4fe");
        private byte[] sblobbybuf = Encoding.UTF8.GetBytes("ex2/05_zon_z3/chr/z3c1/level/z3c1");
        private byte[] shblobbybuf = Encoding.UTF8.GetBytes("ex3/05_zon_z4/chr/z4c1/level/z4c1");

        private List<IntPtr> TitleScreenNames;    

        private readonly TitleEditAddressResolver Address;

        private Hook<OnGetTitleMapString> GetTitleMapStringHook;
        private Hook<OnCreateScene> CreateSceneHook;
        private Hook<OnPlayMusic> PlayMusicHook;
        private Hook<OnFixOn> FixOnHook;
        private Hook<OnSetWeather> SetWeatherHook;

        private OnFixOn DoFixOn;
        private SetWeather DoSetWeather;

        private sbyte ExpacNum = -1;
        private bool TitleCameraNeedsSet = false;
        private bool WeatherNeedsSet = false;
        private IntPtr WeatherLoc;
        private SetServerWeather ssw;
        private Hook<SetServerWeather> sswhook;

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

            //HighestExpac = scanner.GetStaticAddressFromSig("74 0B 8B 05 ?? ?? ?? ??", 0);
            //HasExpacBeenSet = scanner.GetStaticAddressFromSig("E8 ?? ?? ?? ?? 83 F8 03 77 15", 0x4);

            PluginLog.Log("===== T I T L E E D I T =====");
            PluginLog.Log("CreateScene address {0}", Address.CreateScene);

            //GetTitleMapStringHook = new Hook<OnGetTitleMapString>(Address.GetLobbyMapString, new OnGetTitleMapString(HandleGetTitleMapString), this);
            CreateSceneHook = new Hook<OnCreateScene>(Address.CreateScene, new OnCreateScene(HandleNewHook), this);
            PlayMusicHook = new Hook<OnPlayMusic>(Address.PlayMusic, new OnPlayMusic(HandlePlayMusic), this);
            FixOnHook = new Hook<OnFixOn>(Address.FixOn, new OnFixOn(HandleFixOn), this);
            DoFixOn = new OnFixOn(Marshal.GetDelegateForFunctionPointer<OnFixOn>(Address.FixOn));
            DoSetWeather = new SetWeather(Marshal.GetDelegateForFunctionPointer<SetWeather>(Address.SetClientWeather));
            SetWeatherHook = new Hook<OnSetWeather>(Address.SetClientWeather, new OnSetWeather(HandleWeather), this);
            WeatherLoc = Address.WeatherStruct;
            ssw = new SetServerWeather(Marshal.GetDelegateForFunctionPointer<SetServerWeather>(Address.SetServerWeather));
            sswhook = new Hook<SetServerWeather>(Address.SetServerWeather + 4, new SetServerWeather(HandleSSW), this);
            //WeatherLoc = Address.SetClientWeather;

            TitleScreenNames.Add(AllocateString(arrlobbybuf));
            TitleScreenNames.Add(AllocateString(hwlobbybuf));
            TitleScreenNames.Add(AllocateString(sblobbybuf));
            TitleScreenNames.Add(AllocateString(shblobbybuf));
        }

        private void HandleSSW(IntPtr p1, byte p2, float p3, byte p4)
        {
            PluginLog.Log("HELLO THERE {0}, {1}, {2}, {3}", p1, p2, p3, p4);
            sswhook.Original(p1, p2, p3, p4);
        }

        private IntPtr HandleWeather(IntPtr p1, int p2, byte p3)
        {
            PluginLog.Log("Weather was set! {0}, {1}, {2}", p1, p2, p3);
            if (WeatherNeedsSet)
            {
                WeatherNeedsSet = false;
                p3 = 3;
            }
            return SetWeatherHook.Original(p1, p2, p3);
        }

        private IntPtr HandleFixOn(IntPtr self, float[] cameraPos, float[] focusPos, float fovY)
        {
            if (TitleCameraNeedsSet)
            {
                ssw(WeatherLoc + 0x48, 3, 20, 0);
                //DoSetWeather(WeatherLoc, 0, 3);
                TitleCameraNeedsSet = false;
                return FixOnHook.Original(self, new float[] { 192.5f, 23, 128.5f }, new float[] { 314, 114, -55 }, 45);
            }
            return FixOnHook.Original(self, cameraPos, focusPos, fovY);
        }

        public void Enable()
        {
            //GetTitleMapStringHook.Enable();
            CreateSceneHook.Enable();
            PlayMusicHook.Enable();
            FixOnHook.Enable();
            SetWeatherHook.Enable();
            sswhook.Enable();
        }

        public void Dispose()
        {
            //GetTitleMapStringHook.Dispose();
            CreateSceneHook.Dispose();
            PlayMusicHook.Dispose();
            FixOnHook.Dispose();
            SetWeatherHook.Dispose();
            sswhook.Dispose();
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

        private int HandleNewHook(IntPtr p1, uint p2, IntPtr p3, uint p4, IntPtr p5, int p6, uint p7)
        {
            var str = Marshal.PtrToStringAnsi(p1);
            //PluginLog.Log("String is {0}", p1);
            if (Marshal.PtrToStringAnsi(p1).Contains("ex3/05_zon_z4/chr/z4c1/level/z4c1"))
            {
                ssw(WeatherLoc + 0x48, 3, 20, 0);
                TitleCameraNeedsSet = true;
                WeatherNeedsSet = true;
                p1 = TitleScreenNames[1];
                //return CreateSceneHook.Original(p1, p2, p3, p4, p5, p6, p7);
                var toReturn = CreateSceneHook.Original(p1, p2, p3, p4, p5, p6, p7);
                ssw(WeatherLoc + 0x48, 3, 20, 0);
                //Marshal.WriteByte(WeatherLoc, 3);
                return toReturn;
            }
            else
                TitleCameraNeedsSet = false;
            return CreateSceneHook.Original(p1, p2, p3, p4, p5, p6, p7);
        }

        private IntPtr HandlePlayMusic(IntPtr self, string filename, float volume, uint fade_time)
        {
            if (filename.EndsWith("_System_Title.scd"))
                filename = "music/ffxiv/bgm_con_neal.scd";
            return PlayMusicHook.Original(self, filename, volume, fade_time);
        }

        /*
        private long HandleNewHook(long param1, long param2, long param3, long param4, char param5)
        {
            PluginLog.Log("args: {0}, {1}, {2}, {3}, {4}", param1, param2, param3, param4, param5);
            return NewHookHook.Original(param1, param2, param3, param4, param5);
        }
        */
        public void SetExpac(sbyte b)
        {
            ExpacNum = b;
        }
    }
}