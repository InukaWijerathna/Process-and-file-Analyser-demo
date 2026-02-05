using System;
using System.IO;
using System.Threading;
using WinEDR_MVP.Config;
using WinEDR_MVP.Engine;
using WinEDR_MVP.Rules.HIDS;
using WinEDR_MVP.Rules.Malware;
using System.Text.Json;
using System.Windows.Forms;
using System.Windows.Forms;

namespace WinEDR_MVP
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
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
