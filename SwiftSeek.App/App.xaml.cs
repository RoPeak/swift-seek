using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;

namespace SwiftSeek.App
{
    public partial class App : Application
    {
        private Window _window;

        public App()
        {
            LogInfo("App ctor starting");
            UnhandledException += OnUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
            InitializeComponent();
            LogInfo("App ctor completed");
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            try
            {
                LogInfo("OnLaunched starting");
                _window = new MainWindow();
                _window.Activate();
                LogInfo("OnLaunched completed");
            }
            catch (Exception ex)
            {
                LogException("OnLaunched", ex);
                ShowFatalError(ex);
                throw;
            }
        }

        private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            LogException("WinUI", e.Exception);
        }

        private static void OnDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            LogException("AppDomain", e.ExceptionObject as Exception);
        }

        private static void LogException(string source, Exception? exception)
        {
            try
            {
                var logPath = Path.Combine(Path.GetTempPath(), "SwiftSeek.bootstrap.log");
                var message = $"{DateTime.Now:O} {source} unhandled: {exception?.GetType().FullName}: {exception?.Message}{Environment.NewLine}{exception?.StackTrace}{Environment.NewLine}";
                File.AppendAllText(logPath, message);
            }
            catch
            {
                // Swallow logging failures to avoid masking original crashes.
            }
        }

        private static void LogInfo(string message)
        {
            try
            {
                var logPath = Path.Combine(Path.GetTempPath(), "SwiftSeek.bootstrap.log");
                File.AppendAllText(logPath, $"{DateTime.Now:O} INFO {message}{Environment.NewLine}");
            }
            catch
            {
                // Swallow logging failures to avoid masking original crashes.
            }
        }

        private static void ShowFatalError(Exception ex)
        {
            try
            {
                MessageBoxW(IntPtr.Zero, ex.ToString(), "SwiftSeek.App startup error", 0x00000010);
            }
            catch
            {
                // If this fails, rely on log file.
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
    }
}
