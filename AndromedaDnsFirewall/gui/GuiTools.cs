using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndromedaDnsFirewall;
internal class GuiTools {
    public static async Task<bool> AskQuestion(string quest) {
        var wnd = SimpleMessageBox.Create(quest);
        wnd.Title = "Требуется подтверждение";
        wnd.Topmost = true;
        ShowCentred(wnd);
        await wnd.WaitClosed();
        return wnd.ResultOk;
    }

    // Показать сообщение и не блокировать текущий стек await.
    public static void ShowMessageNoWait(string msg, string title = "Сообщение") {
        var wnd = SimpleMessageBox.Create(msg);
        wnd.Title = title;
        wnd.Topmost = true;
        ShowCentred(wnd);
    }

    static public void ShowCentred(Window wnd, Window? by = null) {

        // сначала показываем, потом меняем размер, так как show сам может менять размер
        // show - синхронная, поэтому для пользователя видно не будет.
        wnd.Show(); 

        if (by == null) { by = MainWindow.Inst; }

        double x = 0;
        double y = 0;
        double w = 0;
        double h = 0;
        if (by != null) {
            x = by.Position.X;
            y = by.Position.Y;
            w = by.Width;
            h = by.Height;
        } else {
            // get screen size instead
        }
        if (w != 0 && h != 0) {
            var cx = x + w / 2;
            var cy = y + h / 2;
            wnd.Position = new((cx - wnd.Width / 2).Round_to_int(), (cy - wnd.Height / 2).Round_to_int());
        }
    }
}
