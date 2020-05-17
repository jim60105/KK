/**
 * Windows API wrapper
 * 
 * License: CC0, https://creativecommons.org/publicdomain/zero/1.0/
 * 
 * Author: Kirurobo, http://twitter.com/kirurobo
 * Author: Ru--en, http://twitter.com/ru__en
 */
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Kirurobo
{
    public class WinApi
    {

        #region Windows API
        public static readonly int GWL_STYLE = -16;

        public static readonly int SW_HIDE = 0;
        public static readonly int SW_MAXIMIZE = 3;
        public static readonly int SW_MINIMIZE = 6;
        public static readonly int SW_RESTORE = 9;
        public static readonly int SW_SHOW = 5;

        public static readonly uint SWP_REFRESH = 0x237;
        public static readonly uint SWP_NOSIZE = 0x1;
        public static readonly uint SWP_NOMOVE = 0x2;
        public static readonly uint SWP_NOZORDER = 0x4;
        public static readonly uint SWP_NOACTIVATE = 0x10;
        public static readonly uint SWP_FRAMECHANGED = 0x20;
        public static readonly uint SWP_SHOWWINDOW = 0x40;
        public static readonly uint SWP_NOCOPYBITS = 0x100;
        public static readonly uint SWP_NOOWNERZORDER = 0x200;
        public static readonly uint SWP_NOREPOSITION = 0x200;
        public static readonly uint SWP_NOSENDCHANGING = 0x400;
        public static readonly uint SWP_ASYNCWINDOWPOS = 0x4000;

        public static readonly long WS_BORDER = 0x00800000L;
        public static readonly long WS_VISIBLE = 0x10000000L;
        public static readonly long WS_OVERLAPPED = 0x00000000L;
        public static readonly long WS_CAPTION = 0x00C00000L;
        public static readonly long WS_SYSMENU = 0x00080000L;
        public static readonly long WS_THICKFRAME = 0x00040000L;
        public static readonly long WS_ICONIC = 0x20000000L;
        public static readonly long WS_MINIMIZE = 0x20000000L;
        public static readonly long WS_MAXIMIZE = 0x01000000L;
        public static readonly long WS_MINIMIZEBOX = 0x00020000L;
        public static readonly long WS_MAXIMIZEBOX = 0x00010000L;
        public static readonly long WS_POPUP = 0x80000000L;
        public static readonly long WS_OVERLAPPEDWINDOW = 0x00CF0000L;

        public static readonly long WS_EX_TRANSPARENT = 0x00000020L;
        public static readonly long WS_EX_LAYERED = 0x00080000L;
        public static readonly long WS_EX_TOPMOST = 0x00000008L;
        public static readonly long WS_EX_OVERLAPPEDWINDOW = 0x00000300L;
        public static readonly long WS_EX_ACCEPTFILES = 0x00000010L;

        public static readonly IntPtr HWND_TOP = new IntPtr(0);
        public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        public static readonly uint GA_PARENT = 1;
        public static readonly uint GA_ROOT = 2;
        public static readonly uint GA_ROOTOWNER = 3;
        public static readonly uint GW_HWNDFIRST = 0;
        public static readonly uint GW_HWNDLAST = 1;
        public static readonly uint GW_HWNDNEXT = 2;
        public static readonly uint GW_HWNDPREV = 3;
        public static readonly uint GW_OWNER = 4;
        public static readonly uint GW_CHILD = 5;

        public static readonly uint WM_IME_CHAR = 0x0286;
        public static readonly uint WM_SETTEXT = 0x000C;
        public static readonly uint WM_NCDESTROY = 0x082;
        public static readonly uint WM_WINDOWPOSCHANGING = 0x046;
        public static readonly uint WM_DROPFILES = 0x233;

        public static readonly uint ULW_COLORKEY = 0x00000001;
        public static readonly uint ULW_ALPHA = 0x00000002;
        public static readonly uint ULW_OPAQUE = 0x00000004;
        
        public static readonly uint LWA_COLORKEY = 0x00000001;
        public static readonly uint LWA_ALPHA = 0x00000002;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool EnumWindowsDelegate(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct RECT
        {
            public int left, top, right, bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct COLORREF
        {
            public uint color;

            public COLORREF(uint color)
            {
                this.color = color;
            }

            public COLORREF(byte r, byte g, byte b)
            {
                this.color = (uint)(b * 0x10000 + g * 0x100 + r);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsDelegate lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetWindowText(IntPtr hWnd, [MarshalAs(UnmanagedType.LPStr)]StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetClassName(IntPtr hWnd, [MarshalAs(UnmanagedType.LPStr)]StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out IntPtr lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpszClass, string lpszTitle);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

        [DllImport("user32.dll")]
        public static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern long SetWindowLong(IntPtr hWnd, int nIndex, long value);

        [DllImport("user32.dll")]
        public static extern long GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        public static extern int SetWindowLongPtr32(IntPtr hWnd, int nIndex, int dwNewPtr);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, uint msg, long wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool UpdateLayeredWindow(IntPtr hWnd, IntPtr hdcDst, IntPtr pptDst, IntPtr psize, IntPtr hdcSrc, IntPtr pptSrc, COLORREF crKey, IntPtr pblend, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern bool SetLayeredWindowAttributes(IntPtr hWnd, COLORREF crKey, byte bAlpha, uint dwFlags);

        #endregion

        #region for mouse events
        public static readonly ulong MOUSEEVENTF_ABSOLUTE = 0x8000;
        public static readonly ulong MOUSEEVENTF_LEFTDOWN = 0x0002;
        public static readonly ulong MOUSEEVENTF_LEFTUP = 0x0004;
        public static readonly ulong MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        public static readonly ulong MOUSEEVENTF_MIDDLEUP = 0x0040;
        public static readonly ulong MOUSEEVENTF_MOVE = 0x0001;
        public static readonly ulong MOUSEEVENTF_RIGHTDOWN = 0x0008;
        public static readonly ulong MOUSEEVENTF_RIGHTUP = 0x0010;
        public static readonly ulong MOUSEEVENTF_XDOWN = 0x0080;
        public static readonly ulong MOUSEEVENTF_XUP = 0x0100;
        public static readonly ulong MOUSEEVENTF_WHEEL = 0x0800;
        public static readonly ulong MOUSEEVENTF_HWHEEL = 0x1000;
        public static readonly ulong XBUTTON1 = 0x0001;
        public static readonly ulong XBUTTON2 = 0x0002;

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        public struct POINT
        {
            public int x, y;

            public override string ToString()
            {
                return "(" + x + "," + y + ")";
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CURSORINFO
        {
            public int cbSize;
            public int flags;       // 0:Hidden, 1:Showing, 2:Suppressed(Window8-)
            public IntPtr hCursor;
            public POINT ptScreenPos;

            public override string ToString()
            {
                return string.Format("Flags:{0}, HCursor:{1}, Point:{2}", flags, hCursor, ptScreenPos.ToString());
            }
        }

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT point);

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetCursorInfo(ref CURSORINFO pcursorinfo);

        [DllImport("user32.dll")]
        public static extern uint mouse_event(ulong dwFlags, int dx, int dy, ulong dwData, IntPtr dwExtraInfo);

        public static readonly int GWL_EXSTYLE = -20;
        public static readonly int GWLP_HINSTANCE = -6;
        public static readonly int GWLP_ID = -12;
        public static readonly int GWLP_STYLE = -16;
        public static readonly int GWLP_USERDATA = -21;
        public static readonly int GWLP_WNDPROC = -4;

        [DllImport("user32.dll")]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        #endregion

        #region for shell functions

        [DllImport("shell32.dll")]
        public static extern void DragAcceptFiles(IntPtr hWnd, bool bAccept);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern uint DragQueryFile(IntPtr hDrop, uint iFile, [MarshalAs(UnmanagedType.LPWStr)]StringBuilder lpszFile, uint cch);

        [DllImport("shell32.dll")]
        public static extern void DragFinish(IntPtr hDrop);

        /// <summary>
        /// Set window procedure.
        /// This method is not implemented in user32.dll
        /// </summary>
        /// <returns>Previous window procedure</returns>
        public static IntPtr SetWindowProcedure(IntPtr hWnd, IntPtr wndProcPtr)
        {
            // Reference https://qiita.com/DandyMania/items/d1404c313f67576d395f

            if (IntPtr.Size == 8)
            {
                // 64bit
                return SetWindowLongPtr(hWnd, GWLP_WNDPROC, wndProcPtr);
            }
            else
            {
                // 32bit
                return new IntPtr(SetWindowLongPtr32(hWnd, GWLP_WNDPROC, wndProcPtr.ToInt32()));
            }
        }
        #endregion

        #region Hooks
        public static readonly int WH_CALLWNDPROC = 4;
        public static readonly int WH_CALLWNDPROCRET = 12;
        public static readonly int WH_CBT = 5;
        public static readonly int WH_DEBUG = 9;
        public static readonly int WH_FOREGROUNDIDLE = 11;
        public static readonly int WH_GETMESSAGE = 3;
        public static readonly int WH_JOURNALPLAYBACK = 1;
        public static readonly int WH_JOURNALRECORD = 0;
        public static readonly int WH_KEYBOARD = 2;
        public static readonly int WH_KEYBOARD_LL = 13;
        public static readonly int WH_MOUSE = 7;
        public static readonly int WH_MOUSE_LL = 14;
        public static readonly int WH_MSGFILTER = -1;
        public static readonly int WH_SHELL = 10;
        public static readonly int WH_SYSMSGFILTER = 6;

        [StructLayout(LayoutKind.Sequential)]
        public struct CWPSTRUCT
        {
            public long lParam;
            public long wParam;
            public uint message;
            public IntPtr hwnd;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public ushort time;
            public POINT pt;
        }

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hmod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, ref MSG lParam);

        [DllImport("kernel32.dll")]
        public static extern long GetLastError();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr HookProc(int code, IntPtr wParam, ref MSG lParam);

        #endregion

        #region Common controll

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class OpenFileName
        {
            public int structSize = 0;
            public IntPtr dlgOwner = IntPtr.Zero;
            public IntPtr instance = IntPtr.Zero;
            public string filter = null;
            public string customFilter = null;
            public int maxCustFilter = 0;
            public int filterIndex = 0;
            public string file = null;
            public int maxFile = 0;
            public string fileTitle = null;
            public int maxFileTitle = 0;
            public string initialDir = null;
            public string title = null;
            public int flags = 0;
            public short fileOffset = 0;
            public short fileExtension = 0;
            public string defExt = null;
            public IntPtr custData = IntPtr.Zero;
            public IntPtr hook = IntPtr.Zero;
            public string templateName = null;
            public IntPtr reservedPtr = IntPtr.Zero;
            public int reservedInt = 0;
            public int flagsEx = 0;

            public static readonly int OFN_ALLOWMULTISELECT = 0x00000200;
            public static readonly int OFN_CREATEPROMPT = 0x00000200;
            public static readonly int OFN_DONTADDTORECENT = 0x02000000;
            public static readonly int OFN_ENABLEHOOK = 0x00000020;
            public static readonly int OFN_ENABLEINCLUDENOTIFY = 0x00400000;
            public static readonly int OFN_ENABLESIZING = 0x00800000;
            public static readonly int OFN_ENABLETEMPLATE = 0x00000040;
            public static readonly int OFN_ENABLETEMPLATEHANDLE = 0x00000080;
            public static readonly int OFN_EXPLORER = 0x00080000;
            public static readonly int OFN_EXTENSIONDIFFERENT = 0x00000400;
            public static readonly int OFN_FILEMUSTEXIST = 0x00001000;
            public static readonly int OFN_FORCESHOWHIDDEN = 0x10000000;
            public static readonly int OFN_HIDEREADONLY = 0x00000004;
            public static readonly int OFN_LONGNAMES = 0x00200000;
            public static readonly int OFN_NOCHANGEDIR = 0x00000008;
            public static readonly int OFN_NODEREFERENCELINKS = 0x00100000;
            public static readonly int OFN_NOLONGNAMES = 0x00040000;
            public static readonly int OFN_NONETWORKBUTTON = 0x00020000;
            public static readonly int OFN_NOREADONLYRETURN = 0x00008000;
            public static readonly int OFN_NOTESTFILECREATE = 0x00010000;
            public static readonly int OFN_NOVALIDATE = 0x00000100;
            public static readonly int OFN_OVERWRITEPROMPT = 0x00000002;
            public static readonly int OFN_PATHMUSTEXIST = 0x00000800;
            public static readonly int OFN_READONLY = 0x00000001;
            public static readonly int OFN_SHAREAWARE = 0x00004000;
            public static readonly int OFN_SHOWHELP = 0x00000010;

            public OpenFileName()
            {
                this.structSize = Marshal.SizeOf(this);
                //this.filter = "All Files\0*.*\0\0";
                this.file = new string('\0', 4096);
                this.maxFile = this.file.Length;
                this.fileTitle = new string('\0', 256);
                this.maxFileTitle = this.fileTitle.Length;
                this.title = "Open";
                this.flags = OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR;
            }
        }

        [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        public static extern bool GetOpenFileName([In, Out] OpenFileName lpofn);

        #endregion
    }
}