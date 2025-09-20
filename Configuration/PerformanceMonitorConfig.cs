using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thmd.Configuration
{
    public class PerformanceMonitorConfig
    {
        public bool EnablePerformanceMonitoring { get; set; }
        public int MonitoringInterval { get; set; } // in seconds
        public string LogFilePath { get; set; }
        public PerformanceMonitorConfig()
        {
            EnablePerformanceMonitoring = false;
            MonitoringInterval = 60; // Default to 60 seconds
            LogFilePath = "performance.log"; // Default log file path
        }
    }
}
// Version: 0.1.0.68
