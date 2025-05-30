using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace NoFences.Win32
{
    public class DesktopUtil
    {
        private const Int32 GWL_STYLE = -16;
        private const Int32 GWL_HWNDPARENT = -8;
        private const Int32 WS_MAXIMIZEBOX = 0x00010000;
        private const Int32 WS_MINIMIZEBOX = 0x00020000;

        private const int SPI_GETICONMETRICS = 0x002D;
        private const int SPI_GETICONTITLELOGFONT = 0x001F;
        private const int SPI_GETICONSPACING = 0x0048;
        private const int CSIDL_DESKTOP = 0x0000;
        private const int SHGFP_TYPE_CURRENT = 0;
        private const int SHGFP_TYPE_DEFAULT = 1;

        [DllImport("User32.dll", EntryPoint = "GetWindowLong")]
        private extern static Int32 GetWindowLongPtr(IntPtr hWnd, Int32 nIndex);

        [DllImport("User32.dll", EntryPoint = "SetWindowLong")]
        private extern static Int32 SetWindowLongPtr(IntPtr hWnd, Int32 nIndex, Int32 dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpWindowClass, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        // P/Invoke declarations
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(int uiAction, int uiParam, ref int pvParam, int fWinIni);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHParseDisplayName(string name, IntPtr bindingContext, out IntPtr pidl, uint sfgaoIn, out uint psfgaoOut);

        [DllImport("shell32.dll")]
        private static extern int SHGetFolderLocation(IntPtr hwndOwner, int nFolder, IntPtr hToken, uint dwReserved, out IntPtr ppidl);

        [DllImport("shell32.dll")]
        private static extern int SHGetDesktopFolder(out IShellFolder ppshf);

        [DllImport("shell32.dll")]
        private static extern int SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string pszPath, IntPtr pbc, ref Guid riid, out IShellItem ppv);

        [ComImport, Guid("000214E6-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellFolder
        {
            // Define necessary methods (not used directly here)
        }

        [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            // Define necessary methods (not used directly here)
        }


        public static void PreventMinimize(IntPtr handle)
        {
            Int32 windowStyle = GetWindowLongPtr(handle, GWL_STYLE);
            SetWindowLongPtr(handle, GWL_STYLE, windowStyle & ~WS_MAXIMIZEBOX & ~WS_MINIMIZEBOX);
        }

        public static void GlueToDesktop(IntPtr handle)
        {
            IntPtr nWinHandle = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Progman", null);
            SetWindowLongPtr(handle, GWL_HWNDPARENT, nWinHandle.ToInt32());

        }

        public static Point GetClosestSnappedPosition(Point cursorPosition)
        {
            // Get the desktop grid spacing
            int gridSpacingX = 0;
            int gridSpacingY = 0;
            SystemParametersInfo(SPI_GETICONSPACING, 0, ref gridSpacingX, 0);
            SystemParametersInfo(SPI_GETICONSPACING, 0, ref gridSpacingY, 0);

            // Calculate the closest snapped position
            int snappedX = (cursorPosition.X / gridSpacingX) * gridSpacingX;
            int snappedY = (cursorPosition.Y / gridSpacingY) * gridSpacingY;

            return new Point(snappedX, snappedY);
        }

        private void MoveDesktopIcon(string path, Point newPosition)
        {
            /*  IntPtr pidl = IntPtr.Zero;
              try
              {
                  // Parse the display name to get the PIDL
                  SHParseDisplayName(path, IntPtr.Zero, out pidl, 0, out _);

                  if (pidl != IntPtr.Zero)
                  {
                      // Get the desktop folder
                      IShellFolder desktopFolder;
                      SHGetDesktopFolder(out desktopFolder);

                      // Move the icon to the new position
                      desktopFolder.SetPositionOf(pidl, newPosition);
                  }
              }
              finally
              {
                  if (pidl != IntPtr.Zero)
                  {
                      Marshal.FreeCoTaskMem(pidl);
                  }
              }*/
        }
    }
}