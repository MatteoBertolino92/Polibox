using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Client
{
    class TimerC
    {
        private System.Timers.Timer aTimer = null;

        /// <summary>
        /// Il timer viene attivato ogni 15 minuti = 15*60*1000 = 900000ms
        /// </summary>
        internal TimerC()
        {
            aTimer = new System.Timers.Timer();
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.Interval = 900000;  
        }

        internal void enable()
        {
            aTimer.Enabled = true;
        }
        internal void disable()
        {
            aTimer.Enabled = false;
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            Thread t = new Thread(reactOnTimer);
            t.Start();
        }

        private static void reactOnTimer()
        {
            if (SSLStream.IsSocketConnected(GlobalClass.k) && GlobalClass.onGoing == false)
            {
                DirSearchWrapper(GlobalClass.path);
            }
        }

        internal static void DirSearchWrapper(object o)
        {
            if (GlobalClass.onGoing == false)
            {
                lock (GlobalClass.lockableObj)
                {
                    try
                    {
                        GlobalClass.onGoing = true;

                        SecCom.listener.sendCommand("VFI");
                        string tmp = SecCom.listener.receiveCommand();
                        if (tmp != "ACK")
                            throw new Exception("Command VFI expected");

                        DirSearch(o);

                        SecCom.listener.sendCommand("VFE");
                        tmp = SecCom.listener.receiveCommand();
                        if (tmp != "ACK")
                            throw new Exception("Command VFE expected");
                    }

                    catch (Exception)
                    {
                        GlobalClass.onGoing = false;
                    }

                    finally
                    {
                        GlobalClass.onGoing = false;
                    }
                } //lock
            } //if
            else {
                System.Windows.MessageBox.Show("Sincronization/Big transfer file already ongoing");
            }
        }

        private static void DirSearch(object o)
        {
            string sDir = (string)o;

            try
            {
                string tmp = null;
                foreach (string f in Directory.GetFiles(sDir))
                {
                    //Send VFILE command
                    SecCom.listener.sendCommand("VFL");
                    tmp = SecCom.listener.receiveCommand();
                    if (tmp != "ACK")
                        throw new Exception("Command VFL expected");

                    //Send PATH
                    SecCom.listener.sendCommand(f);
                    tmp = SecCom.listener.receiveCommand();
                    if (tmp != "ACK")
                        throw new Exception("Path expected");

                    //Send checksum
                    string checksum = GlobalClass.calculateChecksum(f);
                    SecCom.listener.sendCommand("CHK");
                    tmp = SecCom.listener.receiveCommand();
                    if (tmp != "ACK")
                        throw new Exception("Command CHK expected");
                    SecCom.listener.sendCommand(checksum);
                    tmp = SecCom.listener.receiveCommand();
                    if (tmp != "ACK")
                        throw new Exception("CHKS expected");

                    //Receive answer: YES for resend, NO for continue
                    SecCom.listener.sendCommand("ASW");
                    tmp = SecCom.listener.receiveCommand();
                    if (tmp == "YES")
                    {
                        //Send File
                        SecCom.listener.sendFile(f);
                        tmp = SecCom.listener.receiveCommand();
                        if (tmp != "ACK")
                            throw new Exception("Ending of file expected");
                    }

                    else if (tmp == "NOP")
                    {
                        continue;
                    }

                    else
                        throw new Exception("Y/N expected");
                }

                foreach (string d in Directory.GetDirectories(sDir))
                {
                    //Send VFOLD command
                    SecCom.listener.sendCommand("VFD");
                    tmp = SecCom.listener.receiveCommand();
                    if (tmp != "ACK")
                        throw new Exception("Command VFD expected");

                    //Send PATH
                    SecCom.listener.sendCommand(d);
                    tmp = SecCom.listener.receiveCommand();
                    if (tmp != "ACK")
                        throw new Exception("Path expected");

                    DirSearch(d);
                }
            }
            catch (System.Exception)
            {
                //just to skip the code
              //  System.Windows.MessageBox.Show(excpt.Message);
            }
        }
    }
}
