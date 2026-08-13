using NoFences.Model;
using NoFences.Util;
using NoFences.Win32;
using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace NoFences
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Allow context menus to inherit the system dark preference.
            WindowUtil.SetPreferredAppMode(1);

            using (var mutex = new Mutex(true, "No_fences", out bool createdNew))
            {
                if (!createdNew)
                {
                    AppLogger.Info("A second application instance was ignored.");
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                SettingsValidator.NormalizeGlobalSettings();
                DesktopUtil.TrySetDesktopIconsVisible(
                    !Properties.Settings.Default.hide_desktop_icons,
                    out _);
                AppLogger.Info("NoFences started.");

                FenceManager.Instance.LoadFences();
                if (Application.OpenForms.Count == 0)
                    FenceManager.Instance.CreateFence("First fence");

                Application.Run();
            }
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            AppLogger.Error("An unexpected UI error occurred.", e.Exception);
            MessageBox.Show(
                "NoFences encountered an unexpected error. Your fence data was not deleted. " +
                "Details were written to the application log.",
                "NoFences",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            AppLogger.Error(
                "An unhandled application error occurred.",
                e.ExceptionObject as Exception);
        }

        public static string GetAppVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }
    }
}
