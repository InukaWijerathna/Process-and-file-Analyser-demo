# 🛡️ WinEDR MVP — Windows Endpoint Detection & Response

A lightweight, real-time **Host-based Intrusion Detection System (HIDS)** and **Malware Scanner** built with **.NET 10.0 Windows Forms**. WinEDR MVP monitors running processes, network connections, file-system artefacts, and registry persistence mechanisms to surface suspicious activity on a Windows endpoint.

> **Disclaimer:** This project is an educational demonstration / proof-of-concept. It is not intended to replace production-grade EDR solutions.

---

## ✨ Features

| Category | Capability |
|---|---|
| **Process Analysis** | Detects system-process masquerading (e.g., a fake `svchost.exe` running outside `C:\Windows\System32`) |
| **Explorer.exe Guard** | Strictly validates that `explorer.exe` runs only from `C:\WINDOWS` — the legitimate path for your taskbar, Start menu, and File Explorer |
| **Suspicious Execution** | Flags processes launched from untrusted directories (Downloads, Temp, Desktop) |
| **Network Monitoring** | Identifies connections to known-bad ports (4444, 6667, 1337, 31337), port-scanning behaviour, traffic bursts, and C2-like patterns |
| **Startup Persistence** | Scans both `HKCU` and `HKLM` Run/RunOnce registry keys for unexpected auto-start entries |
| **Malware File Scan** | Searches untrusted directories for double-extension files (`.pdf.exe`) and hidden executables |
| **SeDebugPrivilege** | Enables the debug privilege at startup via P/Invoke so the scanner can inspect protected system processes |
| **Fluent UI** | Dark-mode interface styled after the Windows 11 Fluent Design System |

---

## 📸 Screenshots

_Run the application to see the dark-mode Fluent UI with real-time alert results in a clean data grid._

---

## 🏗️ Architecture

```
WinEDR_MVP/
├── Config/
│   └── AppConfig.cs            # JSON-based configuration model
├── Engine/
│   ├── AlertManager.cs         # Event-driven alert pipeline
│   └── DetectionEngine.cs      # Rule orchestrator — runs all registered rules
├── Interfaces/
│   └── IDetectionRule.cs       # Common interface for all detection rules
├── Models/
│   └── Alerts.cs               # Alert, DetectionEvent, Severity & Type enums
├── Rules/
│   ├── HIDS/
│   │   ├── SystemProcessMasqueradingRule.cs   # HIDS-P1
│   │   ├── SuspiciousExecutionRule.cs         # HIDS-P2
│   │   ├── SuspiciousNetworkActivityRule.cs   # HIDS-N1/N2/N3/N4
│   │   └── StartupPersistenceRule.cs          # HIDS-R1
│   └── Malware/
│       └── FileScannerRule.cs                 # MAL-F1 / MAL-F5
├── MainForm.cs                 # WinForms UI logic (single-scan, progress bar)
├── MainForm.Designer.cs        # Fluent Design styled controls
├── Program.cs                  # Entry point — SeDebugPrivilege + UAC handling
├── app.manifest                # UAC application manifest
├── config.json                 # Runtime configuration
└── WinEDR_MVP.csproj           # Project file (.NET 10.0, WinForms)
```

### How it works

1. **Startup** — `Program.cs` attempts to enable `SeDebugPrivilege` via Win32 P/Invoke. If the app is not running as Administrator, a warning dialog is shown but the app continues with reduced visibility.
2. **Configuration** — `config.json` is loaded (or auto-generated on first run) defining trusted processes, trusted/untrusted paths, and per-rule settings.
3. **Engine** — When the user clicks **Start Engine**, a single scan cycle is dispatched on a background thread. The `DetectionEngine` iterates through all registered `IDetectionRule` implementations.
4. **Rules** — Each rule evaluates the current system state and returns a list of `DetectionEvent` objects.
5. **Alerts** — Events are converted to `Alert` objects, pushed through the `AlertManager`, and displayed in the data grid in real time.
6. **Completion** — After the scan cycle finishes, the progress bar hides and the UI resets. The engine does **not** loop continuously.

---

## 🔍 Detection Rules

### HIDS (Host-based Intrusion Detection)

| Rule ID | Name | Severity | Description |
|---------|------|----------|-------------|
| **HIDS-P1** | System Process Masquerading | 🔴 High | Flags system processes (e.g., `svchost.exe`, `services.exe`) running from unexpected file paths. `explorer.exe` is strictly validated against `C:\WINDOWS\explorer.exe`. |
| **HIDS-P2** | Suspicious Execution Path | 🟡 Medium | Detects any running process whose executable is located in an untrusted directory (Downloads, Temp, Desktop). |
| **HIDS-N1** | Suspicious Outbound Port | 🔴 High | Alerts on active TCP connections to known malicious ports: `4444`, `6667`, `1337`, `31337`. |
| **HIDS-N2** | Port Scanning Behaviour | 🟡 Medium | Flags a remote IP if the host has connections to more than 20 distinct ports on that IP. |
| **HIDS-N3** | Network Traffic Burst | 🟡 Medium | Alerts when the total active TCP connection count exceeds 100. |
| **HIDS-N4** | C2 Pattern (Simulated) | 🔴 High | Simulated rule — triggers on connections to port `9999` to demonstrate payload/C2 detection logic. |
| **HIDS-R1** | Startup Persistence | 🔴 High | Scans `HKCU\...\Run`, `HKCU\...\RunOnce`, `HKLM\...\Run`, and `HKLM\...\RunOnce` for programs that auto-start with Windows. |

### Malware (File-based)

| Rule ID | Name | Severity | Description |
|---------|------|----------|-------------|
| **MAL-F1** | Suspicious File Extension | 🔴 High | Detects double-extension files (`.pdf.exe`, `.doc.exe`, `.txt.js`) in untrusted directories. |
| **MAL-F5** | Hidden Executable | 🟡 Medium | Flags `.exe` or `.ps1` files with the Windows "Hidden" attribute set in untrusted directories. |

> **Note:** File scans use `SearchOption.TopDirectoryOnly` — subdirectories are **not** recursively scanned.

---

## ⚙️ Configuration

The `config.json` file controls runtime behaviour:

```json
{
  "TrustedSystemProcesses": [
    "svchost.exe",
    "explorer.exe",
    "lsass.exe",
    "services.exe",
    "csrss.exe"
  ],
  "TrustedExecutionPaths": [
    "C:\\Windows\\System32",
    "C:\\Windows\\SysWOW64",
    "C:\\Program Files"
  ],
  "UntrustedExecutionPaths": [
    "%USERPROFILE%\\Downloads",
    "%USERPROFILE%\\AppData\\Local\\Temp",
    "C:\\Temp"
  ],
  "Rules": {
    "HIDS-P1": { "Enabled": true, "Threshold": 1, "Severity": "High" },
    "HIDS-N3": { "Enabled": true, "Threshold": 100, "Severity": "Medium" }
  },
  "NetworkScanWindowSeconds": 10
}
```

| Key | Description |
|-----|-------------|
| `TrustedSystemProcesses` | Process names that should only run from trusted paths |
| `TrustedExecutionPaths` | Directories considered safe for process execution |
| `UntrustedExecutionPaths` | Directories scanned for malware indicators (supports `%USERPROFILE%`) |
| `Rules` | Per-rule overrides for enabling/disabling and setting thresholds |
| `NetworkScanWindowSeconds` | Time window for stateful network analysis |

---

## 🚀 Getting Started

### Prerequisites

- **Windows 10/11**
- **.NET 10.0 SDK** — [Download](https://dotnet.microsoft.com/download)

### Build & Run

```bash
# Clone the repository
git clone https://github.com/InukaWijerathna/Process-and-file-Analyser-demo.git
cd Process-and-file-Analyser-demo

# Build the project
dotnet build

# Run the application
dotnet run
```

### Run with Administrator privileges (recommended)

For full process inspection capabilities (SeDebugPrivilege), run the built executable as Administrator:

```
1. Navigate to bin\Debug\net10.0-windows\
2. Right-click WinEDR_MVP.exe → "Run as administrator"
```

If launched without admin rights, the application will still work but may not be able to inspect certain protected system processes.

---

## 🔐 SeDebugPrivilege

WinEDR MVP uses the Windows `SeDebugPrivilege` to read information from system-level processes. This is necessary because:

- **Without it:** `Process.MainModule` throws `Access Denied` for processes like `svchost.exe`, `lsass.exe`, and `csrss.exe`, making process-path validation impossible.
- **With it:** The scanner can read the full executable path of every running process to verify it against trusted locations.

The privilege is enabled at startup via Win32 P/Invoke calls to:
1. `OpenProcessToken` — Opens the current process token
2. `LookupPrivilegeValue` — Resolves the `SeDebugPrivilege` LUID
3. `AdjustTokenPrivileges` — Enables the privilege in the token

> ⚠️ This privilege is only available when the application is running as Administrator.

---

## 🎨 UI Design

The interface follows the **Windows 11 Fluent Design System** principles:

- **Dark Mode** — Deep charcoal backgrounds (`#202020`, `#2D2D2D`)
- **Typography** — Segoe UI Semibold for headings, Segoe UI for body text
- **Flat Controls** — Zero-border buttons with Fluent accent colours (Blue `#005FB8`, Red `#C42B1C`)
- **Clean Data Grid** — Borderless table with horizontal separators, generous row padding, and accent-blue selection highlight
- **Progress Bar** — Marquee-style progress indicator visible during scans

---

## 📄 License

This project is provided as-is for educational and demonstration purposes.

---

## 🤝 Contributing

Contributions are welcome! Feel free to open issues or pull requests for:

- New detection rules
- UI improvements
- Performance optimisations
- Documentation updates
