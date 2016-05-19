using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading;
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
using System.Windows.Threading;
using System.Reflection;

namespace Server_v2
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TextWriter _writer = null;
        BackgroundWorker bw = new BackgroundWorker();

        public MainWindow()
        {
            InitializeComponent();
            InitializeConsole();
            bw.RunWorkerAsync();
        }

        private void InitializeConsole()
        {
            bw.DoWork += bw_DoWork;
            bw.WorkerReportsProgress = true;
            bw.ProgressChanged += bw_ProgressChanged;
            txtConsole.TextAlignment = TextAlignment.Left;
            // Disabilito le modifiche
            txtConsole.IsReadOnly = true;
            // Instantiate the writer
            _writer = new ConsoleRedirection(bw);
            // Redirect the out Console stream
            Console.SetOut(_writer);
        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            Server.StartServer(bw);
        }

        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int n = e.ProgressPercentage;
            if (n == 1)
            {
                string stringa = e.UserState as string;
                txtConsole.AppendText(stringa);
            }
            if (n == 2)
            {
                string stringa = e.UserState as string;
                UtentiConnessi.Items.Add(stringa);
            }
            if (n == 3)
            {
                string stringa = e.UserState as string;
                UtentiConnessi.Items.Remove(stringa);
            }
            if (n == 4)
            {
                UtentiConnessi.Items.Clear();
            }
        }
        
        private void Console_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtConsole.ScrollToEnd();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Server.DB.printUsers();
        }

        private void PrintFolders_Click(object sender, RoutedEventArgs e)
        {
            Server.DB.printFolders();
        }

        private void PrintFiles_Click(object sender, RoutedEventArgs e)
        {
            Server.DB.printFiles();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                foreach (SSLStream x in SecCom.listSock) x.Close();

                bw.CancelAsync();
            } 
            catch (Exception)
            {

            }
        }

        private void UtentiConnessi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}