using MapAssist.Helpers;
using System.ComponentModel;

namespace MapAssist.Types
{
    public class MapSeed
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        private BackgroundWorker BackgroundCalculator;
        private uint GameSeedXor { get; set; } = 0;

        public bool IsReady => BackgroundCalculator != null && GameSeedXor != 0;

        public uint Get(UnitPlayer player)
        {
            if (GameSeedXor != 0)
            {
                return (uint)(player.InitSeedHash ^ GameSeedXor);
            }
            else if (BackgroundCalculator == null)
            {
                var InitSeedHash = player.InitSeedHash;
                var EndSeedHash = player.EndSeedHash;

                BackgroundCalculator = new BackgroundWorker();

                BackgroundCalculator.DoWork += (sender, args) =>
                {
                    var foundSeed = D2Hash.Reverse(EndSeedHash);

                    if (foundSeed != null)
                    {
                        GameSeedXor = (uint)InitSeedHash ^ (uint)foundSeed;
                    }

                    BackgroundCalculator.Dispose();

                    if (GameSeedXor == 0)
                    {
                        _log.Info("Failed to brute force map seed");
                        BackgroundCalculator = null;
                    }
                };

                BackgroundCalculator.RunWorkerAsync();
            }

            return 0;
        }

        public static IntPtr GetLdrAddress()
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                IntPtr hProc = WindowsExternal.OpenProcess(0x001F0FFF, false, processContext.ProcessId);
                //Allocate memory for a new PROCESS_BASIC_INFORMATION structure
                IntPtr pbi = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(PROCESS_BASIC_INFORMATION)));
                //Allocate memory for a long
                IntPtr outLong = Marshal.AllocHGlobal(sizeof(long));
                IntPtr outPtr = IntPtr.Zero;

                //Store API call success in a boolean
                var queryStatus = NtQueryInformationProcess(hProc, 0, pbi, (uint)Marshal.SizeOf(typeof(PROCESS_BASIC_INFORMATION)), outLong);

                //STATUS_SUCCESS = 0, so if API call was successful querySuccess should contain 0 ergo we reverse the check.
                if (queryStatus == 0)
                {
                    var info = (PROCESS_BASIC_INFORMATION)Marshal.PtrToStructure(pbi, typeof(PROCESS_BASIC_INFORMATION));
                    var pebPtr = info.PebBaseAddress;

                    var peb = processContext.Read<PEB_32>(pebPtr);
                    outPtr = peb.Ldr;
                }

                //Free allocated space
                Marshal.FreeHGlobal(pbi);

                //Return pointer to PEB base address
                return outPtr;
            }
        }

        private static uint HiDWord(ulong number)
        {
            return (uint)(number >> 32);
        }

        private static uint ror4(uint value, int count)
        {
            var nbits = sizeof(uint) * 8;

            count = -count % nbits;
            var low = value << (nbits - count);
            value >>= count;
            value |= low;

            return value;
        }

        [DllImport("ntdll.dll", SetLastError = true)]
        static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, IntPtr processInformation, uint processInformationLength, IntPtr returnLength);


        private struct PROCESS_BASIC_INFORMATION
        {
            public uint ExitStatus;
            public IntPtr PebBaseAddress;
            public UIntPtr AffinityMask;
            public int BasePriority;
            public UIntPtr UniqueProcessId;
            public UIntPtr InheritedFromUniqueProcessId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public partial struct PEB_32
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] Reserved1;
            public byte BeingDebugged;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public byte[] Reserved2;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public IntPtr[] Reserved3;
            public IntPtr Ldr;
            public IntPtr ProcessParameters;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public IntPtr[] Reserved4;
            public IntPtr AtlThunkSListPtr;
            public IntPtr Reserved5;
            public uint Reserved6;
            public IntPtr Reserved7;
            public uint Reserved8;
            public uint AtlThunkSListPtr32;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 45)]
            public IntPtr[] Reserved9;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 96)]
            public byte[] Reserved10;
            public IntPtr PostProcessInitRoutine;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            public byte[] Reserved11;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public IntPtr[] Reserved12;
            public uint SessionId;
        }
    }
}
