using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace SwiftSeek.App;

public static class Program
{
    [DllImport("Microsoft.ui.xaml.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern void XamlCheckProcessRequirements();

    [DllImport("Microsoft.WindowsAppRuntime.Bootstrap.dll", EntryPoint = "MddBootstrapInitialize", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int MddBootstrapInitialize(uint majorMinorVersion, string? versionTag, uint minVersionRevision);

    [DllImport("Microsoft.WindowsAppRuntime.Bootstrap.dll", EntryPoint = "MddBootstrapShutdown", ExactSpelling = true)]
    private static extern void MddBootstrapShutdown();

    [STAThread]
    public static void Main(string[] args)
    {
        var logPath = Path.Combine(Path.GetTempPath(), "SwiftSeek.bootstrap.log");
        File.AppendAllText(logPath, $"Starting at {DateTime.Now:O}{Environment.NewLine}");

        const uint windowsAppSdkVersion = 0x00010005;
        const uint minVersionRevision = 0x00000000;
        var hr = MddBootstrapInitialize(windowsAppSdkVersion, null, minVersionRevision);
        File.AppendAllText(logPath, $"MddBootstrapInitialize hr=0x{hr:X8}{Environment.NewLine}");

        WinRT.ComWrappersSupport.InitializeComWrappers();
        File.AppendAllText(logPath, "Starting WinUI app\n");
        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });

        File.AppendAllText(logPath, "WinUI app exited\n");
        MddBootstrapShutdown();
    }
}
