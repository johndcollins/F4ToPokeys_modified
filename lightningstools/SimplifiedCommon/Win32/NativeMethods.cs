using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace SimplifiedCommon.Win32
{
    public static class NativeMethods
    {
        public const int GWL_EXSTYLE = (-20);
        public const int WS_EX_TOOLWINDOW = 0x80;
        public const int WS_EX_APPWINDOW = 0x40000;

        public const int VK_SHIFT = 0x10;
        public const int VK_CONTROL = 0x11;
        public const int VK_MENU = 0x12;
        public const int LLKHF_EXTENDED = 0x01;

        public const uint MAPVK_VK_TO_VSC = 0x00;
        public const uint MAPVK_VSC_TO_VK = 0x01;
        public const uint MAPVK_VK_TO_CHAR = 0x02;
        public const uint MAPVK_VSC_TO_VK_EX = 0x03;
        public const uint MAPVK_VK_TO_VSC_EX = 0x04;

        public const int SM_CXSCREEN = 0;
        public const int SM_CYSCREEN = 1;
        public const int SRCCOPY = 13369376;
        public const int CAPTUREBLT = 0x40000000;
        public const int HGDI_ERR = 0xFFFF;
        /// <summary>
        /// WM is just a placeholder class for WM (WindowMessage) definitions
        /// </summary>
        public class WM
        {
            public const int WM_RESIZE = 0x0802;
            public const int WM_MOUSEMOVE = 0x0200;
            public const int WM_NCMOUSEMOVE = 0x00A0;
            public const int WM_NCLBUTTONDOWN = 0x00A1;
            public const int WM_NCLBUTTONUP = 0x00A2;
            public const int WM_NCLBUTTONDBLCLK = 0x00A3;
            public const int WM_LBUTTONDOWN = 0x0201;
            public const int WM_LBUTTONUP = 0x0202;
            public const int WM_KEYDOWN = 0x0100;
        }

        /// <summary>
        /// HT is just a placeholder for HT (HitTest) definitions
        /// </summary>
        public class HT
        {
            public const int HTERROR = (-2);
            public const int HTTRANSPARENT = (-1);
            public const int HTNOWHERE = 0;
            public const int HTCLIENT = 1;
            public const int HTCAPTION = 2;
            public const int HTSYSMENU = 3;
            public const int HTGROWBOX = 4;
            public const int HTSIZE = HTGROWBOX;
            public const int HTMENU = 5;
            public const int HTHSCROLL = 6;
            public const int HTVSCROLL = 7;
            public const int HTMINBUTTON = 8;
            public const int HTMAXBUTTON = 9;
            public const int HTLEFT = 10;
            public const int HTRIGHT = 11;
            public const int HTTOP = 12;
            public const int HTTOPLEFT = 13;
            public const int HTTOPRIGHT = 14;
            public const int HTBOTTOM = 15;
            public const int HTBOTTOMLEFT = 16;
            public const int HTBOTTOMRIGHT = 17;
            public const int HTBORDER = 18;
            public const int HTREDUCE = HTMINBUTTON;
            public const int HTZOOM = HTMAXBUTTON;
            public const int HTSIZEFIRST = HTLEFT;
            public const int HTSIZELAST = HTBOTTOMRIGHT;

            public const int HTOBJECT = 19;
            public const int HTCLOSE = 20;
            public const int HTHELP = 21;
        }

        [Serializable, StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct TEXTMETRIC
        {
            public int tmHeight;
            public int tmAscent;
            public int tmDescent;
            public int tmInternalLeading;
            public int tmExternalLeading;
            public int tmAveCharWidth;
            public int tmMaxCharWidth;
            public int tmWeight;
            public int tmOverhang;
            public int tmDigitizedAspectX;
            public int tmDigitizedAspectY;
            public char tmFirstChar;
            public char tmLastChar;
            public char tmDefaultChar;
            public char tmBreakChar;
            public byte tmItalic;
            public byte tmUnderlined;
            public byte tmStruckOut;
            public byte tmPitchAndFamily;
            public byte tmCharSet;
        }
        public const uint BI_RGB = 0;
        public const uint DIB_RGB_COLORS = 0;
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct BITMAPINFO
        {
            public uint biSize;
            public int biWidth, biHeight;
            public short biPlanes, biBitCount;
            public uint biCompression, biSizeBitmap;
            public int biXPelsPerMeter, biYPelsPerMeter;
            public uint biClrUsed, biClrImportant;
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 256)]
            public uint[] cols;
        }
        public static uint MAKERGB(int r, int g, int b)
        {
            return ((uint)(b & 255)) | ((uint)((r & 255) << 8)) | ((uint)((g & 255) << 16));
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }

            public static implicit operator System.Drawing.Point(POINT p)
            {
                return new System.Drawing.Point(p.X, p.Y);
            }

            public static implicit operator POINT(System.Drawing.Point p)
            {
                return new POINT(p.X, p.Y);
            }
        }

        [StructLayout(LayoutKind.Sequential,
        Pack = 1, CharSet = CharSet.Unicode)]
        public struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(
                UnmanagedType.ByValArray,
                SizeConst = 32)]
            public char[] DeviceName;
            [MarshalAs(
                UnmanagedType.ByValArray,
                SizeConst = 128)]
            public char[] DeviceString;
            public int StateFlags;
            [MarshalAs(
                UnmanagedType.ByValArray,
                SizeConst = 128)]
            public char[] DeviceID;
            [MarshalAs(
                UnmanagedType.ByValArray,
                SizeConst = 128)]
            public char[] DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;

            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;

            public short dmLogPixels;
            public short dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        };

        // PInvoke declaration for EnumDisplaySettings Win32 API
        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        public static extern int EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

        // PInvoke declaration for ChangeDisplaySettings Win32 API
        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        public static extern int ChangeDisplaySettings(ref DEVMODE lpDevMode, int dwFlags);

        // helper for creating an initialized DEVMODE structure
        public static DEVMODE CreateDevmode()
        {
            DEVMODE dm = new DEVMODE();
            dm.dmDeviceName = new String(new char[32]);
            dm.dmFormName = new String(new char[32]);
            dm.dmSize = (short)Marshal.SizeOf(dm);
            return dm;
        }

        // constants
        public const int ENUM_CURRENT_SETTINGS = -1;
        public const int DISP_CHANGE_SUCCESSFUL = 0;
        public const int DISP_CHANGE_BADDUALVIEW = -6;
        public const int DISP_CHANGE_BADFLAGS = -4;
        public const int DISP_CHANGE_BADMODE = -2;
        public const int DISP_CHANGE_BADPARAM = -5;
        public const int DISP_CHANGE_FAILED = -1;
        public const int DISP_CHANGE_NOTUPDATED = -3;
        public const int DISP_CHANGE_RESTART = 1;
        public const int DMDO_DEFAULT = 0;
        public const int DMDO_90 = 1;
        public const int DMDO_180 = 2;
        public const int DMDO_270 = 3;

        //API constants.
        public const int MAX_PATH = 260;

         public const int D3DFMT_A8R8G8B8=21;
     public const int D3DFMT_DXT1=827611204;
     public const int D3DFMT_DXT2=844388420;
     public const int D3DFMT_DXT3=861165636;
     public const int D3DFMT_DXT4=877942852;
     public const int D3DFMT_DXT5=894720068;
     public const int D3DFMT_R8G8B8=20;
     public const int D3DFMT_UNKNOWN=0;
     public const int D3DFMT_X8R8G8B8=22;
     public const int DDPF_ALPHA=2;
     public const int DDPF_ALPHAPIXELS=1;
     public const int DDPF_COMPRESSED=128;
     public const int DDPF_FOURCC=4;
     public const int DDPF_PALETTEINDEXED1=2048;
     public const int DDPF_PALETTEINDEXED2=4096;
     public const int DDPF_PALETTEINDEXED4=8;
     public const int DDPF_PALETTEINDEXED8=32;
     public const int DDPF_PALETTEINDEXEDTO8=16;
     public const int DDPF_RGB=64;
     public const int DDPF_RGBTOYUV=256;
     public const int DDPF_YUV=512;
     public const int DDPF_ZBUFFER=1024;
     public const int DDPF_ZPIXELS=8192;
     public const int DDSCAPS_COMPLEX=8;
     public const int DDSCAPS_MIPMAP=4194304;
     public const int DDSCAPS_TEXTURE=4096;
     public const int DDSCAPS2_CUBEMAP=512;
     public const int DDSCAPS2_CUBEMAP_NEGATIVEX=2048;
     public const int DDSCAPS2_CUBEMAP_NEGATIVEY=8192; 
     public const int DDSCAPS2_CUBEMAP_NEGATIVEZ=32768; 
     public const int DDSCAPS2_CUBEMAP_POSITIVEX=1024; 
     public const int DDSCAPS2_CUBEMAP_POSITIVEY=4096; 
     public const int DDSCAPS2_CUBEMAP_POSITIVEZ=16384; 
     public const int DDSD_ALPHABITDEPTH=128; 
     public const int DDSD_BACKBUFFERCOUNT=32; 
     public const int DDSD_CAPS=1; 
     public const int DDSD_DEPTH=8388608;
     public const int DDSD_HEIGHT=2;
     public const int DDSD_LINEARSIZE=524288;
     public const int DDSD_LPSURFACE=2048;
     public const int DDSD_MIPMAPCOUNT=131072;
     public const int DDSD_PITCH=8;
     public const int DDSD_PIXELFORMAT=4096;
     public const int DDSD_WIDTH=4;
     public const int DDSD_ZBUFFERBITDEPTH = 64;

        //size=32 bytes
        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        public struct DDPIXELFORMAT
        {
            [FieldOffset(0)]
            public int dwSize;
            [FieldOffset(4)]
            public int dwFlags;
            [FieldOffset(8)]
            public int dwFourCC;
            [FieldOffset(12)]
            public int dwRGBBitCount;
            [FieldOffset(12)]
            public int dwYUVBitCount;
            [FieldOffset(12)]
            public int dwZBufferBitDepth;
            [FieldOffset(12)]
            public int dwAlphaBitDepth;
            [FieldOffset(12)]
            public int dwLuminanceBitCount;
            [FieldOffset(12)]
            public int dwBumpBitCount;
            [FieldOffset(16)]
            public int dwRBitMask;
            [FieldOffset(16)]
            public int dwYBitMask;
            [FieldOffset(16)]
            public int dwStencilBitDepth;
            [FieldOffset(16)]
            public int dwLuminanceBitMask;
            [FieldOffset(16)]
            public int dwBumpDuBitMask;
            [FieldOffset(20)]
            public int dwGBitMask;
            [FieldOffset(20)]
            public int dwUBitMask;
            [FieldOffset(20)]
            public int dwZBitMask;
            [FieldOffset(20)]
            public int dwBumpDvBitMask;
            [FieldOffset(24)]
            public int dwBBitMask;
            [FieldOffset(24)]
            public int dwVBitMask;
            [FieldOffset(24)]
            public int dwStencilBitMask;
            [FieldOffset(24)]
            public int dwBumpLuminanceBitMask;
            [FieldOffset(28)]
            public int dwRGBAlphaBitMask;
            [FieldOffset(28)]
            public int dwYUVAlphaBitMask;
            [FieldOffset(28)]
            public int dwLuminanceAlphaBitMask;
            [FieldOffset(28)]
            public int dwRGBZBitMask;
            [FieldOffset(28)]
            public int dwYUVZBitMask;
        }

        //size=16 bytes
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DDSCAPS2
        {
            public int dwCaps;
            public int dwCaps2;
            public int dwCaps3;
            public int dwCaps4;
        }
        //size=8 bytes
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DDCOLORKEY
        {
            public int dwColorSpaceLowValue;
            public int dwColorSpaceHighValue;
        }
        //size=124 bytes
        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        public struct DDSURFACEDESC2
        {
            [FieldOffset(0)]
            public int dwSize;
            [FieldOffset(4)]
            public int dwFlags;
            [FieldOffset(8)]
            public int dwHeight;
            [FieldOffset(12)]
            public int dwWidth;
            [FieldOffset(16)]
            public int lPitch;
            [FieldOffset(16)]
            public int dwLinearSize;
            [FieldOffset(20)]
            public int dwBackBufferCount;
            [FieldOffset(24)]
            public int dwMipMapCount;
            [FieldOffset(24)]
            public int dwRefreshRate;
            [FieldOffset(28)]
            public int dwAlphaBitDepth;
            [FieldOffset(32)]
            public int dwReserved;
            [FieldOffset(36)]
            public int lpSurface;
            [FieldOffset(40)]
            public DDCOLORKEY ddckCKDestOverlay;
            [FieldOffset(40)]
            public int dwEmptyFaceColor;
            [FieldOffset(48)]
            public DDCOLORKEY ddckCKDestBlt;
            [FieldOffset(56)]
            public DDCOLORKEY ddckCKSrcOverlay;
            [FieldOffset(56)]
            public DDCOLORKEY ddckCKSrcBlt;
            [FieldOffset(72)]
            public DDPIXELFORMAT ddpfPixelFormat;
            [FieldOffset(104)]
            public DDSCAPS2 ddsCaps;
            [FieldOffset(120)]
            public int dwTextureStage;
        }




        //API declares. For each API wrapped by this sample DLL (there are 5),
        //there will be a commented call containing the API call in C++ from the
        //Microsoft Win32 API reference. Immediately underneath the C++ call will
        //be the corresponding C# api call.

        /*
        BOOL PathCompactPathEx(
            LPTSTR pszOut,
            LPCTSTR pszSrc,
            UINT cchMax,
            DWORD dwFlags
        );
        */
        [DllImport("shlwapi", EntryPoint = "PathCompactPathEx", SetLastError = true)]
        public static extern bool PathCompactPathEx(
            StringBuilder pszOut,
            string pszSrc,
            int cchMax,
            int dwFlags
        );

        /*
        BOOL PathIsContentType(
            LPCTSTR pszPath,
            LPCTSTR pszContentType
        );
        */
        [DllImport("shlwapi", EntryPoint = "PathIsContentType", SetLastError = true)]
        public static extern bool PathIsContentType(
            string pszPath,
            string pszContentType
        );

        /*
        BOOL PathMakePretty(
            LPTSTR lpPath
        );
        */
        [DllImport("shlwapi", EntryPoint = "PathMakePretty", SetLastError = true)]
        public static extern bool PathMakePretty(
            StringBuilder lpPath
        );

        /*
        BOOL PathCanonicalize(
            LPTSTR lpszDst,
            LPCTSTR lpszSrc
        );
        */
        [DllImport("shlwapi", EntryPoint = "PathCanonicalize", SetLastError = true)]
        public static extern bool PathCanonicalize(
            StringBuilder lpszDst,
            string lpszSrc
        );

        /*
        LPCTSTR PathFindSuffixArray(
            LPCTSTR pszPath,
            LPCTSTR* apszSuffix,
            int iArraySize
        );
        */
        [DllImport("shlwapi", EntryPoint = "PathFindSuffixArray", SetLastError = true)]
        public static extern string PathFindSuffixArray(
            string pszPath,
            string[] apszSuffix,
            int iArraySize
        );

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth,
           int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetTextMetrics(IntPtr hdc, out TEXTMETRIC lptm);

        [DllImportAttribute("user32.dll", SetLastError = true)]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImportAttribute("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll", EntryPoint = "GetWindowDC", SetLastError = true)]
        public static extern IntPtr GetWindowDC(IntPtr ptr);

        [DllImport("user32.dll", EntryPoint = "ReleaseDC", SetLastError = true)]
        public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject", SetLastError = true)]
        public static extern IntPtr DeleteObject(IntPtr hDc);

        [DllImport("gdi32.dll", EntryPoint = "SelectObject", SetLastError = true)]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr bmp);


        [DllImport("User32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public extern static bool EnumDisplayDevices(
            string lpDevice,
            uint iDevNum,
            ref DISPLAY_DEVICE
            lpDisplayDevice,
            uint dwFlags);


        [DllImport("gdi32.dll", EntryPoint = "BitBlt", SetLastError = true)]
        public static extern bool BitBlt(IntPtr hdcDest, int xDest,
                                         int yDest, int wDest,
                                         int hDest, IntPtr hdcSource,
                                         int xSrc, int ySrc, int RasterOp);

        [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleBitmap", SetLastError = true)]
        public static extern IntPtr CreateCompatibleBitmap
                                    (IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleDC", SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO bmi, uint Usage, out IntPtr bits, IntPtr hSection, uint dwOffset);

        [DllImport("user32.dll", EntryPoint = "GetDesktopWindow", SetLastError = true)]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll", EntryPoint = "GetForegroundWindow", SetLastError = true)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", EntryPoint = "GetDC", SetLastError = true)]
        public static extern IntPtr GetDC(IntPtr ptr);

        [DllImport("gdi32.dll", EntryPoint = "DeleteDC", SetLastError = true)]
        public static extern IntPtr DeleteDC(IntPtr hDc);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool DestroyIcon(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hHandle);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool LockWindowUpdate(IntPtr hWndLock);
        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr CreateDC(string lpszDriver, string lpszDevice,
           string lpszOutput, IntPtr lpInitData);

        [DllImport("user32.dll")]
        public static extern IntPtr GetMessageExtraInfo();
        [DllImportAttribute("User32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(String ClassName, String WindowName);
        [DllImportAttribute("User32.dll", SetLastError = true)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);


        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint GetWindowModuleFileName(IntPtr hwnd, [Out] StringBuilder lpszFileName, uint cchFileNameMax);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, [Out] out int ProcessId);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint GetCurrentThreadId();
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        public static extern short GetKeyState(int nVirtKey);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetShortPathName(
            [MarshalAs(UnmanagedType.LPTStr)]
        string path,
            [MarshalAs(UnmanagedType.LPTStr)]
        StringBuilder shortPath,
            int shortPathLength
            );

        [DllImport("Kernel32.dll", EntryPoint = "RtlMoveMemory", SetLastError = true)]
        public static extern void MoveMemory(IntPtr dest, IntPtr src, int size);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError =true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        
        [DllImport("gdi32.dll")]
        public static extern int AddFontResource(string lpszFilename);
        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT Point);
        [DllImport("user32.dll")]
        public static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);
    }
}
