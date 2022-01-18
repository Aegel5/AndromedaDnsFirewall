﻿using AndromedaDnsFirewall.main;
using AndromedaDnsFirewall.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AndromedaDnsFirewall
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            notifyIcon.Visible = true;

            var ctxMene = new System.Windows.Forms.ContextMenuStrip();
            notifyIcon.ContextMenuStrip = ctxMene;

            ctxMene.Items.Add("Show", null, (object sender, EventArgs args) => { ShowWnd(); });
            ctxMene.Items.Add("Exit", null, (object sender, EventArgs args) => { ExitWnd(); });

            notifyIcon.DoubleClick += (object sender, EventArgs args) => { ShowWnd(); };

            foreach (var val in (WorkMode[]) Enum.GetValues(typeof(WorkMode)))
            {
                RadioButton btn = new();
                btn.Content = val;
                modePanel.Children.Add(btn);
                btn.Checked += Btn_Click;
                btn.GroupName = "1";
                btn.Margin = new Thickness(4);

                if(val == Quickst.Inst.mode)
                {
                    btn.IsChecked = true;
                }
            }

            InitAutoStartMenu();

            AutoUpdater();

            UpdateTitle();
        }
        long curUpdateForLog = 0;
        async void AutoUpdater() {
            while (!GlobalData.QuitPending) {
                await Task.Delay(10.sec());

                if (curlst != CurLst.Log)
                    continue;

                if(curUpdateForLog != holder.logChangeId) {
                    FillList2();
                }

                curUpdateForLog = holder.logChangeId;
            }
        }

        async void UpdateTitle() {
            while (!GlobalData.QuitPending) {
                await Task.Delay(30.sec());
                Title = $"AndromedaDnsFirewall ResolveAvrTime={(int)holder.resolver.Avr}, ResolveErrors={holder.resolver.cntErr}";
            }
        }

        void ExitWnd() {
            if (GlobalData.QuitPending) {
                return;
            }

            Log.Info("Start quit.");

            GlobalData.QuitPending = true;

            CloseFormIsExit = true;
            Application.Current.Shutdown();
        }

        void CloseToTray() {
            ShowInTaskbar = false;
            Hide();
        }

        public void ShowWnd() {
            ShowInTaskbar = true;
            Show();
            Activate();
        }

        bool CloseFormIsExit = false;

        private void Btn_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as RadioButton;
            var cur = (WorkMode)btn.Content;
            if (Quickst.Inst.mode != cur)
            {
                Quickst.Inst.mode = cur;
                Quickst.Save(); // int this thread because it quick
            }
        }

        internal MainHolder holder { get; private set; } = new();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Log.Info("Program loaded");
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            curlst = CurLst.Log;
            FillList2();

        }

        void FillList<T>(IEnumerable<T> lst)
        {
            lstLog.Items.Clear();
            foreach (var elem in lst)
            {
                ListBoxItem item = new();
                item.Content = elem.ToString();
                item.DataContext = elem;
                lstLog.Items.Add(item);
            }
        }

        enum CurLst {
            Log,
            Wt,
            Black
        }
        CurLst curlst = CurLst.Log;

        void FillList2() {
            if(curlst == CurLst.Wt) {
                FillList(holder.whiteList);
            }else if(curlst == CurLst.Black) {
                FillList(holder.blackList);
            } else {
                FillList(holder.logLst);
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            curlst = CurLst.Wt;
            FillList2();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            curlst = CurLst.Black;
            FillList2();
        }

        enum AddType {
            toBlack,
            toWhile,
            Delete
        }

        void AddToList(object sender, AddType type)
        {
            var item = (sender as MenuItem)?.DataContext;

            if (item == null)
                return;

            var str = item as string;
            if (str == null)
            {
                var log = item as LogItem;
                if (log != null)
                {
                    str = log.elem.data;
                }
            }
            if (str == null)
                return;

            if(type == AddType.Delete) {
                NameRulesStore.Inst.Delete(str);
                holder.whiteList.Remove(str);
                holder.blackList.Remove(str);
            } else if(type == AddType.toBlack) { 
                NameRulesStore.Inst.Update(str, true);
                holder.blackList.Add(str);
                holder.whiteList.Remove(str);
            }
            else
            {
                NameRulesStore.Inst.Update(str, false);
                holder.blackList.Remove(str);
                holder.whiteList.Add(str);
            }

            if(curlst != CurLst.Log) {
                FillList2();
            }

        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            AddToList(sender, AddType.toBlack);
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            AddToList(sender, AddType.toWhile);
        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e) {
            AddToList(sender, AddType.Delete);
        }

        System.Windows.Forms.NotifyIcon notifyIcon = new();

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            Log.Info("MainForm_FormClosing");
            if (CloseFormIsExit) {
                notifyIcon.Visible = false;
            } else {
                CloseToTray();
                e.Cancel = true;
            }
        }

        void InitAutoStartMenu() {
            var path = AutostartCheck.GetAutostartExe();
            var hasAuto = !string.IsNullOrEmpty(path);
            var name = $"Autostart: {(hasAuto ? path : "EMPTY")}";
            menuAutostart.IsChecked = hasAuto;
            menuAutostart.Header = name;
        }

        private void menuAutostart_Click(object sender, RoutedEventArgs e) {
            var add = menuAutostart.IsChecked;
            if (add) {
                AutostartCheck.SetCurAutostart();
            } else {
                AutostartCheck.RemoveAutostart();
            }
            InitAutoStartMenu();
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e) {
            ExitWnd();
        }
    }
}
