using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeyenceDemo
{
    public class PrinterData
    {
        public int Id { get; set; }
        public string InputPath { get; set; } = "";
        public string LogPath { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public string StatusText { get; set; } = "Status: Idle";
        public Color StatusColor { get; set; } = Color.FromName("Control");
        public bool IsEnabled { get; set; } = false;

        public bool IsValid() => IsEnabled && !string.IsNullOrWhiteSpace(InputPath) && !string.IsNullOrWhiteSpace(LogPath) && !string.IsNullOrWhiteSpace(IpAddress);
    }
}
