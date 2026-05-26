using System;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using WinEDR_MVP.Config;
using WinEDR_MVP.Engine;
using WinEDR_MVP.Rules.HIDS;
using WinEDR_MVP.Rules.Malware;
using System.Text.Json;
using System.Windows.Forms;

namespace WinEDR_MVP
{
    static class Program
    {
        // ── P/Invoke for SeDebugPrivilege ──────────────────────────────
        private const int TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const int TOKEN_QUERY = 0x0008;
        private const int SE_PRIVILEGE_ENABLED = 0x00000002;
        private const string SE_DEBUG_NAME = "SeDebugPrivilege";

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public int Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_PRIVILEGES
        {
            public int PrivilegeCount;
            public LUID_AND_ATTRIBUTES Privileges;
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool LookupPrivilegeValue(string? lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges,
            ref TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Enables SeDebugPrivilege for the current process.
        /// Requires the application to be running as Administrator.
        /// </summary>
        private static bool EnableSeDebugPrivilege()
        {
            try
            {
                if (!OpenProcessToken(Process.GetCurrentProcess().Handle,
                    TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out IntPtr tokenHandle))
                {
                    Console.WriteLine("Failed to open process token.");
                    return false;
                }

                try
                {
                    if (!LookupPrivilegeValue(null, SE_DEBUG_NAME, out LUID luid))
                    {
                        Console.WriteLine("Failed to lookup SeDebugPrivilege.");
                        return false;
                    }

                    var tp = new TOKEN_PRIVILEGES
                    {
                        PrivilegeCount = 1,
                        Privileges = new LUID_AND_ATTRIBUTES
                        {
                            Luid = luid,
                            Attributes = SE_PRIVILEGE_ENABLED
                        }
                    };

                    if (!AdjustTokenPrivileges(tokenHandle, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero))
                    {
                        Console.WriteLine("Failed to adjust token privileges.");
                        return false;
                    }

                    int error = Marshal.GetLastWin32Error();
                    if (error != 0)
                    {
                        Console.WriteLine($"AdjustTokenPrivileges returned error: {error}. " +
                            "Make sure the application is running as Administrator.");
                        return false;
                    }

                    Console.WriteLine("SeDebugPrivilege enabled successfully.");
                    return true;
                }
                finally
                {
                    CloseHandle(tokenHandle);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enabling SeDebugPrivilege: {ex.Message}");
                return false;
            }
        }
        // ───────────────────────────────────────────────────────────────

        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Enable SeDebugPrivilege so we can inspect system processes
            if (!EnableSeDebugPrivilege())
            {
                MessageBox.Show(
                    "Could not enable SeDebugPrivilege.\n\n" +
                    "Please run this application as Administrator for full process inspection.",
                    "WinEDR MVP - Privilege Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            // Ensure config exists
            string configPath = "config.json";
            if (!File.Exists(configPath))
            {
                CreateDefaultConfig(configPath);
            }

            Application.Run(new MainForm());
        }

        static void CreateDefaultConfig(string path)
        {
            var config = new AppConfig();
            config.TrustedSystemProcesses.Add("svchost.exe");
            config.TrustedSystemProcesses.Add("explorer.exe");
            config.TrustedSystemProcesses.Add("services.exe");
            
            config.TrustedExecutionPaths.Add(@"C:\Windows\System32");
            config.TrustedExecutionPaths.Add(@"C:\Windows\Explorer.exe"); 
            config.TrustedExecutionPaths.Add(@"C:\Program Files");

            config.UntrustedExecutionPaths.Add(@"%USERPROFILE%\Downloads");
            config.UntrustedExecutionPaths.Add(@"%USERPROFILE%\AppData\Local\Temp");
            config.UntrustedExecutionPaths.Add(@"%USERPROFILE%\Desktop"); // For testing self-scan

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, JsonSerializer.Serialize(config, options));
        }
    }
}
