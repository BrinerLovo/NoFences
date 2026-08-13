using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NoFences.Util
{
    internal static class AppLogger
    {
        private const long MaximumLogBytes = 1024 * 1024;
        private static readonly object SyncRoot = new object();
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NoFences",
            "Logs");
        private static readonly string LogPath = Path.Combine(LogDirectory, "NoFences.log");

        public static string DirectoryPath => LogDirectory;

        public static void Info(string message)
        {
            Write("INFO", message, null);
        }

        public static void Error(string message, Exception exception)
        {
            Write("ERROR", message, exception);
        }

        private static void Write(string level, string message, Exception exception)
        {
            try
            {
                lock (SyncRoot)
                {
                    Directory.CreateDirectory(LogDirectory);
                    RotateIfNeeded();

                    var line = new StringBuilder(256)
                        .Append(DateTimeOffset.Now.ToString("O"))
                        .Append(" [")
                        .Append(level)
                        .Append("] ")
                        .Append(message ?? string.Empty);
                    if (exception != null)
                        line.AppendLine().Append(exception);

                    File.AppendAllText(LogPath, line.AppendLine().ToString(), Encoding.UTF8);
                }
            }
            catch
            {
                // Logging must never become a second application failure.
            }

            Debug.WriteLine($"[{level}] {message} {exception}");
        }

        private static void RotateIfNeeded()
        {
            if (!File.Exists(LogPath) || new FileInfo(LogPath).Length < MaximumLogBytes)
                return;

            string previousPath = LogPath + ".previous";
            if (File.Exists(previousPath))
                File.Delete(previousPath);
            File.Move(LogPath, previousPath);
        }
    }
}
