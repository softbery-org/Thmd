// Version: 0.0.0.4
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thmd.Configuration
{
    /// <summary>
    /// Represents a generic configuration interface for loading and saving configuration settings.
    /// </summary>
    public interface IConfig
    {
        /// <summary>
        /// Loads the configuration settings from a persistent storage.
        /// </summary>
        public void Load();
        /// <summary>
        /// Saves the current configuration settings to a persistent storage.
        /// </summary>
        public void Save();
    }
}
