using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AndromedaDnsFirewall;

public partial class PublicBlockTab : UserControl
{
    public PublicBlockTab()
    {
        InitializeComponent();
		cmd_add.Click += (a, b) => { Config.Inst.PublicBlockLists.Add(new("URL")); };
		cmd_delete.Click += (a, b) => {
			var cur = (PublicBlockEntry)ge_publicBlock.SelectedItem;
			if (cur == null) return;
			Config.Inst.PublicBlockLists.Remove(cur);
			};
		cmd_reload.Click += async (a, b) => {
			var cur = (PublicBlockEntry)ge_publicBlock.SelectedItem;
			if (cur == null) return;
			await cur.LoadFromUrl();
		};
		ge_publicBlock.ItemsSource = Config.Inst.PublicBlockLists;
    }
}
