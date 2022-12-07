using CtxNotTopmost.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
//http://pinvoke.net/default.aspx/user32.EnumDesktopWindows
namespace CtxNotTopmost.Services
{

    internal class OpenWindow
    {
        public Process Process { get; set; }
        public IntPtr Hwnd { get; set; }
        public string WindowTitle { get; set; }
    }

    internal class WindowService
    {
        OpenWindow[] openWindows;
        Process thisApp;

        public event EventHandler<string> TopMostDetected;

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int cx, int cy, int wFlags);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);
        [DllImport("user32.dll")]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);


        [DllImport("user32.dll")]
        static extern bool EnumDesktopWindows(IntPtr hDesktop, EnumDesktopWindowsDelegate lpfn, IntPtr lParam);
        private delegate bool EnumDesktopWindowsDelegate(IntPtr hWnd, int lParam);


        [DllImport("user32.dll", EntryPoint = "GetWindowText", ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpWindowText, int nMaxCount);


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);


        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int GWL_EXSTYLE = -20;
        const int WS_EX_TOPMOST = 0x0008;
        const int HWND_TOPMOST = -1;
        const int HWND_NOTOPMOST = -2;
        const int SWP_NOMOVE = 0x0002;
        const int SWP_NOSIZE = 0x0001;

        public static bool IsWindowTopMost(IntPtr hWnd)
        {
            int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            return (exStyle & WS_EX_TOPMOST) == WS_EX_TOPMOST;
        }

        public void RemoveTopMost(IntPtr hWnd)
        {
            SetWindowPos(hWnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
        }


        public void SetTopMost(IntPtr hWnd)
        {
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
        }
        public async Task DetectTopMost(string processName, List<string> windowsToFocusNames)
        {
            bool topMostWasRevoked = false;

            List<OpenWindow> openwindowByName = openWindows?.Where(p => p.Process.ProcessName.ToLower() == processName.ToLower()).ToList();
            if (openwindowByName == null)
            {
                await Task.Delay(1000);
                return;
            }
            foreach (var proc in openwindowByName)
            {
                bool isTopMost = IsWindowTopMost(proc.Process.MainWindowHandle);
                if (isTopMost)
                {
                    //only works if there is a delay?
                    await Task.Delay(10);
                    RemoveTopMost(proc.Process.MainWindowHandle);
                    TopMostDetected?.Invoke(this, $"Topmost detected & disabled for {proc.Process.MainWindowHandle}");
                    topMostWasRevoked = true;
                }
            }


            if (topMostWasRevoked)
            {
                List<OpenWindow> focusByName = new List<OpenWindow>();

                foreach (var windowToFocusName in windowsToFocusNames)
                {
                    focusByName.AddRange(openWindows?.Where(p => p.Process.ProcessName.ToLower() == windowToFocusName.ToLower()).ToList());
                }
                if (focusByName != null)
                {
                    foreach (var openwindow in focusByName)
                    {

                        //bool isTopMost = IsWindowTopMost(proc.MainWindowHandle);
                        //if (!isTopMost)
                        //{
                        await Task.Delay(10);
                        //if (openwindow.Process.MainWindowHandle != IntPtr.Zero)
                        //{
                        //    //SetTopMost(proc.Process.Handle);
                        //    SetTopMost(openwindow.Hwnd);
                        //    TopMostDetected?.Invoke(this, $"Topmost set for {openwindow.Process.MainWindowHandle}");
                        //}
                        //else
                        //{

                        //SetTopMost(proc.Process.Handle);
                        SetTopMost(openwindow.Hwnd);
                        TopMostDetected?.Invoke(this, $"Topmost set for {openwindow.Hwnd}");
                        //}

                        //}
                    }
                }
            }

            if (thisApp != null)
            {
                //bool appIsTopMost = IsWindowTopMost(thisApp.MainWindowHandle);
                //if (!appIsTopMost)
                //{
                await Task.Delay(10);
                SetTopMost(thisApp.MainWindowHandle);
                TopMostDetected?.Invoke(this, $"Topmost set for {thisApp.MainWindowHandle}");
                //}
            }
            await Task.Delay(1000);
        }


        public async Task Halt(string[] processToFocusNames)
        {
            List<OpenWindow> focusByName = new List<OpenWindow>();

            foreach (var procToFocusName in processToFocusNames)
            {
                focusByName.AddRange(openWindows?.Where(p => p.Process.ProcessName.ToLower() == procToFocusName.ToLower()).ToList());
            }
            if (focusByName != null)
            {
                foreach (var proc in focusByName)
                {

                    bool isTopMost = IsWindowTopMost(proc.Process.MainWindowHandle);
                    if (isTopMost)
                    {
                        await Task.Delay(10);
                        RemoveTopMost(proc.Process.MainWindowHandle);
                    }
                }
            }
            await Task.Delay(1000);
        }

        public async Task<List<ActiveWindow>> GetActiveWindows()
        {
            List<ActiveWindow> activeWindows = new List<ActiveWindow>();
            //await Task.Delay(10000);
            await Task.Run(() =>
            {
                //Process[] processes = Process.GetProcesses().Where(p => p.MainWindowHandle != IntPtr.Zero && p.ProcessName != "CtxNotTopmost").ToArray();
                foreach (var item in openWindows.ToList())
                {
                    if (item.Process.MainWindowTitle.Length > 0)
                    {
                        ActiveWindow activeWindow = new ActiveWindow();
                        activeWindow.Title = item.WindowTitle;
                        activeWindow.wHnd = item.Hwnd;
                        activeWindow.ProcessName = item.Process.ProcessName;
                        if (activeWindow.ProcessName.ToLower() != "cdviewer" && activeWindow.ProcessName.ToLower() != "ctxnottopmost")
                            activeWindows.Add(activeWindow);
                    }
                }
            });
            return activeWindows;
        }

        public async void DetectProcesses()
        {
            await Task.Run(() =>
            {
                thisApp = Process.GetCurrentProcess();
                do
                {
                    //processes = Process.GetProcesses().Where(p => p.MainWindowHandle != IntPtr.Zero).ToArray();
                    //processes = Process.GetProcesses().ToArray();
                    //processes = Process.GetProcesses().Where(p => p.MainWindowHandle != IntPtr.Zero || p.ProcessName.ToLower() == "teams").ToArray();
                    openWindows = GetWindows();
                    Task.Delay(10000).Wait();
                } while (true);
            });
        }


        public void FocusWindow(IntPtr hWnd)
        {
            ShowWindow(hWnd, 0);
            ShowWindow(hWnd, 1);
            //SetForegroundWindow(hWnd);


            //var hwnd = thisApp.Handle;

            //var threadId1 = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
            //var threadId2 = GetWindowThreadProcessId(hwnd, IntPtr.Zero);

            //if (threadId1 != threadId2)
            //{
            //    AttachThreadInput(threadId1, threadId2, true);
            //    SetForegroundWindow(hwnd);
            //    AttachThreadInput(threadId1, threadId2, false);
            //}
            //else
            //    SetForegroundWindow(hwnd);
        }


        private OpenWindow[] GetWindows()
        {

            var collection = new List<OpenWindow>();

            EnumDesktopWindowsDelegate filter = delegate (IntPtr hWnd, int lParam)
            {
                try
                {

                    StringBuilder strbTitle = new StringBuilder(255);
                    int nLength = GetWindowText(hWnd, strbTitle, strbTitle.Capacity + 1);
                    string strTitle = strbTitle.ToString();

                    if (IsWindowVisible(hWnd) && string.IsNullOrEmpty(strTitle) == false)
                    {
                        Debug.WriteLine(strTitle);
                        OpenWindow openWindow = new OpenWindow();
                        openWindow.Hwnd = hWnd;
                        openWindow.WindowTitle = strTitle;
                        collection.Add(openWindow);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                return true;
            };

            //List<Process> getProcesses = new List<Process>();
            if (EnumDesktopWindows(IntPtr.Zero, filter, IntPtr.Zero))
            {
                foreach (var item in collection)
                {
                    //Debug.WriteLine(item);
                    GetWindowThreadProcessId(item.Hwnd, out uint processId);
                    item.Process = Process.GetProcessById((int)processId);
                }
            }
            //return getProcesses.ToArray();
            return collection.ToArray();
        }
    }
}
