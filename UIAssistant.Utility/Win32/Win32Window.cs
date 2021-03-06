﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using System.Windows;
using System.Windows.Automation;
using System.Windows.Media;

using UIAssistant.Interfaces;
using UIAssistant.Interfaces.Native;

namespace UIAssistant.Utility.Win32
{
    public class Win32Window : IWindow
    {
        public override bool Equals(object obj)
        {
            return this.WindowHandle == (obj as Win32Window).WindowHandle;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        [Flags]
        public enum SetWindowPosFlags : uint
        {
            AsynchronousWindowPosition = 0x4000,
            DeferErase = 0x2000,
            DrawFrame = 0x0020,
            FrameChanged = 0x0020,
            HideWindow = 0x0080,
            DoNotActivate = 0x0010,
            DoNotCopyBits = 0x0100,
            IgnoreMove = 0x0002,
            DoNotChangeOwnerZOrder = 0x0200,
            DoNotRedraw = 0x0008,
            DoNotReposition = 0x0200,
            DoNotSendChangingEvent = 0x0400,
            IgnoreResize = 0x0001,
            IgnoreZOrder = 0x0004,
            ShowWindow = 0x0040,
        }

        public static class HWND
        {
            public static IntPtr
            NoTopMost = new IntPtr(-2),
            TopMost = new IntPtr(-1),
            Top = new IntPtr(0),
            Bottom = new IntPtr(1);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "FindWindow", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindowByClass(string lpClassName, IntPtr zero);

        [DllImport("user32.dll", EntryPoint = "FindWindow", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindowByCaption(IntPtr zero, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindowEx(IntPtr hWnd, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", EntryPoint = "FindWindowEx", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindowExByClass(IntPtr hWnd, IntPtr hwndChildAfter, string lpszClass, IntPtr zero);

        [DllImport("user32.dll")]
        private static extern int GetWindowRect(IntPtr hwnd, ref RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetTopWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, WindowShowStyle nCmdShow);

        public IntPtr WindowHandle { get; private set; }

        public Win32Window(IntPtr windowHandle)
        {
            this.WindowHandle = windowHandle;
        }

        [DllImport("user32.dll")]
        //[return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, int lParam);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        public static Win32Window Find(string className, string caption = null)
        {
            var handle = FindWindow(className, caption);
            if (handle == IntPtr.Zero)
            {
                return null;
            }
            return new Win32Window(handle);
        }

        public IWindow FindChild(string className, string caption = null)
        {
            var handle = FindWindowEx(WindowHandle, IntPtr.Zero, className, caption);
            return new Win32Window(handle);
        }

        public IWindow FindChild(IWindow childAfter, string className, string caption = null)
        {
            var handle = FindWindowEx(WindowHandle, childAfter.WindowHandle, className, caption);
            return new Win32Window(handle);
        }

        public void ButtonClick()
        {
            NativeMethods.PostMessage(WindowHandle, NativeMethods.BM_CLICK, IntPtr.Zero, IntPtr.Zero);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public int Length;
            public WindowPlacementFlags flags;
            public int showCmd;
            public POINT ptMinPosition;
            public POINT ptMaxPosition;
            public RECT rcNormalPosition;
        }

        public enum WindowPlacementFlags
        {
            WPF_ASYNCWINDOWPLACEMENT = 0x0004,
            WPF_RESTORETOMAXIMIZED = 0x0002,
            WPF_SETMINPOSITION = 0x0001,
        }

        public const int WS_EX_TOPMOST = 0x00000008;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_LAYERED = 0x80000;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hwnd, StringBuilder lpString, int cch);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("User32.Dll")]
        private static extern IntPtr GetDesktopWindow();

        public bool IsAltTabWindow()
        {
            StringBuilder classBuffer = new StringBuilder(256);
            if (!IsWindowVisible(WindowHandle))
            {
                return true;
            }

            if (!IsAltTabWindow(WindowHandle))
            {
                return true;
            }

            if (string.IsNullOrEmpty(Title))
            {
                return true;
            }

            if ((NativeMethods.GetWindowLongPtr(WindowHandle, GWL.GWL_EXSTYLE).ToInt32() & WS_EX_TOOLWINDOW) != 0)
            {
                return true;
            }

            // For Windows10
            GetClassName(WindowHandle, classBuffer, classBuffer.Capacity);
            string className = classBuffer.ToString();
            if (!string.IsNullOrEmpty(className))
            {
                if (className == "ApplicationFrameWindow")
                {
                    var child = FindWindowExByClass(WindowHandle, IntPtr.Zero, "Windows.UI.Core.CoreWindow", IntPtr.Zero);
                    if (child == IntPtr.Zero)
                    {
                        return true;
                    }
                }
                else if (className == "Windows.UI.Core.CoreWindow")
                {
                    return true;
                }
            }
            return false;
        }

        public string Title => GetTitle();
        private string GetTitle()
        {
            StringBuilder title = new StringBuilder(256);
            GetWindowText(WindowHandle, title, title.Capacity);
            return title?.ToString();
        }

        public static void Filter(Func<IWindow, bool> func)
        {
            EnumWindowsProc filter = (IntPtr windowHandle, IntPtr lParam) =>
            {
                return func(new Win32Window(windowHandle));
            };
            EnumWindows(filter, IntPtr.Zero);
        }

        private ImageSource _Icon;
        public ImageSource Icon
        {
            get
            {
                if (_Icon != null)
                {
                    return _Icon;
                }
                if (FilePath != null)
                {
                    _Icon = Win32Icon.GetIcon(FilePath);
                }
                return _Icon;
            }
        }

        private System.Diagnostics.Process _Process;
        private System.Diagnostics.Process Process
        {
            get
            {
                if (_Process != null)
                {
                    return _Process;
                }
                int processId;
                NativeMethods.GetWindowThreadProcessId(WindowHandle, out processId);
                try
                {
                    _Process = System.Diagnostics.Process.GetProcessById(processId);
                }
                catch
                {

                }
                return _Process;
            }
        }

        public string ProcessName
        {
            get
            {
                return Process?.ProcessName;
            }
        }

        private string _FilePath;
        public string FilePath
        {
            get
            {
                if (_FilePath != null)
                {
                    return _FilePath;
                }
                try
                {
                    _FilePath = Process?.MainModule.FileName;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Print(ex.Message);
                }
                return _FilePath;
            }
        }

        public static Win32Window ActiveWindow
        {
            get
            {
                var target = GetForegroundWindow();
                if (target == IntPtr.Zero)
                {
                    target = GetTopWindow(IntPtr.Zero);
                }
                return new Win32Window(target);
            }
        }

        public IWindow LastActivePopup => new Win32Window(GetLastActivePopup(WindowHandle));

        public void Activate()
        {
            ForciblyControlWindow(() =>
            {
                var windowHandle = GetLastActivePopup(WindowHandle);
                if (IsIconic(windowHandle))
                {
                    var wp = new WINDOWPLACEMENT();
                    wp.Length = Marshal.SizeOf(wp);
                    GetWindowPlacement(windowHandle, ref wp);
                    // Hide しないと Restore 時にアクティブにならないウィンドウがある。(foobar2000 とか)
                    ShowWindow(windowHandle, WindowShowStyle.Hide);
                    ShowWindow(windowHandle, WindowShowStyle.Restore);
                }
                else
                {
                    SetForegroundWindow(windowHandle);
                    BringWindowToTop(windowHandle);
                }
            });
        }

        public void ShowWindow(WindowShowStyle flags)
        {
            ForciblyControlWindow(() => ShowWindow(WindowHandle, flags));
        }

        public bool IsTopMost
        {
            get
            {
                return (NativeMethods.GetWindowLongPtr(WindowHandle, GWL.GWL_EXSTYLE).ToInt32() & WS_EX_TOPMOST) != 0;
            }
        }

        public void ToggleTopMost()
        {
            if (IsTopMost)
            {
                SetNoTopMost();
            }
            else
            {
                SetTopMost();
            }
        }

        public void SetTopMost()
        {
            SetWindowPos(WindowHandle, HWND.TopMost);
        }

        public void SetNoTopMost()
        {
            SetWindowPos(WindowHandle, HWND.NoTopMost);
        }

        public static void SetWindowPos(IntPtr windowHandle, IntPtr insertAfterWindow, bool activate = false, bool hide = false)
        {
            SetWindowPosFlags flag = SetWindowPosFlags.IgnoreMove | SetWindowPosFlags.IgnoreResize;
            if (!activate)
            {
                flag |= SetWindowPosFlags.DoNotActivate;
            }
            if (hide)
            {
                flag |= SetWindowPosFlags.HideWindow;
            }
            else
            {
                flag |= SetWindowPosFlags.ShowWindow;
            }

            ForciblyControlWindow(() => SetWindowPos(windowHandle, insertAfterWindow, 0, 0, 0, 0, flag));
        }

        public bool SetWindowPos(IWindow insertAfterWindow, bool activate, bool hide = false)
        {
            try
            {
                SetWindowPosFlags flag = SetWindowPosFlags.IgnoreMove | SetWindowPosFlags.IgnoreResize;
                if (!activate)
                {
                    flag |= SetWindowPosFlags.DoNotActivate;
                }
                if (hide)
                {
                    flag |= SetWindowPosFlags.HideWindow;
                }

                ForciblyControlWindow(() => SetWindowPos(WindowHandle, insertAfterWindow.WindowHandle, 0, 0, 0, 0, flag));
            }
            catch
            {
                return false;
            }

            return true;
        }

        [DllImport("user32.dll")]
        static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        public const int LWA_ALPHA = 0x2;
        public const int LWA_COLORKEY = 0x1;

        public void SetOpacity(byte value)
        {
            // SetWindowLong(Handle, GWL_EXSTYLE, GetWindowLong(Handle, GWL_EXSTYLE) ^ WS_EX_LAYERED);
            NativeMethods.SetWindowLongPtr(WindowHandle, GWL.GWL_EXSTYLE, new IntPtr(NativeMethods.GetWindowLongPtr(WindowHandle, GWL.GWL_EXSTYLE).ToInt32() ^ WS_EX_LAYERED));
            SetLayeredWindowAttributes(WindowHandle, 0, value, LWA_ALPHA);
        }

        public void Close()
        {
            NativeMethods.PostMessage(WindowHandle, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }

        public Rect Bounds => GetBounds();
        private Rect GetBounds()
        {
            RECT rect = new RECT();
            if (IsIconic(WindowHandle))
            {
                WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
                wp.Length = Marshal.SizeOf(wp);
                GetWindowPlacement(WindowHandle, ref wp);
                if (wp.flags == WindowPlacementFlags.WPF_RESTORETOMAXIMIZED)
                {
                    rect = new RECT(new Screen().Bounds);
                }
                else
                {
                    rect = wp.rcNormalPosition;
                }
            }
            else
            {
                var result = DwmApi.DwmGetWindowAttribute(WindowHandle, DWMWINDOWATTRIBUTE.ExtendedFrameBounds, out rect, Marshal.SizeOf(typeof(RECT)));
                if (NativeMethods.HResultHasError(result))
                {
                    GetWindowRect(WindowHandle, ref rect);
                }
            }
            return rect.ToRectangle();
        }

        public AutomationElement Element => AutomationElement.FromHandle(WindowHandle);

        private static void ForciblyControlWindow(Action action)
        {
            NativeMethods.AttachedThreadInputAction(
                () =>
                {
                    IntPtr lockTimeOut = IntPtr.Zero;
                    NativeMethods.SystemParametersInfo(NativeMethods.SPI_GETFOREGROUNDLOCKTIMEOUT, 0, lockTimeOut, 0);
                    NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETFOREGROUNDLOCKTIMEOUT, 0, IntPtr.Zero, 0);
                    action();
                    NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETFOREGROUNDLOCKTIMEOUT, 0, lockTimeOut, 0);
                });
        }

        public enum GetAncestorFlags
        {
            GetParent = 1,
            GetRoot = 2,
            GetRootOwner = 3
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hwnd, GetAncestorFlags flag);

        [DllImport("user32.dll")]
        private static extern IntPtr GetLastActivePopup(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        // https://blogs.msdn.microsoft.com/oldnewthing/20071008-00/?p=24863
        private static bool IsAltTabWindow(IntPtr hwnd)
        {
            var hwndWalk = GetAncestor(hwnd, GetAncestorFlags.GetRootOwner);

            IntPtr hwndTry;
            while ((hwndTry = GetLastActivePopup(hwndWalk)) != hwndTry)
            {
                if (IsWindowVisible(hwndTry))
                    break;
                hwndWalk = hwndTry;
            }
            return hwndWalk == hwnd;
        }
    }
}
