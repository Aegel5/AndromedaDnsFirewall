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

		ge_logs.ItemsSource = MainHolder.Inst.logLst;
    }
}
