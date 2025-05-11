using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace AndromedaDnsFirewall;

public partial class TabLog : UserControl
{
    public TabLog()
    {
        InitializeComponent();

		MainHolder.Create();

		cmd_block.Click += (a, b) => {
			var cur = (LogItem)ge_logs.SelectedItem;
			if (cur == null) return;
			UserLists.Block(cur.elem.data);
			UserBlockTab.Inst.Update();

		};
		cmd_allow.Click += (a, b) => {
			var cur = (LogItem)ge_logs.SelectedItem;
			if (cur == null) return;
			UserLists.Allow(cur.elem.data);
			UserBlockTab.Inst.Update();
		};

		cmd_clear.Click += (a, b) => {
			MainHolder.Inst.logLst.Clear();
		};


		ge_logs.ItemsSource = MainHolder.Inst.logLst;
    }
}
