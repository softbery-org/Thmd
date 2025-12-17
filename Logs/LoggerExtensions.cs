// Version: 0.0.0.1
// LoggerExtensions.cs
using Thmd.Logs;

namespace Thmd.Consolas;

public static class LoggerExtensions
{
    /// <summary>
    /// Wypisuje wiadomość jako Info z informacją o typie obiektu
    /// </summary>
    public static void WriteLine(this object obj, string message, params object[] args)
    {
        var typeName = obj?.GetType().Name ?? "Unknown";
        Logger.Info($"[{typeName}]: {message}");
    }
}
