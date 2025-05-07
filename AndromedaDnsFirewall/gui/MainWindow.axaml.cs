using AndromedaDnsFirewall.main;
using System.Threading;
using Avalonia.Controls;

namespace AndromedaDnsFirewall
{
    public partial class MainWindow : Window
    {
		MainHolder holder = new();
		public static MainWindow Inst;
		public MainWindow()
        {
			Inst = this;

			InitializeComponent();

			Log.Info("Start program");

			holder.Init();

		}
    }
}
