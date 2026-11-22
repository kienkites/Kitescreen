using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Kitescreen.DriverSetup;

// Used only by the elevated installer. The release build refuses the CI test certificate.
internal static class Program
{
    private const string HardwareId = @"Root\KitescreenPrototypeDisplay";
    private const uint SpdrpHardwareId = 0x00000001;
    private const uint DicdGenerateId = 0x00000001;
    private const uint DifRegisterDevice = 0x00000019;
    private const uint DifRemove = 0x00000005;
    private static readonly Guid DisplayClass = new("4d36e968-e325-11ce-bfc1-08002be10318");

    private static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "remove")
        {
            try { RemoveDevices(); return 0; }
            catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        }
        if (args.Length != 2 || args[0] != "install")
        {
            Console.Error.WriteLine("Usage: Kitescreen.DriverSetup install <absolute INF path> | remove");
            return 2;
        }

        try
        {
            var inf = Path.GetFullPath(args[1]);
            var directory = Path.GetDirectoryName(inf)!;
            if (!File.Exists(inf) || !File.Exists(Path.Combine(directory, "IddSampleDriver.dll")))
                throw new FileNotFoundException("The display driver package is incomplete.");
            var catalog = Path.Combine(directory, "IddSampleDriver.cat");
            if (!File.Exists(catalog)) throw new FileNotFoundException("The signed driver catalog is missing.");
            if (!File.ReadAllText(inf).Contains(HardwareId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The driver does not match the Kitescreen device.");

            // The packaging script requires a Microsoft release-signed catalog.
            // Windows validates its signature and payload when installing the driver.
            if (File.Exists(Path.Combine(directory, "Kitescreen-Test.cer")))
                throw new InvalidDataException("The VM test package cannot be installed by the release installer.");

            if (FindExistingDevice())
            {
                InstallDriver(inf);
                return 0;
            }

            CreateRootDevice();
            try { InstallDriver(inf); }
            catch
            {
                RemoveDevices();
                throw;
            }
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static void InstallDriver(string inf)
    {
        if (!DiInstallDriverW(IntPtr.Zero, inf, 0, out var reboot)) ThrowLastError("Install driver");
        if (reboot) Console.WriteLine("Windows requests a restart to finish driver installation.");
    }

    private static void CreateRootDevice()
    {
        var classGuid = DisplayClass;
        var set = SetupDiCreateDeviceInfoList(ref classGuid, IntPtr.Zero);
        if (set == new IntPtr(-1)) ThrowLastError("Create device information set");
        try
        {
            var data = new SpDevinfoData { Size = (uint)Marshal.SizeOf<SpDevinfoData>() };
            if (!SetupDiCreateDeviceInfoW(set, "KitescreenDisplay", ref classGuid, null, IntPtr.Zero,
                    DicdGenerateId, ref data)) ThrowLastError("Create root device");
            var ids = Encoding.Unicode.GetBytes(HardwareId + "\0\0");
            if (!SetupDiSetDeviceRegistryPropertyW(set, ref data, SpdrpHardwareId, ids, (uint)ids.Length))
                ThrowLastError("Set hardware ID");
            if (!SetupDiCallClassInstaller(DifRegisterDevice, set, ref data)) ThrowLastError("Register root device");
        }
        finally { SetupDiDestroyDeviceInfoList(set); }
    }

    private static bool FindExistingDevice()
    {
        // Only look for a present instance; a stale phantom must not suppress registration.
        var set = SetupDiGetClassDevsW(IntPtr.Zero, null, IntPtr.Zero, 0x06); // ALLCLASSES | PRESENT
        if (set == new IntPtr(-1)) ThrowLastError("Enumerate devices");
        try
        {
            uint index = 0;
            while (true)
            {
                var data = new SpDevinfoData { Size = (uint)Marshal.SizeOf<SpDevinfoData>() };
                if (!SetupDiEnumDeviceInfo(set, index, ref data))
                {
                    if (Marshal.GetLastWin32Error() == 259) return false; // ERROR_NO_MORE_ITEMS
                    ThrowLastError("Enumerate device");
                }
                var bytes = new byte[4096];
                if (SetupDiGetDeviceRegistryPropertyW(set, ref data, SpdrpHardwareId, out _, bytes,
                        (uint)bytes.Length, out _))
                {
                    var ids = Encoding.Unicode.GetString(bytes).Split('\0', StringSplitOptions.RemoveEmptyEntries);
                    if (ids.Any(id => id.Equals(HardwareId, StringComparison.OrdinalIgnoreCase))) return true;
                }
                index++;
            }
        }
        finally { SetupDiDestroyDeviceInfoList(set); }
    }

    private static void RemoveDevices()
    {
        var set = SetupDiGetClassDevsW(IntPtr.Zero, null, IntPtr.Zero, 0x06);
        if (set == new IntPtr(-1)) ThrowLastError("Enumerate devices for removal");
        try
        {
            // Each item in the enumeration is inspected at most once.
            uint index = 0;
            while (true)
            {
                var data = new SpDevinfoData { Size = (uint)Marshal.SizeOf<SpDevinfoData>() };
                if (!SetupDiEnumDeviceInfo(set, index, ref data))
                {
                    if (Marshal.GetLastWin32Error() == 259) return;
                    ThrowLastError("Enumerate device for removal");
                }
                var bytes = new byte[4096];
                if (!SetupDiGetDeviceRegistryPropertyW(set, ref data, SpdrpHardwareId, out _, bytes,
                        (uint)bytes.Length, out _))
                {
                    index++;
                    continue;
                }
                var ids = Encoding.Unicode.GetString(bytes).Split('\0', StringSplitOptions.RemoveEmptyEntries);
                if (!ids.Any(id => id.Equals(HardwareId, StringComparison.OrdinalIgnoreCase)))
                {
                    index++;
                    continue;
                }
                if (!SetupDiCallClassInstaller(DifRemove, set, ref data)) ThrowLastError("Remove Kitescreen device");
                index++;
            }
        }
        finally { SetupDiDestroyDeviceInfoList(set); }
    }

    private static void ThrowLastError(string operation) =>
        throw new Win32Exception(Marshal.GetLastWin32Error(), operation);

    [StructLayout(LayoutKind.Sequential)]
    private struct SpDevinfoData
    {
        public uint Size;
        public Guid ClassGuid;
        public uint DevInst;
        public IntPtr Reserved;
    }

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern IntPtr SetupDiCreateDeviceInfoList(ref Guid classGuid, IntPtr hwndParent);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiCreateDeviceInfoW(IntPtr set, string name, ref Guid classGuid,
        string? description, IntPtr hwndParent, uint flags, ref SpDevinfoData data);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiSetDeviceRegistryPropertyW(IntPtr set, ref SpDevinfoData data,
        uint property, byte[] buffer, uint size);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiCallClassInstaller(uint function, IntPtr set, ref SpDevinfoData data);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SetupDiGetClassDevsW(IntPtr classGuid, string? enumerator,
        IntPtr hwndParent, uint flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiEnumDeviceInfo(IntPtr set, uint index, ref SpDevinfoData data);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiGetDeviceRegistryPropertyW(IntPtr set, ref SpDevinfoData data,
        uint property, out uint type, byte[] buffer, uint size, out uint required);

    [DllImport("newdev.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DiInstallDriverW(IntPtr hwndParent, string inf, uint flags,
        [MarshalAs(UnmanagedType.Bool)] out bool reboot);
}
