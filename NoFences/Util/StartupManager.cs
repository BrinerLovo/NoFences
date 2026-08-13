using Microsoft.Win32;
using System;

namespace NoFences.Util
{
    internal static class StartupManager
    {
        public const string ApplicationName = "NoFences";
        private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public static bool TrySetStartup(string appPath, bool enable, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath, true))
                {
                    if (key == null)
                    {
                        errorMessage = "The Windows startup registry key could not be opened.";
                        return false;
                    }

                    if (enable)
                        key.SetValue(ApplicationName, $"\"{appPath}\"");
                    else
                        key.DeleteValue(ApplicationName, false);
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                AppLogger.Error("Unable to update the Windows startup setting.", ex);
                return false;
            }
        }

        public static bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath, false))
                    return key?.GetValue(ApplicationName) != null;
            }
            catch (Exception ex)
            {
                AppLogger.Error("Unable to read the Windows startup setting.", ex);
                return false;
            }
        }
    }
}
