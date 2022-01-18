using AndromedaDnsFirewall.main;
using AndromedaDnsFirewall.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
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
        }

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
            FillList(holder.logLst);

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

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            FillList(holder.storage.whiteList);
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            FillList(holder.storage.blackList);
        }

        void AddToList(object sender, bool black)
        {
            var item = sender as ListBoxItem;
            if (item == null)
            {
                item = lstLog.SelectedItem as ListBoxItem;
                if (item == null)
                    return;
            }

            var data = item.DataContext;
            var str = data as string;
            if (str == null)
            {
                var log = data as LogItem;
                if (log != null)
                {
                    str = log.elem.data;
                }
            }
            if (str == null)
                return;

            if (black)
            {
                holder.storage.blackList.Add(str);
                holder.storage.whiteList.Remove(str);
            }
            else
            {
                holder.storage.blackList.Remove(str);
                holder.storage.whiteList.Add(str);
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            AddToList(sender, true);
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            AddToList(sender, false);
        }
    }
}
