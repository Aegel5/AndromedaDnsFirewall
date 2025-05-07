using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace AndromedaDnsFirewall;

public partial class TabLog : UserControl
{

	class LogEntry {
		public string Msg { get; set; } = "";
		public IBrush Background { get; set; } = new SolidColorBrush();
	}

	List<LogEntry> listLogs = new();
    public TabLog()
    {
        InitializeComponent();
		ge_logs.ItemsSource = listLogs;
    }
}
