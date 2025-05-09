using System.Threading;
using Avalonia.Controls;

namespace AndromedaDnsFirewall;

public partial class MainWindow : Window {

	public static MainWindow? Inst;
	public MainWindow() {
		Inst = this;

		InitializeComponent();

		ge_block.Click += (a, b) =>{ 
			Config.Inst.mode = WorkMode.AllExceptBlockList;
			ge_cur_block.Header = ge_block.Header;
		};
		ge_allow.Click += (a, b) => {
			Config.Inst.mode = WorkMode.AllowAll;
			ge_cur_block.Header = ge_allow.Header;
		};

		Log.Info("Start program");

		UserLists.Load();
		PublicBlockList.Init();
		MainHolder.Inst.Init();
		UserBlockTab.Inst.Update();

	}
}
