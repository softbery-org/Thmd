// Version: 0.1.5.40
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thmd.Configuration
{
    /// <summary>
    /// Represents the configuration settings for performance monitoring.
    /// </summary>
    public class PerformanceMonitorConfig : IConfig
    {
        private readonly object _lock = new();
        /// <summary>
        /// Gets or sets a value indicating whether performance monitoring is enabled.
        /// </summary>
        public bool EnablePerformanceMonitoring { get; set; }
        /// <summary>
        /// Gets or sets the monitoring interval in seconds.
        /// </summary>
        public int MonitoringInterval { get; set; } // in seconds
        /// <summary>
        /// Gets or sets the file path where performance logs will be saved.
        /// </summary>
        public string LogFilePath { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceMonitorConfig"/> class with default settings.
        /// </summary>
        public PerformanceMonitorConfig()
        {
            EnablePerformanceMonitoring = false;
            MonitoringInterval = 60; // Default to 60 seconds
            LogFilePath = "performance.log"; // Default log file path
        }

        /// <summary>
        /// Loads the performance monitor configuration from the JSON file.
        /// </summary>
        public void Load()
        {
            lock (_lock)
            {
                var loadedConfig = Config.LoadFromJsonFile<PerformanceMonitorConfig>(Config.PerformanceMonitorConfigPath);
                EnablePerformanceMonitoring = loadedConfig.EnablePerformanceMonitoring;
                MonitoringInterval = loadedConfig.MonitoringInterval;
                LogFilePath = loadedConfig.LogFilePath;
            }
        }

        /// <summary>
        /// Saves the performance monitor configuration to the JSON file.
        /// </summary>
        public void Save()
        {
            lock (_lock)
            {
                Config.SaveToFile(Config.PerformanceMonitorConfigPath, this);
            }
        }
    }
}
