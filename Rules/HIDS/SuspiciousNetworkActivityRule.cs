using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using WinEDR_MVP.Config;
using WinEDR_MVP.Interfaces;
using WinEDR_MVP.Models;

namespace WinEDR_MVP.Rules.HIDS
{
    // Shared state for stateful network rules
    public static class NetworkState
    {
        public static Dictionary<string, int> PortScanTracker = new Dictionary<string, int>(); 
        public static DateTime LastScanTime = DateTime.MinValue;
        public static int ConnectionBurstCounter = 0;
        public static DateTime BurstStartTime = DateTime.MinValue;
    }

    public class SuspiciousNetworkActivityRule : IDetectionRule
    {
        public string RuleId => "HIDS-N";
        public string Name => "Network Anomaly Detection";
        public string Description => "Detects suspicious network connections, scanning, and bursts.";

        private readonly AppConfig _config;

        public SuspiciousNetworkActivityRule(AppConfig config)
        {
            _config = config;
        }

        public List<DetectionEvent> Evaluate()
        {
            var events = new List<DetectionEvent>();
            var properties = IPGlobalProperties.GetIPGlobalProperties();
            var connections = properties.GetActiveTcpConnections();

            // HIDS-N1: Suspicious Outbound Ports
            var suspiciousPorts = new  [] { 4444, 6667, 1337, 31337 }; 

            foreach (var conn in connections)
            {
                if (conn.State == TcpState.Established || conn.State == TcpState.SynSent)
                {
                    if (suspiciousPorts.Contains(conn.RemoteEndPoint.Port))
                    {
                        events.Add(new DetectionEvent
                        {
                            RuleId = "HIDS-N1",
                            RuleName = "Suspicious Outbound Port",
                            Severity = AlertSeverity.High,
                            Type = AlertType.TROJ,
                            Description = $"Connection to suspicious port {conn.RemoteEndPoint.Port} at {conn.RemoteEndPoint.Address}",
                            Metadata = new { Local = conn.LocalEndPoint.ToString(), Remote = conn.RemoteEndPoint.ToString() }
                        });
                    }
                }
            }

            // Stateful Checks
            var now = DateTime.UtcNow;
            
            // HIDS-N3: Burst
            if ((now - NetworkState.BurstStartTime).TotalSeconds > 30)
            {
                NetworkState.BurstStartTime = now;
                NetworkState.ConnectionBurstCounter = 0;
            }
             
            if (connections.Length > 100) 
            {
                events.Add(new DetectionEvent
                {
                    RuleId = "HIDS-N3",
                    RuleName = "Abnormal Network Traffic Burst",
                    Severity = AlertSeverity.Medium,
                    Type = AlertType.RECON,
                    Description = $"High connection count detected: {connections.Length} active connections.",
                });
            }

            // HIDS-N2: Port Scanning 
            var distinctRemotePorts = connections.Select(c => c.RemoteEndPoint.Port).Distinct().Count();
            if (distinctRemotePorts > 20) 
            {
                events.Add(new DetectionEvent
                {
                    RuleId = "HIDS-N2",
                    RuleName = "Potential Port Scanning Behavior",
                    Severity = AlertSeverity.Medium,
                    Type = AlertType.RECON,
                    Description = $"Host is connected to {distinctRemotePorts} distinct remote ports.",
                });
            }

            // HIDS-N4: Header/Payload Analysis Simulation
            foreach (var conn in connections)
            {
                // Simulation trigger
                if (conn.RemoteEndPoint.Port == 9999) 
                {
                     events.Add(new DetectionEvent
                    {
                        RuleId = "HIDS-N4",
                        RuleName = "Packet Sniffing / C2 Pattern",
                        Severity = AlertSeverity.High,
                        Type = AlertType.BACK,
                        Description = "Detected suspicious traffic pattern (simulated payload match).",
                    });
                }
            }

            return events;
        }
    }
}
