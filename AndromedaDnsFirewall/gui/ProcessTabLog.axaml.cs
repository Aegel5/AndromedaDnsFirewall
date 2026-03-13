using AndromedaDnsFirewall.main;
using Avalonia.Controls;


namespace AndromedaDnsFirewall;

public partial class ProcessTabLog : UserControl {
	public ProcessTabLog() {
		InitializeComponent();

		ge_logs.ItemsSource = ProcessListModel.ModelBinding;
	}
}
