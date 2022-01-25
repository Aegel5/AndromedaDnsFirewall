using AndromedaDnsFirewall.main;
using AndromedaDnsFirewall.Utils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace AndromedaDnsFirewall
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            foreach (string arg in Environment.GetCommandLineArgs()) {
                if (arg == "/swap_autostart") {
                    try {
                        AutostartCheck.Swap();
                    } catch (Exception ex) {
                        Log.Err(ex);
                    }
                    Shutdown();
                    return;
                }
            }

            Application.Current.DispatcherUnhandledException += (object sender, DispatcherUnhandledExceptionEventArgs e) => {
                var msg = $"TOPLEVEL Exception occured:\n{e.Exception}";
                MessageBox.Show(msg, e.Exception.Message, MessageBoxButton.OK, MessageBoxImage.Information);
                Log.Err(e.Exception);
                e.Handled = true;
                Shutdown();
            };



            // set priority for our main worker
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;

            Quickst.Load();
            Config.Load();

            MainWindow mainWindow = new MainWindow();
            mainWindow.holder.Init();
            mainWindow.Show();
        }
    }
}
