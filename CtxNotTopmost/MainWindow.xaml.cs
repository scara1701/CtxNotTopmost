using CtxNotTopmost.Services;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CtxNotTopmost
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        CancellationTokenSource tokenSource;
        WindowService windowService;
        public MainWindow()
        {
            InitializeComponent();
        }

        //private async Task DetectTopMost()
        //{
        //    WindowService windowService = new WindowService();
        //    windowService.TopMostDetected += WindowService_TopMostDetected;
        //    //await windowService.DetectTopMost("CDViewer", Properties.Settings.Default.WindowToDisplay.Cast<string>().ToArray());
        //}

        private void WindowService_TopMostDetected(object? sender, string e)
        {
            Debug.WriteLine(DateTime.Now.ToShortTimeString() + " " + e);
        }

        private void Window_Activated(object sender, EventArgs e)
        {

        }

        public async Task DetectTopMost()
        {
            tokenSource = new CancellationTokenSource();
            windowService = new WindowService();
            windowService.TopMostDetected += WindowService_TopMostDetected;
            Task.Run(() => DetectingApps(tokenSource.Token));
        }

        private async Task DetectingApps(CancellationToken token)
        {
            do
            {
                await windowService.DetectTopMost("CDViewer", Properties.Settings.Default.WindowToDisplay.Cast<string>().ToArray());
            } while (!token.IsCancellationRequested);

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            tokenSource.Cancel();
        }

        protected override async void OnClosing(CancelEventArgs e)
        {
            tokenSource.Cancel();
            await windowService.Halt(Properties.Settings.Default.WindowToDisplay.Cast<string>().ToArray());
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DetectTopMost();
        }
    }
}
