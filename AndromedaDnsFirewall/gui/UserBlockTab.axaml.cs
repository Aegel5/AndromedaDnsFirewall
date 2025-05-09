using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AndromedaDnsFirewall;

public partial class UserBlockTab : UserControl
{
	public static UserBlockTab Inst;
    public UserBlockTab()
    {
		Inst = this;
        InitializeComponent();

		cmd_delete.Click += (a, b) => {
			if (ge_logs.SelectedItem == null) return;
			var cur = (KeyValuePair<string, RuleBlockType>)ge_logs.SelectedItem;
			UserLists.Delete(cur.Key);
			Update();
		};
	}

	public void Update() {
		ge_logs.ItemsSource = null;
		ge_logs.ItemsSource = UserLists.list;
	}
}
