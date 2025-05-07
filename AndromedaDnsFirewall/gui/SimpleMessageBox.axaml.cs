using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall;

public partial class SimpleMessageBox : Window
{
    public SimpleMessageBox()
    {
        if (closedPulse.IsPulsed) throw new System.Exception("box already was used");
        InitializeComponent();
        ge_ok.Click += (a,b) => { ResultOk = true; Close(); };
		ge_cancel.Click += (a,b) => { ResultOk = false; Close(); };
        Closed += (a, b) => {
            closedPulse.Pulse();
        };

    }

    AOneTimePulse closedPulse = new();

    public bool ResultOk { get; private set; } = false;

    public static SimpleMessageBox Create(string msg) {
        var window = new SimpleMessageBox();
        window.ge_text.Text = msg;
        return window;
    }

    public async Task WaitClosed() {
        await closedPulse.Wait();
    }

}
