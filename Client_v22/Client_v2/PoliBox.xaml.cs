using Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Client_v2
{
    /// <summary>
    /// Logica di interazione per PoliBox.xaml
    /// </summary>
    public partial class PoliBox : Window
    {
        private WpfTreeViewBinding.MainWindow f1;
        private ShowContent f2;

        public PoliBox()
        {
            try
            {
                GlobalClass.firstActive = false;
                GlobalClass.mainActive = true;
                GlobalClass.regular = false;
                InitializeComponent();
                WelcomeTextBlock.Text = WelcomeTextBlock.Text + " " + SecCom.name;
                Dispatcher d = this.Dispatcher;
                ThreadPool.QueueUserWorkItem(new WaitCallback(checkLed), d);
                
                //Prima di ogni altra cosa, sinronizzazione iniziale !
                Thread t = new Thread(new ParameterizedThreadStart(TimerC.DirSearchWrapper));
                t.Start(GlobalClass.path); //Se partisse un evento del watcher prima.. amen !

                GlobalClass.fw = new Watcher(GlobalClass.path);
                GlobalClass.fw.Observe();
                GlobalClass.timer = new TimerC();
                GlobalClass.timer.enable();
            }
            catch (Exception)
            {
                // hust to skip the code
               // System.Windows.MessageBox.Show(e.ToString());
            }
        }

        private void DisconnectButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Color colorOrange = Color.FromRgb(255, 124, 17);
            Color colorWhite = Color.FromRgb(255, 255, 255);
            SolidColorBrush orange = new SolidColorBrush(colorOrange);
            SolidColorBrush white = new SolidColorBrush(colorWhite);
            DisconnectButton.Background = orange;
            DisconnectButton.Foreground = white;
        }

        private void DisconnectButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Color colorWhite = Color.FromRgb(255, 255, 255);
            Color colorOrange = Color.FromRgb(255, 124, 17);
            SolidColorBrush orange = new SolidColorBrush(colorOrange);
            SolidColorBrush white = new SolidColorBrush(colorWhite);
            DisconnectButton.Background = white;
            DisconnectButton.Foreground = orange;
        }

        private void RecoveryButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Color colorOrange = Color.FromRgb(255, 124, 17);
            Color colorWhite = Color.FromRgb(255, 255, 255);
            SolidColorBrush orange = new SolidColorBrush(colorOrange);
            SolidColorBrush white = new SolidColorBrush(colorWhite);
            DisconnectButton.Background = orange;
            DisconnectButton.Foreground = white;
        }

        private void RecoveryButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Color colorWhite = Color.FromRgb(255, 255, 255);
            Color colorOrange = Color.FromRgb(255, 124, 17);
            SolidColorBrush orange = new SolidColorBrush(colorOrange);
            SolidColorBrush white = new SolidColorBrush(colorWhite);
            DisconnectButton.Background = white;
            DisconnectButton.Foreground = orange;
        }

        private void ShowContentButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Color colorOrange = Color.FromRgb(255, 124, 17);
            Color colorWhite = Color.FromRgb(255, 255, 255);
            SolidColorBrush orange = new SolidColorBrush(colorOrange);
            SolidColorBrush white = new SolidColorBrush(colorWhite);
            DisconnectButton.Background = orange;
            DisconnectButton.Foreground = white;
        }

        private void ShowContentButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Color colorWhite = Color.FromRgb(255, 255, 255);
            Color colorOrange = Color.FromRgb(255, 124, 17);
            SolidColorBrush orange = new SolidColorBrush(colorOrange);
            SolidColorBrush white = new SolidColorBrush(colorWhite);
            DisconnectButton.Background = white;
            DisconnectButton.Foreground = orange;
        }

        private void SyncronizeButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Color colorOrange = Color.FromRgb(255, 124, 17);
            Color colorWhite = Color.FromRgb(255, 255, 255);
            SolidColorBrush orange = new SolidColorBrush(colorOrange);
            SolidColorBrush white = new SolidColorBrush(colorWhite);
            DisconnectButton.Background = orange;
            DisconnectButton.Foreground = white;
        }

        private void SyncronizeButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Color colorWhite = Color.FromRgb(255, 255, 255);
            Color colorOrange = Color.FromRgb(255, 124, 17);
            SolidColorBrush orange = new SolidColorBrush(colorOrange);
            SolidColorBrush white = new SolidColorBrush(colorWhite);
            DisconnectButton.Background = white;
            DisconnectButton.Foreground = orange;
        }

        private void SyncroButton_Click(object sender, RoutedEventArgs e)
        {
            Thread t = new Thread(new ParameterizedThreadStart(TimerC.DirSearchWrapper));
            t.Start(GlobalClass.path);
        }

        private void RecoveryButton_Click(object sender, RoutedEventArgs e)
        {
            if (GlobalClass.onGoing == true)
            {
                System.Windows.MessageBox.Show("Synchronization or big file transfer ongoing. Please try again later");
                return;
            }
            lock (GlobalClass.lockableObj)
            {
                string response = null;
                List<string> lpath = new List<string>();

                SecCom.listener.sendCommand("LIS");
                while ((response = (SecCom.listener.receiveCommand())) != "FIN")
                {
                    lpath.Add(response);
                    SecCom.listener.sendCommand("ACK");
                    string tmpchk = SecCom.listener.receiveCommand();
                    GlobalClass.dictRecover[response] = tmpchk;
                    SecCom.listener.sendCommand("ACK");
                }
                
                f2 = new ShowContent(lpath);
                f2.Show();
            }

        }
    
        private void ShowContentButton_Click(object sender, RoutedEventArgs e)
        {
            f1 = new WpfTreeViewBinding.MainWindow();
            f1.Show();
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher d1 = this.Dispatcher;
            GlobalClass.regular = true;
            ThreadPool.QueueUserWorkItem(new WaitCallback(endFunction), d1);
        }

        private void endFunction(object o)
        {
            var d1 = (Dispatcher)o;
            lock (GlobalClass.lockableObj)
            {
                try
                {
                    if (SSLStream.IsSocketConnected(GlobalClass.k))
                    {
                        SecCom.listener.sendCommand("ECM");
                        string tmp = SecCom.listener.receiveCommand();
                        if (tmp != "ACK")
                            throw new Exception();

                        SecCom.listener.Close();
                        SecCom.listener = null;
                        lock (GlobalClass.locktry)
                        {
                            GlobalClass.k.sendCommand("ECM");
                            GlobalClass.k.Close();
                            GlobalClass.k = null;
                        }
                    }
                    else
                    {
                        GlobalClass.k = null;
                        SecCom.listener.Close();
                        SecCom.listener = null;
                    }

                    if (GlobalClass.timer != null)
                        GlobalClass.timer.disable();

                    d1.BeginInvoke(new GlobalClass.nomedelegate(() => { new MainWindow().Show(); }));
                    d1.BeginInvoke(new GlobalClass.nomedelegate(() => { this.Close(); }));                    
                }
                catch (Exception)
                {

                    if (SecCom.listener != null)
                        SecCom.listener.Close();

                    if (GlobalClass.k != null)
                        GlobalClass.k.Close();

                    SecCom.listener = null;
                    GlobalClass.k = null;
                    if (GlobalClass.timer != null)
                        GlobalClass.timer.disable();

                    d1.BeginInvoke(new GlobalClass.nomedelegate(() => { new MainWindow().Show(); }));
                    d1.BeginInvoke(new GlobalClass.nomedelegate(() => { this.Close(); }));

                }
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
                        d1.BeginInvoke(new GlobalClass.nomedelegate(() => { ell1.Fill = new SolidColorBrush(Colors.Green); }));
                        d1.BeginInvoke(new GlobalClass.nomedelegate(() => { Status1.Text = "Connected"; }));
                    }
                    else
                    {
                        d1.BeginInvoke(new GlobalClass.nomedelegate(() => { ell1.Fill = new SolidColorBrush(Colors.Red); }));
                        d1.BeginInvoke(new GlobalClass.nomedelegate(() => { Status1.Text = "Not Connected"; }));
                    }
                }
                System.Threading.Thread.Sleep(1000);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (GlobalClass.regular == false && GlobalClass.onGoing==false)
            {
                Dispatcher d1 = this.Dispatcher;
                ThreadPool.QueueUserWorkItem(new WaitCallback(endFunction), d1);
                return;
            }
            if (this.f1 != null && this.f1.IsEnabled)
            {
                this.f1.Close();
            }
            if (this.f2 != null && this.f2.IsEnabled)
            {
                   this.f2.Close();
            }
        
            if (GlobalClass.watcher!=null) {
                GlobalClass.watcher.EnableRaisingEvents = false;
                GlobalClass.watcher = null;
            }
            if (GlobalClass.timer!=null)
                GlobalClass.timer.disable();

            if (SecCom.listener != null)
            {
                SecCom.listener.Close();
                SecCom.listener = null;
            }
            lock(GlobalClass.locktry)
            {
                if (GlobalClass.k != null)
                {
                    GlobalClass.k.sendCommand("ECM");
                    GlobalClass.k.Close();
                    GlobalClass.k = null;
                }
            }

            }
        }
}
