using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinEDR_MVP.Config;
using WinEDR_MVP.Engine;
using WinEDR_MVP.Models;
using WinEDR_MVP.Rules.HIDS;
using WinEDR_MVP.Rules.Malware;

namespace WinEDR_MVP
{
    public partial class MainForm : Form
    {
        private DetectionEngine _engine;
        private AlertManager _alertManager;
        private CancellationTokenSource _cts;
        private BindingList<AlertViewModel> _alertsView;

        public MainForm()
        {
            InitializeComponent();
            SetupEngine();
        }

        private void SetupEngine()
        {
            // Load Config
            string configPath = "config.json";
            var config = AppConfig.Load(configPath); // Assuming static Load exists

            _alertManager = new AlertManager();
            _alertManager.OnAlert += HandleAlert;
            
            _engine = new DetectionEngine(_alertManager);

            // Register Rules
            _engine.RegisterRule(new SystemProcessMasqueradingRule(config));
            _engine.RegisterRule(new SuspiciousExecutionRule(config));
            _engine.RegisterRule(new SuspiciousNetworkActivityRule(config));
            _engine.RegisterRule(new StartupPersistenceRule());
            _engine.RegisterRule(new FileScannerRule(config));

            // UI Data Binding
            _alertsView = new BindingList<AlertViewModel>();
            dgvAlerts.DataSource = _alertsView;
        }

        private void HandleAlert(Alert alert)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<Alert>(HandleAlert), alert);
                return;
            }

            _alertsView.Insert(0, new AlertViewModel
            {
                Timestamp = alert.Timestamp.ToLocalTime().ToString("HH:mm:ss"),
                Severity = alert.Severity.ToString(),
                Type = alert.Type.ToString(),
                Title = alert.Title,
                Description = alert.Description
            });
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            if (_cts != null) return;

            _cts = new CancellationTokenSource();
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            statusLabel.Text = "Status: Running...";
            statusLabel.ForeColor = Color.Green;
            scanProgressBar.Visible = true;

            try
            {
                await Task.Run(() =>
                {
                    Console.WriteLine("Starting Single Scan Cycle"); // Debug
                    _engine.RunCycle();
                }, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Stopped
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Engine Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                statusLabel.Text = "Status: Stopped";
                statusLabel.ForeColor = Color.Red;
                scanProgressBar.Visible = false;
                _cts = null;
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            _cts?.Cancel();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _cts?.Cancel();
            base.OnFormClosing(e);
        }
    }

    // Simple ViewModel for the Grid
    public class AlertViewModel
    {
        public string Timestamp { get; set; }
        public string Severity { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
}
