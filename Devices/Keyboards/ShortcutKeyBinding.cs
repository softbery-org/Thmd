// Version: 0.1.4.56
using System;
using System.Windows.Input;

namespace Thmd.Devices.Keyboards
{
    /// <summary>
    /// Represents a customizable keyboard shortcut binding that can trigger an associated action.
    /// </summary>
    public class ShortcutKeyBinding
    {
        /// <summary>
        /// Gets or sets the main key of the shortcut (e.g., <see cref="Key.S"/> or <see cref="Key.F1"/>).
        /// </summary>
        public Key MainKey { get; set; }

        /// <summary>
        /// Gets or sets an optional secondary key that can be used in combination with the main key.
        /// </summary>
        public Key SecondKey { get; set; }

        /// <summary>
        /// Gets or sets an optional modifier key (e.g., <see cref="ModifierKeys.Control"/>, <see cref="ModifierKeys.Alt"/>).
        /// </summary>
        public ModifierKeys? ModifierKey { get; set; }

        /// <summary>
        /// Gets or sets a string representation of the shortcut (e.g., "Ctrl+S" or "Alt+F4").
        /// </summary>
        public string Shortcut { get; set; }

        /// <summary>
        /// Gets or sets a description of the shortcut's purpose or functionality.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the action that should be executed when the shortcut is triggered.
        /// </summary>
        public Action RunAction { get; set; }
    }
}
