using Microsoft.Win32;
using System;
using System.Windows.Forms;

namespace NoFences.Util
{


    public class StartupManager
    {
        private const string RegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public static void SetStartup(string appName, string appPath, bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKey, true))
                {
                    if (key == null)
                        return;

                    if (enable)
                    {
                        key.SetValue(appName, $"\"{appPath}\"");
                        MessageBox.Show("Application will now start with Windows.", "Startup Enabled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        key.DeleteValue(appName, false);
                        MessageBox.Show("Application startup disabled.", "Startup Disabled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error modifying startup settings: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static bool IsStartupEnabled(string appName)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKey, false))
            {
                return key?.GetValue(appName) != null;
            }
        }
    }

}
