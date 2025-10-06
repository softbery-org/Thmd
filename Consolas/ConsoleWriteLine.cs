// Version: 0.1.7.68
using System;
using System.Reflection;

namespace Thmd.Consolas
{
    /// <summary>
    /// Delegate for getting class name
    /// </summary>
    /// <returns>BaseString of Class</returns>
    public delegate string GetClassNameDelegate(object sender);

    /// <summary>
    /// Static class for extending Console.WriteLine functionality
    /// </summary>
    public static class ConsoleWriteLine
    {
        /// <summary>
        /// Get method base name by type name
        /// </summary>
        /// <returns>class name</returns>
        private static string GetName(object sender)
        {
            if (sender == null)
                return "Unknown";
            return sender.GetType().FullName; // Poprawka: u�yj GetType().BaseString zamiast DeclaringType
        }

        /// <summary>
        /// Write line using Console.WriteLine() with exception
        /// </summary>
        /// <example>
        /// Console.WriteLine($"[{datetime}][{class_name}][{ex.HResult}]: {ex.Message}");
        /// </example>
        /// <param name="sender"></param>
        /// <param name="ex">Exception to use</param>
        public static void WriteLine(this object sender, Exception ex)
        {
            string data = DateTime.Now.ToString("dd-MM-yyyy H:mm:ss"); // Aktualizacja daty
            GetClassNameDelegate delegat = GetName; // U�ycie delegata
            var class_name = delegat(sender);

            if (ex != null)
                Console.WriteLine($"[{data}][{class_name}][{ex.HResult}]: {ex.Message}");
            else
                Console.WriteLine($"[{data}][{class_name}]: [{class_name}]");
        }

        /// <summary>
        /// Write line using Console.WriteLine() with string value
        /// </summary>
        /// <example>
        /// Console.WriteLine($"[{datetime}][{class_name}]: {msg}");
        /// </example>
        /// <param name="sender"></param>
        /// <param name="msg">string value</param>
        public static void WriteLine(this object sender, string msg)
        {
            string data = DateTime.Now.ToString("dd-MM-yyyy H:mm:ss"); // Aktualizacja daty
            GetClassNameDelegate delegat = GetName; // U�ycie delegata
            var class_name = delegat(sender);

            if (!string.IsNullOrEmpty(msg))
                Console.WriteLine($"[{data}][{class_name}]: {msg}");
            else
                Console.WriteLine($"[{data}][{class_name}]: [{class_name}]");
        }
    }
}
