using Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Client_v2
{
    /// <summary>
    /// Interaction logic for ShowList.xaml
    /// </summary>
    public partial class ShowContent : Window
    {
        internal List<string> lpath;
        private bool allWell=false;

        public ShowContent()
        {
            InitializeComponent();
            Dispatcher d = this.Dispatcher;
            ThreadPool.QueueUserWorkItem(new WaitCallback(checkLed), d);
        }

        public ShowContent(List<string> lpath)
        {
            InitializeComponent();
            this.lpath = lpath; 
            listBox.ItemsSource = lpath;
            Dispatcher d = this.Dispatcher;
            ThreadPool.QueueUserWorkItem(new WaitCallback(checkLed), d);
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            int i = 0;
            List<string> lrecover = new List<string>();

            if (listBox.SelectedItems.Count <= 0)
            {
                System.Windows.MessageBox.Show("Nothing selected");
                this.Close();
            }
            else
            {
                while (i < listBox.SelectedItems.Count)
                {
                    string path = listBox.SelectedItems[i].ToString();
                    System.Windows.MessageBox.Show("Recovering: " + path);

                    //Ricava dir if not exist.
                    string dir = handleDir(path);

                    lrecover.Add(path);
                    i++;
                }
                Thread t = new Thread(new ParameterizedThreadStart(askFiles));
                t.Start(lrecover);
                t.Join();
                this.Close();
            }
        }

        private string handleDir(string path)
        {
            string directory = null;

            int i = 0;
            i = path.LastIndexOf('\\');
            directory = path.Substring(0, i);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            return directory;
        }

        private void askFiles(object o)
        {
            try
            {
                lock (GlobalClass.lockableObj)
                {
                    List<string> l = new List<string>();
                    l = (List<string>)o;
                    //System.Windows.MessageBox.Show("startssssssss");

                    SecCom.listener.sendCommand("REQ");
                    string ack = SecCom.listener.receiveCommand();
                    if (ack != "ACK")
                        throw new Exception();

                    foreach (string all in l)
                    {
                        SecCom.listener.sendCommand("SFL");
                        ack = SecCom.listener.receiveCommand();
                        
                        //Since all, find: path + filename + ts
                            int count = all.Split(' ').Length - 1;
                            string[] tmpx = all.Split(' ');
                            StringBuilder path = new StringBuilder("");
                            StringBuilder times = new StringBuilder("");
                            string pth=null;
                            string ts=null;
                            string chksum = null;
                            int c = 0;

                            foreach (string str in tmpx) {
                                if (c < count - 2)
                                {
                                    path.Append(str + " ");
                                }
                                else if (c == count - 2)
                                {
                                    path.Append(str);
                                    pth = path.ToString();
                                }
                                else if (c == count - 1)
                                {
                                    times.Append(str+" ");
                                }
                                else if (c == count) {
                                    times.Append(str);
                                    ts = times.ToString();
                                }
                                c++;
                            }
                         
                        SecCom.listener.sendCommand(pth);
                        string tmp = SecCom.listener.receiveCommand();
                        if (tmp != "ACK")
                            throw new Exception("ACK Expected");
                        SecCom.listener.sendCommand(ts);

                        SecCom.listener.receiveCommand();
                        chksum = GlobalClass.dictRecover[pth + " " + ts];
                        SecCom.listener.sendCommand(chksum);

                        tmp = SecCom.listener.receiveCommand();
                        if (tmp == "NCK")
                            continue;

                        SecCom.listener.sendCommand("SND");

                        GlobalClass.watcher.EnableRaisingEvents = false;
                        SecCom.listener.receiveFile(pth);
                        GlobalClass.watcher.EnableRaisingEvents = true;
                    }
                    System.Windows.MessageBox.Show("Recover done");
                }
            }
            catch (Exception ex)
            {
                //just skip the code
               // System.Windows.MessageBox.Show(ex.ToString());
            }
            finally
            {
                allWell = true;
                SecCom.listener.sendCommand("FIN");
                GlobalClass.watcher.EnableRaisingEvents = true;
            }
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (allWell == false) {
                GlobalClass.watcher.EnableRaisingEvents = true;
                SecCom.listener.sendCommand("FIN");
            }
        }
    }
}
