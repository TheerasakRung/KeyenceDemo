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

namespace KeyenceDemo
{
    public partial class Form1 : Form
    {
        private List<PrinterData> _printers = new List<PrinterData>();
        private readonly Random _random = new Random();
        private CancellationTokenSource _cts;
        public Form1()
        {
            InitializeComponent();
            InitializePrinters();
            SetDefaultValues();

            btnStop.Enabled = false;
        }

        private void SetDefaultValues()
        {
            // --- เครื่องที่ 1 ---
            // (หมายเหตุ: IP ที่ถูกต้องคือ 192.168.0.1)
            txtIPinkjet1.Text = "192.168.0.1";
            txtBrowseLog1.Text = @"C:\Users\theer\Desktop\TEST_keyence\Jet1\text";
            txtLogInput1.Text = @"C:\Users\theer\Desktop\TEST_keyence\Jet1\log";

            // --- เครื่องที่ 2 ---
            txtIPinkjet2.Text = "192.168.0.2";
            txtBrowseLog2.Text = @"C:\Users\theer\Desktop\TEST_keyence\Jet2\text";
            txtLogInput2.Text = @"C:\Users\theer\Desktop\TEST_keyence\Jet2\log";

            // --- เครื่องที่ 3 ---
            txtIPinkjet3.Text = "192.168.0.3";
            txtBrowseLog3.Text = @"C:\Users\theer\Desktop\TEST_keyence\Jet3\text";
            txtLogInput3.Text = @"C:\Users\theer\Desktop\TEST_keyence\Jet3\log";

            // --- เครื่องที่ 4 ---
            txtIPinkjet4.Text = "192.168.0.4";
            txtBrowseLog4.Text = @"C:\Users\theer\Desktop\TEST_keyence\Jet4\text";
            txtLogInput4.Text = @"C:\Users\theer\Desktop\TEST_keyence\Jet4\log";
        }

        private void InitializePrinters()
        {
            for (int i = 1; i <= 4; i++)
            {
                _printers.Add(new PrinterData { Id = i });
            }
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            SyncUIToData(); // อ่านค่าล่าสุดจากหน้าจอก่อนเสมอ

            foreach (var p in _printers)
            {
                p.IsEnabled = !string.IsNullOrWhiteSpace(p.InputPath);
            }

            if (_printers.Any(p => p.IsEnabled && !p.IsValid()))
            {
                MessageBox.Show("กรุณากรอกข้อมูล Log และ IP ให้ครบทุกช่องที่เปิดใช้งาน", "ข้อมูลไม่ครบ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _cts = new CancellationTokenSource();
            ToggleControls(true);

            try
            {
                // เริ่มการทำงานเบื้องหลัง
                await StartProcessingLoop(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                // ผู้ใช้กด Stop
            }
            finally
            {
                ToggleControls(false);
            }
        }

        // ซิงค์ข้อมูลจากหน้าจอ UI ไปยัง List<PrinterData>
        private void SyncUIToData()
        {
            // อ่านค่าจาก TextBox มาเก็บใน List
            _printers[0].InputPath = txtBrowseLog1.Text; _printers[0].LogPath = txtLogInput1.Text; _printers[0].IpAddress = txtIPinkjet1.Text;
            _printers[1].InputPath = txtBrowseLog2.Text; _printers[1].LogPath = txtLogInput2.Text; _printers[1].IpAddress = txtIPinkjet2.Text;
            _printers[2].InputPath = txtBrowseLog3.Text; _printers[2].LogPath = txtLogInput3.Text; _printers[2].IpAddress = txtIPinkjet3.Text;
            _printers[3].InputPath = txtBrowseLog4.Text; _printers[3].LogPath = txtLogInput4.Text; _printers[3].IpAddress = txtIPinkjet4.Text;
        }

        // ซิงค์ข้อมูลจาก List<PrinterData> กลับไปแสดงผลที่หน้าจอ UI
        private void SyncDataToUI()
        {
            lblStatus1.Text = _printers[0].StatusText; lblStatus1.BackColor = _printers[0].StatusColor;
            lblStatus2.Text = _printers[1].StatusText; lblStatus2.BackColor = _printers[1].StatusColor;
            lblStatus3.Text = _printers[2].StatusText; lblStatus3.BackColor = _printers[2].StatusColor;
            lblStatus4.Text = _printers[3].StatusText; lblStatus4.BackColor = _printers[3].StatusColor;
        }

        private void ToggleControls(bool isRunning)
        {
            Control[] allControls = { txtLogInput1, txtLogInput1, txtIPinkjet1, btnBrowseInput1, btnLogInput1, txtBrowseLog1,
                                      txtLogInput2, txtLogInput2, txtIPinkjet2, btnBrowseInput2, btnLogInput2, txtBrowseLog2,
                                      txtLogInput3, txtLogInput3, txtIPinkjet3, btnBrowseInput3, btnLogInput3, txtBrowseLog3,
                                      txtLogInput4, txtLogInput4, txtIPinkjet4, btnBrowseInput4, btnLogInput4, txtBrowseLog4};


            foreach (var ctrl in allControls)
            {
                ctrl.Enabled = !isRunning;
            }

            if (!isRunning)
            {
                foreach (var p in _printers)
                {
                    p.StatusText = "Status: Idle";
                    p.StatusColor = Color.FromName("Control");
                }
                SyncDataToUI();
            }

            btnStart.Enabled = !isRunning;
            btnStop.Enabled = isRunning;
        }


        private void ProcessPrinter(PrinterData printer)
        {
            try
            {
                var foundFile = Directory.GetFiles(printer.InputPath, "*.txt").FirstOrDefault();
                if (foundFile == null)
                {
                    printer.StatusText = "Status: Waiting for file...";
                    printer.StatusColor = Color.LightBlue;
                    return;
                }

                // --- เพิ่มส่วนตรวจสอบความสมบูรณ์ของไฟล์ ---
                if (!IsFileReady(foundFile))
                {
                    // ถ้าไฟล์ยังไม่พร้อม (ยังเขียนไม่เสร็จ)
                    printer.StatusText = $"File busy: {Path.GetFileName(foundFile)}...";
                    printer.StatusColor = Color.LightYellow;
                    return; // ข้ามไปก่อน แล้วค่อยกลับมาเช็ครอบหน้า
                }
                // ------------------------------------

                // ถ้าไฟล์พร้อมแล้ว ก็เริ่มทำงานตามปกติ
                string fileName = Path.GetFileName(foundFile);

                // อ่านข้อความจากไฟล์
                string fileContent = File.ReadAllText(foundFile).Trim();

                // จำลองการส่งข้อมูล (ในโค้ดของคุณคือการสุ่ม)
                int statusCode = _random.Next(1, 3);

                if (statusCode == 1) // กรณีสำเร็จ
                {
                    printer.StatusText = "Success (Code: 1)";
                    printer.StatusColor = Color.LightGreen;

                    // เขียน Log ด้วยข้อมูลที่อ่านมา
                    WriteToLog(printer.LogPath, $"SUCCESS | File: {fileName} | IP: {printer.IpAddress} | Text: {fileContent}");

                    // ลบไฟล์ทิ้ง
                    File.Delete(foundFile);
                }
                else // กรณี Error
                {
                    printer.StatusText = "Error (Code: 2)";
                    printer.StatusColor = Color.Salmon;
                    WriteToLog(printer.LogPath, $"ERROR | File: {fileName} | IP: {printer.IpAddress} | Failed to process.");
                }
            }
            catch (Exception ex)
            {
                printer.StatusText = "ERROR Accessing File";
                printer.StatusColor = Color.Red;
                System.Diagnostics.Debug.WriteLine($"Error for printer {printer.Id}: {ex.Message}");
            }
        }

        private async Task StartProcessingLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                foreach (var printerData in _printers.Where(p => p.IsValid()))
                {
                    ProcessPrinter(printerData);
                }
                SyncDataToUI();

                // รอ 10 วินาที โดยไม่ทำให้จอค้าง
                await Task.Delay(10000, token);
            }
        }

        private void WriteToLog(string logFolderPath, string message)
        {
            try
            {
                string logFilePath = Path.Combine(logFolderPath, "inkjet_log.txt");
                string entry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}";
                File.AppendAllText(logFilePath, entry);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Log Error: {ex.Message}"); }
        }

        private void btnBrowseInput1_Click(object sender, EventArgs e)
        {
            if (folderBrowseInput1.ShowDialog() == DialogResult.OK)
            {
                Button btn = sender as Button;
                if (btn == btnBrowseInput1) txtBrowseLog1.Text = folderBrowseInput1.SelectedPath;
                if (btn == btnBrowseInput2) txtBrowseLog2.Text = folderBrowseInput1.SelectedPath;
                if (btn == btnBrowseInput3) txtBrowseLog3.Text = folderBrowseInput1.SelectedPath;
                if (btn == btnBrowseInput4) txtBrowseLog4.Text = folderBrowseInput1.SelectedPath;
            }
        }

        private void btnLogInput1_Click(object sender, EventArgs e)
        {
            if (folderLogInput1.ShowDialog() == DialogResult.OK)
            {
                Button btn = sender as Button;
                if (btn == btnLogInput1) txtLogInput1.Text = folderLogInput1.SelectedPath;
                if (btn == btnLogInput2) txtLogInput2.Text = folderLogInput1.SelectedPath;
                if (btn == btnLogInput3) txtLogInput3.Text = folderLogInput1.SelectedPath;
                if (btn == btnLogInput4) txtLogInput4.Text = folderLogInput1.SelectedPath;
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            ToggleControls(false);
            _cts?.Cancel();
        }

        private bool IsFileReady(string filePath)
        {
            try
            {
                // ลองเปิดไฟล์ ถ้าไฟล์กำลังถูกเขียนโดยโปรแกรมอื่น บรรทัดนี้จะเกิด Error
                using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                // การเกิด IOException หมายความว่าไฟล์ยังไม่พร้อมใช้งาน
                return false;
            }

            // ถ้าไม่เกิด Error ใดๆ แสดงว่าไฟล์สมบูรณ์และพร้อมใช้งาน
            return true;
        }
    }
}
