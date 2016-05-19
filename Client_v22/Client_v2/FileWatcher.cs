using System;
using System.IO;
using System.Security.Permissions;
using Client;
using System.Threading;
using System.Collections;

namespace Client_v2
{
    public class Watcher
    {
        public string path = null;
        private static Hashtable fileWriteTime = new Hashtable();

        /// <summary>
        /// Creazione del whatcher
        /// </summary>
        /// <param name="path"></param>
        public Watcher(string path)
        {
            this.path = path;
            GlobalClass.watcher = new FileSystemWatcher();
            GlobalClass.watcher.Path = path;
        }

        /// <summary>
        /// Notifica un cambiamento. Possono essere creazione, delete e rename. 
        /// </summary>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void Observe()
        {
            // Create a new FileSystemWatcher and set its properties.
            GlobalClass.watcher.NotifyFilter = NotifyFilters.LastWrite
                | NotifyFilters.FileName | NotifyFilters.DirectoryName;

            // Add event handlers.
            GlobalClass.watcher.Created += new FileSystemEventHandler(OnCreated);
            GlobalClass.watcher.Deleted += new FileSystemEventHandler(OnDeleted);
            GlobalClass.watcher.Renamed += new RenamedEventHandler(OnRenamed);
            GlobalClass.watcher.Changed += new FileSystemEventHandler(OnChanged);
            GlobalClass.watcher.IncludeSubdirectories = true; //guarda anche i cambiamenti nelle sottocartelle

            // Begin watching.
            GlobalClass.watcher.EnableRaisingEvents = true;
        }

        private static void OnChanged(object source, FileSystemEventArgs e) {
            FileAttributes attr = File.GetAttributes(e.FullPath);
            bool isDir = ((attr & FileAttributes.Directory) == FileAttributes.Directory);
            string currentLastWriteTime = File.GetLastWriteTime(e.FullPath).ToString();

            if (!fileWriteTime.ContainsKey(e.FullPath) || fileWriteTime[e.FullPath].ToString() != currentLastWriteTime)
            {
                fileWriteTime[e.FullPath] = currentLastWriteTime;
                objCrt obj = new objCrt(e.FullPath, isDir);
                Thread t = new Thread(new ParameterizedThreadStart(reactOnChanged));
                t.Start(obj);
            }
        }
        
        /// <summary>
        /// Define the event handlers.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private static void OnCreated(object source, FileSystemEventArgs e)
        {
            FileAttributes attr = File.GetAttributes(e.FullPath);
            bool isDir = ((attr & FileAttributes.Directory) == FileAttributes.Directory);
            string currentLastWriteTime = File.GetLastWriteTime(e.FullPath).ToString();

            if (!fileWriteTime.ContainsKey(e.FullPath) || fileWriteTime[e.FullPath].ToString() != currentLastWriteTime) {

                fileWriteTime[e.FullPath] = currentLastWriteTime;
                objCrt obj = new objCrt(e.FullPath, isDir);
                Thread t = new Thread(new ParameterizedThreadStart(reactOnCreate));
                t.Start(obj);
            }    
        }

        private static void reactOnCreate(object o)
        {
            try
            {
                lock (GlobalClass.lockableObj)
                {

                    String path = ((objCrt)o).path;
                    bool isDir = ((objCrt)o).isDir;

                    System.Threading.Thread.Sleep(1000);
                    //System.Windows.MessageBox.Show("Created: " + path + " " + isDir);
                    
                    string tmp;

                    if (isDir == false)
                    {
                        //Se è un file mando il comando FLE
                        SecCom.listener.sendCommand("FLE");

                        //Ricevo un ACK a fronte del comando FILE
                        tmp = SecCom.listener.receiveCommand();
                        if (tmp != "ACK")
                            throw new Exception("Error");

                        //Invio il Path del file
                        SecCom.listener.sendCommand(path);

                        //Ricevo un ACK a fronte del path del file
                        tmp = SecCom.listener.receiveCommand();
                        if (tmp != "ACK")
                            throw new Exception("Command isDir wrong");

                        //Invio Checksum
                        string checksum = GlobalClass.calculateChecksum(path);
                        SecCom.listener.sendCommand(checksum);

                        //Ricevo ACK a fronte del Checksum
                        tmp = SecCom.listener.receiveCommand();
                        if (tmp == "NCK")
                            return;
                        else if (tmp!="NCK" && tmp!="ACK")
                            throw new Exception("ACK expected");
                    
                        //Invio il file
                        SecCom.listener.sendFile(path);

                        //Ricevo ACK a fronte della ricezione del file
                        tmp = SecCom.listener.receiveCommand();
                        if (tmp != "ACK")
                            throw new Exception("Error sending file");

                    }
                    else
                    {
                        //Explore directory recursively, send a list of checksum of all file inside it. 
                        //S could answer: I have or I haven't. If II, send all.

                        //Se è un direttorio mando il comando DIR
                        SecCom.listener.sendCommand("DIR");

                        //Ricevo ACK a fronte del comando DIR
                        tmp = SecCom.listener.receiveCommand();
                        if (tmp != "ACK")
                            throw new Exception("DIR failed");

                        //Invio il path del direttorio creato
                        SecCom.listener.sendCommand(path);

                        //Ricevo ACK a fronte della ricezione corretta del path
                        tmp = SecCom.listener.receiveCommand();
                        if (tmp != "ACK")
                            throw new Exception("Path dir failed");

                        //Esplora ricorsivamente il direttorio e invia i file o i direttori al suo interno
                        DirSearch(path);
                    }
                }
            }
            catch (Exception e)
            {
                //just to skip the code                
            }
        }

        private static void reactOnChanged(object o)
        {
            try
            {
                lock (GlobalClass.lockableObj)
                {

                    String path = ((objCrt)o).path;
                    bool isDir = ((objCrt)o).isDir;
                    //System.Windows.MessageBox.Show("Changed1: " + path + " " + isDir);
                    System.Threading.Thread.Sleep(1000);
                    if (isDir == true)
                        return;
                    string tmp;

                    if (isDir == false)
                    {
                        //Se è un file mando il comando FILE
                        SecCom.listener.sendCommand("FLE");

                        //Ricevo un ACK a fronte del comando FILE
                        tmp = SecCom.listener.receiveCommand();
                        if (tmp != "ACK")
                            throw new Exception("Error");

                        //Invio il Path del file
                        SecCom.listener.sendCommand(path);

                        //Ricevo un ACK a fronte del path del file
                        tmp = SecCom.listener.receiveCommand();
                        if (tmp != "ACK")
                            throw new Exception("Command isDir wrong");
                        

                        //Invio Checksum
                        string checksum = GlobalClass.calculateChecksum(path);
                        SecCom.listener.sendCommand(checksum);

                        //Ricevo ACK a fronte del Checksum
                        tmp = SecCom.listener.receiveCommand();
                        if (tmp == "NCK")
                            return;
                        else if (tmp != "NCK" && tmp != "ACK")
                            throw new Exception("ACK expected");

                        //Invio il file
                        SecCom.listener.sendFile(path);

                        //Ricevo ACK a fronte della ricezione del file
                        tmp = SecCom.listener.receiveCommand();
                        if (tmp != "ACK")
                            throw new Exception("Error sending file");
                    }
                }
            }
            catch (Exception)
            {
                //just to skip the code
              //  System.Windows.MessageBox.Show(e.ToString());
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
                        throw new Exception("CHK expected");

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
                    //Send VFD command
                    SecCom.listener.sendCommand("VFD");
                    tmp = SecCom.listener.receiveCommand();
                    if (tmp != "ACK")
                        throw new Exception("Command VFD expected");

                    //Send PTH
                    SecCom.listener.sendCommand(d);
                    tmp = SecCom.listener.receiveCommand();
                    if (tmp != "ACK")
                        throw new Exception("Path expected");

                    DirSearch(d);
                }
            }
            catch (System.Exception)
            {
                // skip
                //System.Windows.MessageBox.Show(excpt.Message);
            }
        }

        private static void OnDeleted(object source, FileSystemEventArgs e)
        {
            Thread t = new Thread(new ParameterizedThreadStart(reactOnDelete));
            t.Start(e.FullPath);
        }

        private static void reactOnDelete(object p)
        {
            try
            {
                lock (GlobalClass.lockableObj)
                {
                    String path = (String)p;
                    // System.Windows.MessageBox.Show("Deleted: " + path);
                    System.Threading.Thread.Sleep(1000);
                    SecCom.listener.sendCommand("DEL");
                    String tmp = SecCom.listener.receiveCommand();
                    if (tmp != "ACK")
                        throw new Exception("Command DEL wrong");

                    SecCom.listener.sendCommand(path);
                    tmp = SecCom.listener.receiveCommand();
                    if (tmp != "ACK")
                        throw new Exception("Error sending DEL path");
                }
            }
            catch (Exception)
            {
                //just to skip the code
              //  System.Windows.MessageBox.Show(e.ToString());
            }
        }

        private static void OnRenamed(object source, RenamedEventArgs e)
        {
            FileAttributes attr = File.GetAttributes(e.FullPath);
            bool isDir = ((attr & FileAttributes.Directory) == FileAttributes.Directory);
            objRnm obj = new objRnm(e.OldFullPath, e.FullPath, isDir);
            Thread t = new Thread(new ParameterizedThreadStart(reactOnRename));
            t.Start(obj);
        }

        public static void reactOnRename(object o)
        {
            try
            {
                lock (GlobalClass.lockableObj)
                {
                    String oldPath = ((objRnm)o).oldPath;
                    String newPath = ((objRnm)o).newPath;
                    bool isDir = ((objRnm)o).isDir;
                    //System.Windows.MessageBox.Show("Renamed: " + oldPath + " to: " + newPath + " " + isDir.ToString());
                    System.Threading.Thread.Sleep(1000);

                    SecCom.listener.sendCommand("RNM");
                    String tmp = SecCom.listener.receiveCommand();
                    if (tmp != "ACK")
                        throw new Exception("Command RNM wrong");

                    SecCom.listener.sendCommand(oldPath);
                    tmp = SecCom.listener.receiveCommand();
                    if (tmp != "ACK")
                        throw new Exception("OldPath wrong");

                    SecCom.listener.sendCommand(newPath);
                    tmp = SecCom.listener.receiveCommand();
                    if (tmp != "ACK")
                        throw new Exception("NewPath wrong");

                    SecCom.listener.sendCommand(isDir.ToString());

                    tmp = SecCom.listener.receiveCommand();
                    if (tmp != "ACK")
                        throw new Exception("Sending isDir in RNM wrong");
                    }
            }
            catch (Exception)
            {
                //just to skip the code
            }
        }
    }

    public class objCrt
    {
        public String path = null;
        public bool isDir;

        public objCrt(String path, bool isDir)
        {
            this.path = path;
            this.isDir = isDir;
        }
    }

    public class objRnm
    {
        public String oldPath = null;
        public String newPath = null;
        public bool isDir;

        public objRnm(String oldPath, String newPath, bool isDir)
        {
            this.oldPath = oldPath;
            this.newPath = newPath;
            this.isDir = isDir;
        }
    }
}