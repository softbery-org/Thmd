// Version: 0.1.4.13
using System;
using System.Windows.Input;

namespace Thmd.Devices.Keyboards
{
    /// <summary>
    /// 
    /// </summary>
    public class ShortcutKeyBinding
    {
        /// <summary>
        /// 
        /// </summary>
        public Key MainKey { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Key SecondKey { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ModifierKeys? ModifierKey { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Shortcut { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Action RunAction { get; set; }
    }
}
