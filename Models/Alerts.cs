using System;

namespace WinEDR_MVP.Models
{
    public enum AlertSeverity
    {
        Low,
        Medium,
        High
    }

    public enum AlertType
    {
        MAL,    // Malware
        TROJ,   // Trojan
        BACK,   // Backdoor
        RECON,  // Reconnaissance
        RANSOM, // Ransomware
        INFO    // Informational/Other
    }

    public class Alert
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public AlertSeverity Severity { get; set; }
        public AlertType Type { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string SourceProcess { get; set; }
        public string RuleId { get; set; }
        public object Metadata { get; set; }
    }

    public class DetectionEvent
    {
        public string RuleId { get; set; }
        public string RuleName { get; set; }
        public AlertSeverity Severity { get; set; }
        public AlertType Type { get; set; }
        public string Description { get; set; }
        public object Metadata { get; set; } // Flexible payload (Process info, Network info)
    }
}
