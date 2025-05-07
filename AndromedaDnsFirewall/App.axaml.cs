using AndromedaDnsFirewall.main;
using System.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace AndromedaDnsFirewall
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
				Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;

				Config.Load();

				Dispatcher.UIThread.UnhandledException += (a, b) => {
					b.Handled = true;
					GuiTools.ShowMessageNoWait(b.Exception.ToString(), "Top Level Exception");
				};

				desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
