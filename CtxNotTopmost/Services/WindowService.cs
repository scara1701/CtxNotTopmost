using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CtxNotTopmost.Services
{
    internal class WindowService
    {
        public event EventHandler<string> TopMostDetected;

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int cx, int cy, int wFlags);

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
        public async Task DetectTopMost(string processName, string[] processToFocusNames)
        {
            bool topMostWasRevoked = false;

            List<Process> localByName = Process.GetProcessesByName(processName).ToList();
            foreach (var proc in localByName)
            {
                bool isTopMost = IsWindowTopMost(proc.MainWindowHandle);
                if (isTopMost)
                {
                    //only works if there is a delay?
                    await Task.Delay(10);
                    RemoveTopMost(proc.MainWindowHandle);
                    TopMostDetected?.Invoke(this, $"Topmost detected & disabled for {proc.MainWindowHandle}");
                    topMostWasRevoked |= true;
                }
            }


            if (topMostWasRevoked)
            {
                List<Process> focusByName = new List<Process>();

                foreach (var procToFocusName in processToFocusNames)
                {
                    focusByName.AddRange(Process.GetProcessesByName(procToFocusName).ToList());
                }
                if (focusByName != null)
                {
                    foreach (var proc in focusByName)
                    {

                        bool isTopMost = IsWindowTopMost(proc.MainWindowHandle);
                        if (!isTopMost)
                        {
                            await Task.Delay(10);
                            SetTopMost(proc.MainWindowHandle);
                            TopMostDetected?.Invoke(this, $"Topmost set for {proc.MainWindowHandle}");
                        }
                    }
                }
            }
            await Task.Delay(1000);
        }


        public async Task Halt(string[] processToFocusNames)
        {
            List<Process> focusByName = new List<Process>();

            foreach (var procToFocusName in processToFocusNames)
            {
                focusByName.AddRange(Process.GetProcessesByName(procToFocusName).ToList());
            }
            if (focusByName != null)
            {
                foreach (var proc in focusByName)
                {

                    bool isTopMost = IsWindowTopMost(proc.MainWindowHandle);
                    if (isTopMost)
                    {
                        await Task.Delay(10);
                        RemoveTopMost(proc.MainWindowHandle);
                    }
                }
            }
            await Task.Delay(1000);
        }
    }
}
