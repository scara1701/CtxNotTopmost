using CtxNotTopmost.Services;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Diagnostics;
using System;
using System.Windows.Documents;
using CtxNotTopmost.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;

namespace CtxNotTopmost.ViewModels
{
    internal partial class MainViewModel : BaseViewModel
    {

        CancellationTokenSource tokenSource;
        WindowService windowService;

        [ObservableProperty]
        List<ActiveWindow> activeWindows;

        public MainViewModel()
        {
            Title = "CTX NotTopmost";
            DetectTopMost();
        }

        public async Task DetectTopMost()
        {
            tokenSource = new CancellationTokenSource();
            windowService = new WindowService();
            windowService.TopMostDetected += WindowService_TopMostDetected;
            windowService.DetectProcesses();
            DetectingApps(tokenSource.Token);
            DetectingOpenWindows(tokenSource.Token);
        }

        private async Task DetectingOpenWindows(CancellationToken token)
        {
            do
            {
                try
                {

                    var getActiveWindows = await windowService.GetActiveWindows();
                    Debug.WriteLine(getActiveWindows.Count + "active windows detected");
                    if (ActiveWindows == null)
                    {
                        ActiveWindows = new List<ActiveWindow>();
                    }
                    //await Task.Run(() =>
                    //{

                    //Add items which are missing
                    foreach (var item in getActiveWindows)
                    {
                        if (activeWindows.ToList().Where(a => a.wHnd == item.wHnd).FirstOrDefault() == null)
                        {
                            activeWindows.Add(item);
                        }
                    }
                    //Remove items which are no longer present
                    foreach (var item in activeWindows.ToList())
                    {
                        if (getActiveWindows.Where(a => a.wHnd == item.wHnd).FirstOrDefault() == null)
                        {
                            activeWindows.Remove(item);
                        }
                    }
                    //});
                    ActiveWindows = ActiveWindows.OrderBy(w => w.Title).ToList();
                    //OnPropertyChanged(nameof(ActiveWindows));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
                await Task.Delay(10000);
            } while (!token.IsCancellationRequested);
        }

        private async Task DetectingApps(CancellationToken token)
        {
            do
            {
                await windowService.DetectTopMost("CDViewer", Properties.Settings.Default.WindowToDisplay.Cast<string>().ToList());
            } while (!token.IsCancellationRequested);
        }

        private void WindowService_TopMostDetected(object? sender, string e)
        {
            Debug.WriteLine(DateTime.Now.ToShortTimeString() + " " + e);
        }

        private async Task ExitProgram()
        {
            await windowService.Halt(Properties.Settings.Default.WindowToDisplay.Cast<string>().ToArray());
        }


        [RelayCommand]
        public void FocusWindow(ActiveWindow activeWindow)
        {
            windowService.FocusWindow(activeWindow.wHnd);
        }
    }
}
