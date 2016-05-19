using System;
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.ComponentModel;

namespace Server_v2
{
    class Server
    {
        internal static string data = null;
        internal static DataBase DB = new DataBase();
        private static SSLStreamWaiter listener;
        private static BackgroundWorker bw;

        internal static void StartServer(BackgroundWorker backgroundworker)
        {

            bw = backgroundworker;
            string certificate = setServer();
            Start(certificate);

            return;
        }

        internal static string setServer()
        {
            string startupPath = Environment.CurrentDirectory;
            string certificate = startupPath + "\\SignedByCA.cer";

            Directory.CreateDirectory(".\\users\\");

            return certificate;
        }

        /// <summary>
        /// 1) Crea il tunnel SSL
        /// 2) Serve un client dietro l'altro chiamando la ProcessClient() in un nuovo thread. Un thread per ogni Client connesso.
        /// </summary>
        /// <param name="certificate"></param>
        internal static void Start(string certificate)
        {
            byte[] bytes = new Byte[1024];
            listener = new SSLStreamWaiter(certificate);
            SSLStream handler1 = null;

            try
            {
                while (true) //Server infinito, accetta un client dietro l'altro.
                {
                    TcpClient h = listener.waitNewConnection();
                    handler1 = new SSLStream(h);
                    SecCom.listSock.Add(handler1);
                    Thread t = new Thread(new ParameterizedThreadStart(ProcessClient));
                    t.Start(handler1);
                } //while infinito 
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                // The client stream will be closed with the sslStream
                // because we specified this behavior when creating
                // the sslStream.
                if (handler1 != null)
                    handler1.Close();
            }
        }

        /// <summary>
        /// Il thread si mette in attesa del comando e quando lo riceve lo serve.
        /// </summary>
        /// <param name="h">handler della connessione</param>
        private static void ProcessClient(object h)
        {
            string command;
            SSLStream handler = (SSLStream)h;
            UserClass user = null;
            List<FileClass> checklistFiles = new List<FileClass>();
            List<FolderClass> checklistFolders = new List<FolderClass>();
            List<FileClass> allFileAbsent = new List<FileClass>();

            try
            {
                while (true) //Servi un cliente quante volte vuole
                {
                    //Console.WriteLine("Wait a command... ");
                    command = handler.receiveCommand();

                    if (command == "TRY")
                    {
                        //Non si fa nulla. Serve per mantenere viva la connessione.
                    }

                    if (command == "REG")
                    {
                        printCommand(user, command);
                        SignUp(handler);
                    }

                    if (command == "LOG")
                    {
                        printCommand(user, command);
                        user = Login(handler);

                        if (user == null) Console.WriteLine("[127.0.0.1 - SERVER" + " - " + DateTime.Now.ToString("HH:mm:ss") + "] Not logged in. Wrong password or username not exists.");
                        else bw.ReportProgress(2, user.Username);
                    }

                    if (command == "FLE")
                    {
                        if (user == null) Console.WriteLine("[127.0.0.1 - SERVER" + " - " + DateTime.Now.ToString("HH:mm:ss") + "]  Not logged in. Receive not allowed!");
                        else ReceiveFile(handler, user);
                    }

                    if (command == "DIR")
                    {
                        if (user == null) Console.WriteLine("[127.0.0.1 - SERVER" + " - " + DateTime.Now.ToString("HH:mm:ss") + "]  Not logged in. Receive not allowed!");
                        else ReceiveFolder(handler, user);
                    }


                    if (command == "VFI")
                    {
                        printCommand(user, command);

                        if (user == null) Console.WriteLine("[127.0.0.1 - SERVER" + " - " + DateTime.Now.ToString("HH:mm:ss") + "] Not logged in. Verify not allowed!");
                        else
                        {
                            Console.WriteLine("[" + user.Ip + " - " + user.Username + " - " + DateTime.Now.ToString("HH: mm:ss") + "]" + "Start verify");
                            checklistFiles = InitChecklistFiles(handler, user);
                            checklistFolders = InitChecklistFolders(handler, user);
                            //Mando ACK a fronte di VINIT per segnalare che sono pronto per 
                            //la procedura di verifica
                            handler.sendCommand("ACK");
                        }
                    }

                    if (command == "VFL")
                    {
                        if (user == null) Console.WriteLine("[127.0.0.1 - SERVER" + " - " + DateTime.Now.ToString("HH:mm:ss") + "] Not logged in. Verify not allowed!");
                        else checklistFiles = VerifyFile(handler, user, checklistFiles);
                    }

                    if (command == "VFD")
                    {
                        if (user == null) Console.WriteLine("[127.0.0.1 - SERVER" + " - " + DateTime.Now.ToString("HH:mm:ss") + "] Not logged in. Verify not allowed!");
                        else checklistFolders = VerifyFolder(handler, user, checklistFolders);
                    }

                    if (command == "VFE")
                    {
                        printCommand(user, command);

                        if (user == null) Console.WriteLine("[127.0.0.1 - SERVER" + " - " + DateTime.Now.ToString("HH:mm:ss") + "] Not logged in. Verify not allowed!");
                        else
                        {
                            if (checklistFiles.Count != 0 || checklistFolders.Count != 0) realigns(checklistFolders, checklistFiles);
                            Console.WriteLine("[" + user.Ip + " - " + user.Username + " - " + DateTime.Now.ToString("HH: mm:ss") + "]" + "End verify");
                            handler.sendCommand("ACK");
                        }
                    }

                    if (command == "DEL")
                    {
                        printCommand(user, command);

                        if (user == null) Console.WriteLine("[127.0.0.1 - SERVER" + " - " + DateTime.Now.ToString("HH:mm:ss") + "] Not logged in. Delete not allowed!");
                        else Delete(handler, user);
                    }

                    if (command == "RNM")
                    {
                        printCommand(user, command);

                        if (user == null) Console.WriteLine("[127.0.0.1 - SERVER" + " - " + DateTime.Now.ToString("HH:mm:ss") + "] Not logged in. Rename not allowed!");
                        else Rename(handler, user);
                    }

                    if(command == "LIS")
                    {
                        printCommand(user, command);

                        if (user == null) Console.WriteLine("[127.0.0.1 - SERVER" + " - " + DateTime.Now.ToString("HH:mm:ss") + "] Not logged in. Recovery not allowed!");
                        else allFileAbsent = SendListFileAbsent(handler, user);
                    }

                    if (command == "REQ")
                    {
                        printCommand(user, command);

                        if (user == null) Console.WriteLine("[127.0.0.1 - SERVER" + " - " + DateTime.Now.ToString("HH:mm:ss") + "] Not logged in. Recovery not allowed!");
                        else { Recovery(handler, user, allFileAbsent); allFileAbsent.Clear(); }
                    }

                    if (command == "ECM")
                    {
                        if (user != null)
                            fineComm(handler, user);

                        handler.Close();
                        handler = null;

                        return;
                    }

                    if (command == "")
                    {
                        Console.WriteLine("Something goes wrong");
                        throw new Exception();
                    }
                } //while cliente
            }

            catch (System.Net.Sockets.SocketException)
            {
                bw.ReportProgress(4, null);
                foreach (SSLStream x in SecCom.listSock) x.Close();
            }

            catch (System.IO.IOException)
            {
                if (user != null)
                    bw.ReportProgress(3, user.Username);
                handler.Close();
                handler = null;
            }

            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void printCommand(UserClass user, string command)
        {
            if (user == null)
                Console.Write("[127.0.0.1 - SERVER" + " - " + DateTime.Now.ToString("HH:mm:ss") + "] Command received: ");
            else
                Console.Write("[" + user.Ip + " - " + user.Username + " - " + DateTime.Now.ToString("HH:mm:ss") + "] Command received: ");

            Console.WriteLine(command);
        }

        private static void fineComm(object h, UserClass u)
        {
            SSLStream handler = (SSLStream)h;
            handler.sendCommand("ACK");
            bw.ReportProgress(3, u.Username);
        }

        /// <summary>
        /// A fronte di un comando REG viene chiamata questa funzione che fa partire la comunicazione tra client
        /// e server. La password che viene salvata non è la password che richiede l'utente ma una password che
        /// è l'MD5 tra: 
        /// - la password desiderata;
        /// - l'MD5 di una stringa casuale (sale).
        /// Questo per prevenire attacchi statistici.
        /// </summary>
        /// <param name="h">handler della connessione</param>
        private static void SignUp(object h)
        {
            UserClass user = new UserClass();

            SSLStream handler = (SSLStream)h;
            bool added;

            //Invio ACK a fronte di REG
            handler.sendCommand("ACK");

            //Ricevo il nome utente desiderato
            user.Username = handler.receiveCommand();

            //Controllo se il nome utente esiste
            added = DB.Exists(user);

            if (!added)
            {
                //Se non esiste mando un ACK
                handler.sendCommand("ACK");

                //Ricevo la Password desiderata dall'utente
                string psw = handler.receiveCommand();

                //Creo il SALE e lo invio
                user.Sale = SecCom.createSale();
                handler.sendCommand(user.Sale);

                //MD5 previene gli attacchi statistici delle psw//
                user.Password = SecCom.getRealPassword(psw, user.Sale);

                //Ricevo il Path
                user.ClientPath = handler.receiveCommand();

                //Invio ACK a fronte del Path
                handler.sendCommand("ACK");

                //Aggiungo l'utente e il percorso dell'utente al DB
                DB.AddUser(user);

                Console.WriteLine("[127.0.0.1 - SERVER" + " - " + DateTime.Now.ToString("HH:mm:ss") + "] User added: " + user.Username);
            }
            else
            {
                //Se esiste un nome utente uguale invio il comando NACK
                handler.sendCommand("NCK");
                Console.WriteLine("[127.0.0.1 - SERVER" + " - " + DateTime.Now.ToString("HH:mm:ss") + "] User not added: " + user.Username + " exists.");
            }
        }

        /// <summary>
        /// Per il login servono:
        ///  - cartella da monitorare
        ///  - Username
        ///  - Password ripetuta
        /// 
        /// Viene mandato un hash della psw + sale (poichè psw uguali vengono hashati nello stesso modo, 
        /// rompi una rompi tutte.
        /// Se un utente fa tanti login e l'attaccante li intercetta riesce a prendere la psw in tempi brevi 
        /// (attacco statistico).
        /// Quando ci connettiamo mandiamo quindi un hash(hash(psw+sale))+challenge. Chi fa l'ash è MD5. 
        /// La challenge copre gli attacchi 
        /// statistici.
        /// 
        /// </summary>
        /// <param name="h">handler del socket</param>
        private static UserClass Login(object h)
        {
            UserClass user = new UserClass();
            SSLStream handler = (SSLStream)h;
            bool res;

            //ACK a fronte di LOG
            handler.sendCommand("ACK");

            user.Ip = handler.receiveCommand();

            //ACK a fronte dell'IP
            handler.sendCommand("ACK");

            Console.WriteLine("[127.0.0.1 - SERVER" + " - " + DateTime.Now.ToString("HH:mm:ss") + "] Received login request form " + user.Ip);

            //Ricezione Nome Utente
            user.Username = handler.receiveCommand();

            //Controllo se esiste un nome utente uguale
            res = DB.Exists(user);
            if (res)
            {
                //Ottengo il SALE dell'utente dal DB
                user.Sale = DB.getSale(user);

                //il client non si salva il sale e quindi devo inviarlo
                handler.sendCommand(user.Sale);

                //Ricevo comando CHL
                string tmp = handler.receiveCommand();
                if (tmp != "CHL")
                    throw new Exception("Error in challenge request");

                //Faccio partire la challenge
                string challenge = SecCom.RandomString(12);
                handler.sendCommand(challenge);
                string token = handler.receiveCommand();

                string psw_tmp;
                psw_tmp = DB.getPassword(user);
                string psw_real = SecCom.getRealPassword(psw_tmp, challenge);

                if (psw_real == token)
                {
                    //Se la challenge ha avuto buon esito
                    //Setto la password+sale
                    user.Password = psw_real;
                    
                    //Invio ACK
                    handler.sendCommand("ACK");

                    Console.WriteLine("[" + user.Ip + " - " + user.Username + " - " + DateTime.Now.ToString("HH:mm:ss") + "] '" + user.Username + "' logged in.");
                }
                else
                {
                    //Se la challenge non ha avuto un esito positivo
                    //Invio NACKPSW
                    handler.sendCommand("NPW");
                    Console.WriteLine("[" + user.Ip + " - " + user.Username + " - " + DateTime.Now.ToString("HH:mm:ss") + "] wrong password from '" + user.Username + "'!");
                    return null;
                }

                //Rende gestibile la perdita di dati sul client. 
                //Il client chiede ad ogni login dove è la cartella monitorata.

                //Ricezione comando PATH
                tmp = handler.receiveCommand();
                if (tmp != "PTH")
                    throw new Exception("Error in path request");

                user.ClientPath = DB.getClientPath(user);

                //Invio il Path da monitorare
                handler.sendCommand(user.ClientPath);

                return user;
            }
            else
            {
                //Se il nome utente non esiste invio NACKUSR
                handler.sendCommand("NUS");
                Console.WriteLine("[" + user.Ip + " - " + user.Username + " - " + DateTime.Now.ToString("HH:mm:ss") + "] user not exists!");
                return null;
            }
        }

        private static List<FileClass> InitChecklistFiles(SSLStream h, UserClass user)
        {
            return DB.getFilePresent(user);
        }

        private static List<FolderClass> InitChecklistFolders(SSLStream h, UserClass user)
        {
            return DB.getFolderPresent(user);
        }

        private static List<FolderClass> VerifyFolder(SSLStream h, UserClass user, List<FolderClass> checklistFolders)
        {
            SSLStream handler = (SSLStream)h;
            bool res;
            FolderClass folder = new FolderClass();

            //ACK a fronte del VFOLD 
            handler.sendCommand("ACK");

            //Ricevo il path della cartella
            folder.FolderPath = handler.receiveCommand();
            folder.User = user;

            //Provo ad aggiungere la cartella, se aggiunge res = true, altrimenti res = false
            res = DB.AddFolder(folder);

            if (res) Console.WriteLine("[" + user.Ip + " - " + user.Username + " - " + DateTime.Now.ToString("HH:mm:ss") + "] Folder " + folder.FolderPath + " added.");
            else
            {
                checklistFolders.Remove(folder);
                Console.WriteLine("[" + user.Ip + " - " + user.Username + " - " + DateTime.Now.ToString("HH:mm:ss") + "] Folder " + folder.FolderPath + " already present.");
            }
            //ACK a fronte del PATH
            handler.sendCommand("ACK");

            return checklistFolders;
        }

        private static List<FileClass> VerifyFile(SSLStream h, UserClass user, List<FileClass> checklistFiles)
        {
            SSLStream handler = (SSLStream)h;
            string checksum, tmp;
            bool exists, res = false;
            FileClass file = new FileClass(user);

            try
            {
                // ACK a fronte di un VFILE
                handler.sendCommand("ACK");

                //Riceve il path del file
                string path = handler.receiveCommand();

                file.Filename = Path.GetFileName(path);
                file.Folder = DB.getFolder(Path.GetDirectoryName(path) + "\\", user);

                //ACK a fronte del Path
                handler.sendCommand("ACK");

                //Riceve CHKS
                checksum = handler.receiveCommand();
                //ACK a fronte del CHKS
                handler.sendCommand("ACK");
                if (checksum != "CHK")
                    throw new Exception("Si aspetta CHK");

                //Riceve Checksum
                file.Checksum = handler.receiveCommand();
                //ACK a fronte di un Checksum
                handler.sendCommand("ACK");

                //Riceve il comando ANSWER
                tmp = handler.receiveCommand();
                if (tmp != "ASW")
                    throw new Exception("Si aspetta ANSWER");

                //Controllo se esiste un file con lo stesso checksum
                exists = DB.Exists(file);
                if (exists)
                {
                    //se esiste, vedo se esiste nel DB una istanza del file contenuta nella cartella
                    exists = DB.ExistsPresent(file, file.Folder);
                    if (!exists)
                    {
                        //se non esiste l'istanza la creo nel DB
                        DB.AddFile(file);
                        res = true;
                    }

                    //mando NO, lo possiedo, è inutile che me lo invia. 
                    handler.sendCommand("NOP");

                    if (res) Console.WriteLine("[" + user.Ip + " - " + user.Username + " - " + DateTime.Now.ToString("HH:mm:ss") + "] File " + file.Filename + " added.");
                    else
                    {
                        checklistFiles.Remove(file);
                        Console.WriteLine("[" + user.Ip + " - " + user.Username + " - " + DateTime.Now.ToString("HH:mm:ss") + "] File " + file.Filename + " already present.");
                    }
                }
                else
                {
                    //se non esiste
                    handler.sendCommand("YES");
                    Console.WriteLine("[" + user.Ip + " - " + user.Username + " - " + DateTime.Now.ToString("HH:mm:ss") + "] Receive file from " + user.Username + " of " + Sanitizer.desanitize(file.Filename));
                    handler.receiveFile(".\\users\\" + file.User.Username + "\\" + file.Checksum + Path.GetExtension(Sanitizer.desanitize(file.Filename)));

                    string chk = DB.AddFile(file);
                    if (chk != null) File.Delete(".\\users\\" + user.Username + "\\" + chk + Path.GetExtension(Sanitizer.desanitize(path)));

                    handler.sendCommand("ACK");

                    Console.WriteLine("[" + user.Ip + " - " + user.Username + " - " + DateTime.Now.ToString("HH:mm:ss") + "] File " + file.Filename + " added.");
                }

            }
            catch (SystemException)
            {
                //just to skip the code
            }

            return checklistFiles;
        }

        private static void realigns(List<FolderClass> checklistFolders, List<FileClass> checklistFiles)
        {
            DB.realigns(checklistFiles, checklistFolders);
        }

        /// <summary>
        /// Riceve il file
        /// </summary>
        /// <param name="h">handler della connesione</param>
        /// <param name="user">utente che invia il file</param>
        private static void ReceiveFile(object h, UserClass user)
        {
            FileClass file = new FileClass(user);
            SSLStream handler = (SSLStream)h;

            try
            {
                //ACK a fronte di un FLE
                handler.sendCommand("ACK");

                //Riceve il path del file
                string path = handler.receiveCommand();
                file.Filename = Path.GetFileName(path);
                file.Folder = DB.getFolder(Path.GetDirectoryName(path) + "\\", user);

                //ACK a fronte del path
                handler.sendCommand("ACK");

                //Ricezione Checksum
                file.Checksum = handler.receiveCommand();

                //Se ho il file perchè, per qualche strano motivo mi invia lo stesso file due volte evito l'invio e non aggiungo
                if (DB.Exists(file, file.Folder))
                {
                    handler.sendCommand("NCK");
                    DB.setFilePresent(file);
                    return;
                }

                //Se ho un file con lo stesso checksum, ma non ho questa istanza del file nel DB evito l'invio ma lo aggiungo al DB.

                if (DB.Exists(file))
                {
                    handler.sendCommand("NCK");
                    DB.AddFile(file);
                    Console.WriteLine("[" + user.Ip + " - " + user.Username + " - " + DateTime.Now.ToString("HH:mm:ss") + "] File " + file.Filename + " of " + user.Username + " received");
                    return;
                }
                
                //ACK a fronte del Checksum
                handler.sendCommand("ACK");

                //Ricezione file
                Console.WriteLine("[" + user.Ip + " - " + user.Username + " - " + DateTime.Now.ToString("HH:mm:ss") + "] Receive file from " + user.Username + " of " + file.Filename);
                handler.receiveFile(".\\users\\" + file.User.Username + "\\" + file.Checksum + Path.GetExtension(file.Filename));
                
                //Aggiunta file al DB
                string chk = DB.AddFile(file);
                if (chk != null) File.Delete(".\\users\\" + user.Username + "\\" + chk + Path.GetExtension(path));
                
                //ACK a fronte della ricezione del file
                handler.sendCommand("ACK");

                Console.WriteLine("[" + user.Ip + " - " + user.Username + " - " + DateTime.Now.ToString("HH:mm:ss") + "] File " + file.Filename + " of " + user.Username + " received");

            }
            catch (Exception e)
            {
                //just to skip the code
            }
        }

        private static void ReceiveFolder(SSLStream handler, UserClass user)
        {
            try
            {
                //Invio ACK a fronte di un comando DIR
                handler.sendCommand("ACK");

                //Ricevo il path del direttorio
                FolderClass folder = new FolderClass(handler.receiveCommand(), user, true);

                DB.AddFolder(folder);

                //Invio ACK a fronte del path
                handler.sendCommand("ACK");
            }
            catch (Exception e)
            {
                //just to skip the code
            }
        }

        private static void Delete(SSLStream handler, UserClass user)
        {
            string path;
            try
            {
                //ACK a fronte del comando DEL
                handler.sendCommand("ACK");

                //ricezione del path da eliminare
                path = handler.receiveCommand();

                if (DB.IsFile(path, user))
                {
                    //se è un file metto come assente un file
                    FileClass file = new FileClass();
                    file.User = user;
                    file.Filename = Path.GetFileName(path);
                    file.Folder.FolderPath = Path.GetDirectoryName(path);

                    DB.setFileAbsent(file);

                    handler.sendCommand("ACK");
                }
                if (DB.IsFolder(path, user))
                {
                    //se è una cartella metto come assente la cartella, tutte le sue sotto cartelle
                    //e tutti i file al suo interno
                    FolderClass folder = new FolderClass();
                    folder.User = user;
                    folder.FolderPath = path;
                    folder.Present = true;

                    DB.setFolderAbsent(folder);

                    handler.sendCommand("ACK");
                }
            }
            catch (Exception e)
            {
                //just to skip the code
            }
        }

        private static void Rename(SSLStream handler, UserClass user)
        {
            string oldPath, newPath, isdir;

            //ACK a fronte del comando RNM
            handler.sendCommand("ACK");

            //ricevo il vecchio path
            oldPath = handler.receiveCommand();

            //ACK a fronte del vecchio path
            handler.sendCommand("ACK");

            //ricevo il nuovo path
            newPath = handler.receiveCommand();

            //ACK a fronte del nuovo path
            handler.sendCommand("ACK");

            //ricevo se è un direttorio o meno
            isdir = handler.receiveCommand();

            //mando ACK a fronte dell'informazione - COMUNICAZIONE CONCLUSA
            handler.sendCommand("ACK");

            if (isdir == true.ToString())
            {
                //se è un direttorio
                FolderClass folder = new FolderClass();
                folder.User = user;
                folder.FolderPath = oldPath;
                folder.Present = true;

                DB.RenameFolder(folder, newPath);
            }
            else
            {
                //se è un file
                FileClass file = new FileClass();
                file.Filename = Path.GetFileName(oldPath);
                file.Folder.FolderPath = Path.GetDirectoryName(oldPath);
                file.User = user;
                string newFileName = Path.GetFileName(newPath);

                DB.RenameFile(file, newFileName);
            }
        }

        private static List<FileClass> SendListFileAbsent(SSLStream handler, UserClass user)
        {
            List<FileClass> allFiles = DB.getAllAbsentFiles(user);
            string responce;

            foreach (FileClass file in allFiles)
            {
                //Mando path + timestamp del file
                handler.sendCommand(file.Folder.FolderPath + "\\" + file.Filename + " " + file.Timestamp);
                responce = handler.receiveCommand();
                if (responce != "ACK")
                    throw new Exception("Expected ACK after path and timestamp");
                handler.sendCommand(file.Checksum);
                // System.Windows.MessageBox.Show("Sent: " + file.Timestamp);
                responce = handler.receiveCommand();
                if (responce != "ACK")
                    throw new Exception("Expected ACK after chksum");
            }

            handler.sendCommand("FIN");

            return allFiles;
        }

        private static void Recovery(SSLStream handler, UserClass user, List<FileClass> allFiles)
        {
            string responce;
            string path, timestamp;

            handler.sendCommand("ACK");

            while ((responce = handler.receiveCommand()) != "FIN")
            {
                FileClass file = new FileClass();
                file.User = user;

                //Ricevo SFL
                if (responce != "SFL")
                    throw new Exception("Expected SFL or FIN");

                //mando ACK a fronte di SFL
                handler.sendCommand("ACK");

                //ricevo il path del file
                path = handler.receiveCommand();

                file.Filename = Path.GetFileName(path);
                file.Folder.FolderPath = Path.GetDirectoryName(path);
                file.Folder.User = user;

                //mando ACK a fronte del percorso del file
                handler.sendCommand("ACK");

                //ricevo il timestamp della versione
                timestamp = handler.receiveCommand();
                file.Timestamp = timestamp;
                handler.sendCommand("ACK");

                file.Checksum = handler.receiveCommand();
                
                //Se la lista non è più coerente con il DB
                if (!DB.Exists(file, file.Folder))
                {
                    handler.sendCommand("NCK");
                    return;
                }

                handler.sendCommand("ACK");

                //Riceve SND
                handler.receiveCommand();

                foreach (FileClass f in allFiles)
                {
                    if (f.Filename == file.Filename && f.Timestamp == file.Timestamp)
                    {
                        handler.sendFile(".\\users\\" + user.Username + "\\" + f.Checksum + Path.GetExtension(file.Filename));
                        DB.setFilePresent(f);
                    }
                }
            }
        }
    }
}
