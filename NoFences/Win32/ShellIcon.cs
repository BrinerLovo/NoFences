using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace NoFences.Win32
{
    public static class ShellIcon
    {
        public static Icon GetThumbnail(string filePath, int width, int height)
        {
            IntPtr hBitmap = IntPtr.Zero;
            try
            {
                // Get the thumbnail from Windows Shell
                hBitmap = GetHBitmapThumbnail(filePath, width, height);
                if (hBitmap == IntPtr.Zero)
                    return null;

                using (Bitmap bmp = Image.FromHbitmap(hBitmap))
                {
                    return Icon.FromHandle(bmp.GetHicon()); // Convert Bitmap to Icon
                }
            }
            finally
            {
                if (hBitmap != IntPtr.Zero)
                    DeleteObject(hBitmap);
            }
        }

        private static IntPtr GetHBitmapThumbnail(string filePath, int width, int height)
        {
            IntPtr hBitmap = IntPtr.Zero;
            IShellItem shellItem;
            Guid thumbnailProviderGuid = new Guid("e357fccd-a995-4576-b01f-234630154e96"); // IThumbnailProvider CLSID

            var guid = typeof(IShellItem).GUID;
            int result = SHCreateItemFromParsingName(filePath, IntPtr.Zero, ref guid, out shellItem);
            if (result == 0)
            {
                IThumbnailProvider thumbnailProvider;
                shellItem.BindToHandler(IntPtr.Zero, ref thumbnailProviderGuid, typeof(IThumbnailProvider).GUID, out thumbnailProvider);
                if (thumbnailProvider != null)
                {
                    SIZE size = new SIZE { cx = width, cy = height };
                    thumbnailProvider.GetThumbnail(size, out hBitmap, out uint _);
                    Marshal.ReleaseComObject(thumbnailProvider);
                }
                Marshal.ReleaseComObject(shellItem);
            }
            return hBitmap;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern int SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string pszPath, IntPtr pbc, [In] ref Guid riid, out IShellItem ppv);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE
        {
            public int cx;
            public int cy;
        }

        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IThumbnailProvider ppv);
        }

        [ComImport]
        [Guid("e357fccd-a995-4576-b01f-234630154e96")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IThumbnailProvider
        {
            void GetThumbnail(SIZE size, out IntPtr hBitmap, out uint alphaType);
        }

    }

}
