using System;
using System.Collections.Generic;
using System.Diagnostics;
using WinEDR_MVP.Config;
using WinEDR_MVP.Interfaces;
using WinEDR_MVP.Models;

namespace WinEDR_MVP.Rules.HIDS
{
    // HIDS-P1: System Process Masquerading
    public class SystemProcessMasqueradingRule : IDetectionRule
    {
        public string RuleId => "HIDS-P1";
        public string Name => "System Process Masquerading";
        public string Description => "Detects system processes running outside legitimate paths.";

        private readonly AppConfig _config;

        public SystemProcessMasqueradingRule(AppConfig config)
        {
            _config = config;
        }

        public List<DetectionEvent> Evaluate()
        {
            var events = new List<DetectionEvent>();
            var processes = Process.GetProcesses();

            foreach (var proc in processes)
            {
                try
                {
                    string processName = proc.ProcessName.ToLower();
                    // Basic check: if it looks like a system process (fake list for MVP)
                    if (_config.TrustedSystemProcesses.Contains(processName + ".exe"))
                    {
                        // Check path
                        string path = proc.MainModule?.FileName;
                        if (string.IsNullOrEmpty(path)) continue;

                        bool isLegit = false;
                        if (processName == "explorer")
                        {
                            isLegit = path.Equals(@"C:\WINDOWS\explorer.exe", StringComparison.OrdinalIgnoreCase);
                        }
                        else
                        {
                            foreach (var trustedPath in _config.TrustedExecutionPaths)
                            {
                                if (path.StartsWith(trustedPath, StringComparison.OrdinalIgnoreCase))
                                {
                                    isLegit = true;
                                    break;
                                }
                            }
                        }

                        if (!isLegit)
                        {
                            events.Add(new DetectionEvent
                            {
                                RuleId = RuleId,
                                RuleName = Name,
                                Severity = AlertSeverity.High,
                                Type = AlertType.MAL,
                                Description = $"System process {proc.ProcessName} running from unexpected location: {path}",
                                Metadata = new { ProcessId = proc.Id, Path = path }
                            });
                        }
                    }
                }
                catch { /* Access denied or process exited */ }
            }
            return events;
        }
    }
}
