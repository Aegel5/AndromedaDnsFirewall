using Avalonia.Controls;


namespace AndromedaDnsFirewall;

public partial class TabLog : UserControl {
	public TabLog() {
		InitializeComponent();

		MainHolder.Create();

		cmd_block.Click += (a, b) => {
			var cur = (LogItem)ge_logs.SelectedItem;
			if (cur == null) return;
			UserLists.Block(cur.host);
			UserBlockTab.Inst.Update();

		};
		cmd_allow.Click += (a, b) => {
			var cur = (LogItem)ge_logs.SelectedItem;
			if (cur == null) return;
			UserLists.Allow(cur.host);
			UserBlockTab.Inst.Update();
		};

		cmd_clear.Click += (a, b) => {
			MainHolder.Inst.logSource.ClearNotify();
		};


		ge_logs.ItemsSource = MainHolder.Inst.logSource;
	}
}
