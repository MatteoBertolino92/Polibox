using Client;
using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.IO;

namespace Client_v2
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.FolderBrowserDialog dlg = new FolderBrowserDialog();
        private DialogResult resultDialog;
        private delegate void nomedelegate();

        public MainWindow()
        {
            GlobalClass.firstActive = true;
            GlobalClass.mainActive = false;
            InitializeComponent();
            FolderTextBox.IsReadOnly = true;
            GlobalClass.main = this;
        }

        private void RegisterTab_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Color colorOrange = Color.FromRgb(255, 124, 17);
            Color colorWhite = Color.FromRgb(255, 255, 255);
            SolidColorBrush orange = new SolidColorBrush(colorOrange);
            SolidColorBrush white = new SolidColorBrush(colorWhite);
            RegisterTab.Background = white;
            RegisterTab.Foreground = orange;
            LoginTab.Background = orange;
            LoginTab.Foreground = white;
        }

        private void LoginTab_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Color colorOrange = Color.FromRgb(255, 124, 17);
            Color colorWhite = Color.FromRgb(255, 255, 255);
            SolidColorBrush orange = new SolidColorBrush(colorOrange);
            SolidColorBrush white = new SolidColorBrush(colorWhite);
            RegisterTab.Background = orange;
            RegisterTab.Foreground = white;
            LoginTab.Background = white;
            LoginTab.Foreground = orange;
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            resultDialog = dlg.ShowDialog();
            if (resultDialog.ToString() == "OK")
            {
                FolderTextBox.Clear();
                FolderTextBox.AppendText(dlg.SelectedPath);
                FolderTextBox.ScrollToEnd();
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                lock (GlobalClass.lockableObj)
                {
                    Regex r = new Regex(GlobalClass.pattern, RegexOptions.IgnoreCase);
                    Match m = r.Match(IPTextBoxL.Text);
                    if (m.Success)
                        GlobalClass.IP = IPTextBoxL.Text;
                    else
                    {
                        throw new Exception ("Insert a valid IP");
                    }

                    if (GlobalClass.k == null)
                        connectFunction(); 
                    
                    if (UsernameTextBox.Text == "" || PasswordTextBox.Password == "")
                        throw new Exception("Fill up all fields!");

                    SecCom.name = UsernameTextBox.Text;
                    SecCom.pswClear = PasswordTextBox.Password;

                    while (GlobalClass.k == null) ;
                    SecCom.listener.sendCommand("LOG");
                    string ack = SecCom.listener.receiveCommand();
                    if (ack != "ACK")
                        throw new Exception("Login Failed");
                    
                    SecCom.listener.sendCommand(GetLocalIPv4(NetworkInterfaceType.Wireless80211));
                    ack = SecCom.listener.receiveCommand();
                    if (ack != "ACK")
                        throw new Exception("Login Failed");

                    SecCom.listener.sendCommand(SecCom.name);
                    String tmpsal = SecCom.listener.receiveCommand();
                    if (tmpsal == "NUS")
                        throw new Exception("Name not existing");

                    SecCom.sale = tmpsal;
                    SecCom.pswSaleMd5 = SecCom.GetMd5Hash(SecCom.md5HashVar, (SecCom.pswClear + SecCom.sale));

                    SecCom.listener.sendCommand("CHL");
                    string challenge = SecCom.listener.receiveCommand();

                    SecCom.token = SecCom.GetMd5Hash(SecCom.md5HashVar, (SecCom.pswSaleMd5 + challenge));
                    SecCom.listener.sendCommand(SecCom.token);
                    ack = SecCom.listener.receiveCommand();
                    if (ack == "NPW")
                        throw new Exception("Password not correct");
                    else if (ack != "ACK")
                        throw new Exception("Login Failed");

                    SecCom.listener.sendCommand("PTH");
                    GlobalClass.path = SecCom.listener.receiveCommand();

                    if (!Directory.Exists(GlobalClass.path)) {
                        Directory.CreateDirectory(GlobalClass.path);
                    }
                }
                new PoliBox().Show();
                GlobalClass.main.Close();
            }

            catch (Exception ex)
            {
                ResultL.Text = ex.Message;
                ResultL.Foreground = Brushes.Red;
            }
        } //login

        public string GetLocalIPv4(NetworkInterfaceType _type)
        {
            string output = "";
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.NetworkInterfaceType == _type && item.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            output = ip.Address.ToString();
                        }
                    }
                }
            }
            return output;
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                lock (GlobalClass.lockableObj)
                {
                    Regex r = new Regex(GlobalClass.pattern, RegexOptions.IgnoreCase);
                    Match m = r.Match(IPTextBox.Text);
                    if (m.Success)
                        GlobalClass.IP = IPTextBox.Text;
                    else
                    {
                        throw new Exception("Insert a valid IP");
                    }

                    if (GlobalClass.k == null)
                        connectFunction();

                    //Take value from form 
                    SecCom.name = UsernameTextBox_Copy.Text;
                    SecCom.pswClear = passwordBox_Copy.Password;

                    if (SecCom.pswClear == "" || SecCom.name == "" || resultDialog.ToString() != "OK")
                        throw new Exception("You have to fill all fields!");

                    SecCom.listener.sendCommand("REG");
                    string ack = SecCom.listener.receiveCommand();
                    if (ack != "ACK")
                        throw new Exception("Registration Failed");

                    SecCom.listener.sendCommand(SecCom.name);
                    ack = SecCom.listener.receiveCommand();
                    if (ack == "NCK")
                    {
                        throw new Exception("Username already existing");
                    }
                    else if (ack != "ACK")
                        throw new Exception("Registration Failed");

                    SecCom.listener.sendCommand(SecCom.pswClear);
                    SecCom.sale = SecCom.listener.receiveCommand();
                    if (SecCom.sale == null)
                        throw new Exception("Registration Failed");
                    SecCom.pswSaleMd5 = SecCom.GetMd5Hash(SecCom.md5HashVar, (SecCom.pswClear + SecCom.sale));

                    GlobalClass.path = dlg.SelectedPath;
                    SecCom.listener.sendCommand(dlg.SelectedPath);
                    ack = SecCom.listener.receiveCommand();
                    if (ack != "ACK")
                        throw new Exception("Registration Failed");
                    Result.Foreground = Brushes.Green;
                    Result.Text = "Registration Success";
                }
            }
            catch (Exception ee)
            {
                //just to skip the code
                Result.Text=ee.Message;
                Result.Foreground = Brushes.Red;
            }
        } //Reg function

        private void connectFunction()
        {
            try
            {
                if (GlobalClass.k == null || (!SSLStream.IsSocketConnected(GlobalClass.k)))
                {
                    SecCom.listener = new SSLStream(GlobalClass.IP);
                    SecCom.listener.connectToTCPServer();

                    if (GlobalClass.k == null || (!SSLStream.IsSocketConnected(GlobalClass.k)))
                    {
                        Dispatcher d = this.Dispatcher;
                        ThreadPool.QueueUserWorkItem(new WaitCallback(checkLed), d);
                    }
                }
            }
            catch (Exception)
            {
                if (GlobalClass.k != null)
                {
                    GlobalClass.k.Close();
                    GlobalClass.k = null;
                }
                throw;
            }
        } //connect

        private void checkLed(Object o)
        {
            var d1 = (Dispatcher)o;
            GlobalClass.k = new SSLStream(GlobalClass.IP);
            GlobalClass.k.connectToTCPServer();

            while (true)
            {
                if (GlobalClass.firstActive == true)
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
                System.Threading.Thread.Sleep(5000);
            }
        }

    } //Main window
} //namespace
