using AndromedaDnsFirewall.main;
using AndromedaDnsFirewall.Utils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
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
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Application.Current.DispatcherUnhandledException += (object sender, DispatcherUnhandledExceptionEventArgs e) => {
                var msg = $"TOPLEVEL Exception occured:\n{e.Exception}";
                MessageBox.Show(msg, e.Exception.Message, MessageBoxButton.OK, MessageBoxImage.Information);
                Log.Err(e.Exception);
                e.Handled = true;
            };

            Quickst.Load();
            Config.Load();

            MainWindow mainWindow = new MainWindow();
            mainWindow.holder.Init();
            mainWindow.Show();

        }
    }
}
