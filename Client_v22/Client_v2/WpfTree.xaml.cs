using System.Collections.Generic;
using System.Windows;
using System.IO;
using WpfTreeViewBinding.Model;
using Client;
using System.Threading;
using System.Windows.Threading;
using System;
using System.Windows.Media;

namespace WpfTreeViewBinding.Model
{
    public class Item
    {
        public string Name { get; set; }
        public string Path { get; set; }
    }
}

namespace WpfTreeViewBinding.Model
{
    public class FileItem : Item
    {

    }
}

namespace WpfTreeViewBinding.Model
{
    public class DirectoryItem : Item
    {
        public List<Item> Items { get; set; }

        public DirectoryItem()
        {
            Items = new List<Item>();
        }
    }
}

namespace WpfTreeViewBinding
{
    public class ItemProvider
    {
        public List<Item> GetItems(string path)
        {
            var items = new List<Item>();

            var dirInfo = new DirectoryInfo(path);

            foreach (var directory in dirInfo.GetDirectories())
            {
                var item = new DirectoryItem
                {
                    Name = directory.Name,
                    Path = directory.FullName,
                    Items = GetItems(directory.FullName)
                };

                items.Add(item);
            }

            foreach (var file in dirInfo.GetFiles())
            {
                var item = new FileItem
                {
                    Name = file.Name,
                    Path = file.FullName
                };

                items.Add(item);
            }

            return items;
        }
    }
}

namespace WpfTreeViewBinding
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            var itemProvider = new ItemProvider();
            var items = itemProvider.GetItems(GlobalClass.path);
            DataContext = items;
            Dispatcher d = this.Dispatcher;
            ThreadPool.QueueUserWorkItem(new WaitCallback(checkLed), d);
        }

        private void checkLed(Object o)
        {
            var d1 = (Dispatcher)o;

            if (GlobalClass.k == null)
            {
                GlobalClass.k = new SSLStream(GlobalClass.IP);
                GlobalClass.k.connectToTCPServer();
            }
            while (true)
            {
                if (GlobalClass.mainActive == true)
                {
                    if (SSLStream.IsSocketConnected(GlobalClass.k))
                    {
                        d1.BeginInvoke(new GlobalClass.nomedelegate(() => { ell2.Fill = new SolidColorBrush(Colors.Green); }));
                        d1.BeginInvoke(new GlobalClass.nomedelegate(() => { Status.Text = "Connected"; }));
                    }
                    else
                    {
                        d1.BeginInvoke(new GlobalClass.nomedelegate(() => { ell2.Fill = new SolidColorBrush(Colors.Red); }));
                        d1.BeginInvoke(new GlobalClass.nomedelegate(() => { Status.Text = "Not Connected"; }));
                    }
                }
                System.Threading.Thread.Sleep(1000);
            }
        }

    }
}