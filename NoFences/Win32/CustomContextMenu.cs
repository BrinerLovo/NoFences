using NoFences.Control;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NoFences.Win32
{
    public class CustomContextMenu : ContextMenuStrip
    {
        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE attribute, ref int pvAttribute, uint cbAttribute);

        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE attribute, ref DWM_WINDOW_CORNER_PREFERENCE pvAttribute, uint cbAttribute);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, int crKey, byte bAlpha, uint dwFlags);

        private const uint LWA_COLORKEY = 0x1;
        private const uint LWA_ALPHA = 0x2;
        private const int WS_EX_LAYERED = 0x80000;

        public CustomContextMenu()
        {
            Renderer = new ModernMenuRenderer(); // Use our custom renderer
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            int blur = 2; // 2 = Acrylic, 3 = Mica
            DwmSetWindowAttribute(Handle, DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE, ref blur, sizeof(int));

            // Apply Windows 11 rounded corners
            var preference = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
            if (Handle != IntPtr.Zero)
            {
                DwmSetWindowAttribute(Handle, DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(uint));
            }

            // Enable Dark Mode for Windows 11 menus
            int darkMode = 1;
            DwmSetWindowAttribute(Handle, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
        }

        public enum DWMWINDOWATTRIBUTE
        {
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20, // Enables system dark mode
            DWMWA_WINDOW_CORNER_PREFERENCE = 33, // Enables rounded corners
            DWMWA_SYSTEMBACKDROP_TYPE = 38
        }

        public enum DWM_WINDOW_CORNER_PREFERENCE
        {
            DWMWCP_DEFAULT = 0,
            DWMWCP_DONOTROUND = 1,
            DWMWCP_ROUND = 2,
            DWMWCP_ROUNDSMALL = 3,
        }
    }
}
