using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using WinEDR_MVP.Models;

namespace WinEDR_MVP.Engine
{
    public class AlertManager
    {
        private readonly List<Alert> _alerts = new List<Alert>();
        private readonly string _logPath;
        private readonly object _lock = new object();

        public event Action<Alert> OnAlert;

        public AlertManager(string logPath = "alerts.log")
        {
            _logPath = logPath;
        }

        public void AddAlert(Alert alert)
        {
            lock (_lock)
            {
                _alerts.Add(alert);
                LogAlert(alert);
                OnAlert?.Invoke(alert);
                // Console output is less useful in WinForms, but keeping for debug if needed
                // Console.ForegroundColor = ConsoleColor.Red;
                // Console.WriteLine($"[ALERT] {alert.Type} ({alert.Severity}): {alert.Title} - {alert.Description}");
                // Console.ResetColor();
            }
        }

        private void LogAlert(Alert alert)
        {
            try
            {
                var json = JsonSerializer.Serialize(alert);
                File.AppendAllText(_logPath, json + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log alert: {ex.Message}");
            }
        }

        public List<Alert> GetAlerts()
        {
            lock(_lock)
            {
                return new List<Alert>(_alerts);
            }
        }
    }
}
