using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using WiimoteLib;

namespace WiiBalanceReaderGUI
{
    public partial class Form1 : Form
    {
        // --- Fields ---
        private float tareWeight;
        private float tareCoPX;
        private float tareCoPY;
        private bool isTared = false;
        private bool running = true;
        private bool streaming = false;

        private Wiimote wiimote;
        private DateTime startTime;
        private StreamWriter csvWriter;
        private string participantName;
        private string filePath;
        private string taskType;

        public Form1()
        {
            InitializeComponent();
        }

        // --- Button Handlers ---

        private void btnConnect_Click(object sender, EventArgs e)
        {
            participantName = txtName.Text.Trim();
            taskType = cmbTaskType.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(participantName) || string.IsNullOrEmpty(taskType))
            {
                MessageBox.Show("Enter participant name and select task type.");
                return;
            }

            string folderPath = Path.Combine("data", participantName);
            Directory.CreateDirectory(folderPath);

            filePath = Path.Combine(folderPath,
                $"BalanceBoardData_{taskType}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

            wiimote = new Wiimote();

            try
            {
                lblStatus.Text = "Searching for Wiimote...";
                wiimote.Connect();
                lblStatus.Text = "Connected!";

                if (wiimote.WiimoteState.ExtensionType != ExtensionType.BalanceBoard)
                {
                    MessageBox.Show("Not a Balance Board. Please connect one.");
                    return;
                }

                wiimote.SetReportType(InputReport.IRAccel, true);
                wiimote.WiimoteChanged += OnWiimoteChanged;

                csvWriter = new StreamWriter(filePath);
                Thread.Sleep(2000);
                TareWeight();

                lblStatus.Text = "Ready. Use Start/Stop/Reset.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (wiimote == null) return;

            ResetCenterOfPressure();
            startTime = DateTime.Now;

            csvWriter.WriteLine("RealTimestamp,ElapsedSeconds,TopLeft(kg),TopRight(kg),BottomLeft(kg),BottomRight(kg),TotalWeight(kg),CoPX(cm),CoPY(cm)");
            streaming = true;

            int duration = taskType == "simple" ? 60000 : 39000;

            Task.Run(async () =>
            {
                await Task.Delay(duration);
                streaming = false;
                this.Invoke((Action)(() => lblStatus.Text = "Streaming stopped."));
            });

            lblStatus.Text = "Streaming started...";
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            streaming = false;
            lblStatus.Text = "Streaming stopped.";
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            ResetCenterOfPressure();
            lblStatus.Text = "Center of Pressure reset.";
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            running = false;
            wiimote?.Disconnect();
            csvWriter?.Close();
            Application.Exit();
        }

        // --- Logic methods ---

        private void TareWeight()
        {
            var bb = wiimote.WiimoteState.BalanceBoardState;
            tareWeight = bb.WeightKg;
            tareCoPX = bb.CenterOfGravity.X;
            tareCoPY = bb.CenterOfGravity.Y;
            isTared = true;
        }

        private void ResetCenterOfPressure()
        {
            var bb = wiimote.WiimoteState.BalanceBoardState;
            tareCoPX = bb.CenterOfGravity.X;
            tareCoPY = bb.CenterOfGravity.Y;
        }

        private void OnWiimoteChanged(object sender, WiimoteChangedEventArgs e)
        {
            if (!streaming || !running) return;

            var bb = e.WiimoteState.BalanceBoardState;
            double elapsedSeconds = (DateTime.Now - startTime).TotalSeconds;

            float adjustedWeight = isTared ? bb.WeightKg - tareWeight : bb.WeightKg;
            float adjustedX = isTared ? bb.CenterOfGravity.X - tareCoPX : bb.CenterOfGravity.X;
            float adjustedY = isTared ? bb.CenterOfGravity.Y - tareCoPY : bb.CenterOfGravity.Y;

            string realTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff");

            csvWriter.WriteLine($"{realTime},{elapsedSeconds:F3}," +
                $"{bb.SensorValuesKg.TopLeft:F2},{bb.SensorValuesKg.TopRight:F2}," +
                $"{bb.SensorValuesKg.BottomLeft:F2},{bb.SensorValuesKg.BottomRight:F2}," +
                $"{adjustedWeight:F2},{adjustedX:F2},{adjustedY:F2}");
            csvWriter.Flush();
        }
    }
}