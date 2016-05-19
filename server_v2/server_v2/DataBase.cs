using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SQLite;
using System.IO;
using System.Data;

namespace Server_v2
{
    class DataBase
    {
        const int MAX_NUMBER_OF_VERSIONS = 3;

        //lock per rendere la classe thread safe messa nei metodi che eseguono le query
        internal static object lockDB = new object();

        #region CONSTRUCTOR

        /// <summary>
        /// Costruttore Database: 
        /// Crea tabelle se non è mai stato creato un DB.
        /// TESTATO!
        /// </summary>
        internal DataBase()
        {
            if (!File.Exists(".\\Database.db"))
            {
                try
                {
                    using (SQLiteConnection conn = SetConnection())
                    {
                        conn.Open();
                        using (SQLiteCommand com = conn.CreateCommand())
                        {
                            com.CommandText = @"BEGIN TRANSACTION; " +
                                              // Insert di un utente
                                              @"CREATE TABLE USER(Username TEXT PRIMARY KEY," +
                                                                @"Password TEXT," +
                                                                @"Sale TEXT," +
                                                                @"ClientPath TEXT, " +
                                                                @"ServerPath TEXT); " +
                                              @"CREATE TABLE FOLDER(FolderPath TEXT," +
                                                                   @"User TEXT," +
                                                                   @"ParentPath TEXT," +
                                                                   @"TimestampFolder DATETIME DEFAULT CURRENT_TIMESTAMP," +
                                                                   @"TimestampParent DATETIME," +
                                                                   @"Present TEXT DEFAULT 'True'," +

                                                                   @"PRIMARY KEY(FolderPath,User,TimestampFolder), " +
                                                                   @"FOREIGN KEY(User) REFERENCES USER (Username) ON DELETE CASCADE ON UPDATE CASCADE, " +
                                                                   @"FOREIGN KEY(ParentPath, User, TimestampParent) " +
                                                                   @"REFERENCES FOLDER(FolderPath, User, TimestampFolder) ON DELETE CASCADE ON UPDATE CASCADE); " +
                                              @"CREATE TABLE FILE(Filename TEXT," +
                                                                @"User TEXT," +
                                                                @"Folder TEXT," +
                                                                @"Checksum TEXT," +
                                                                @"TimestampFile DATETIME DEFAULT CURRENT_TIMESTAMP," +
                                                                @"TimestampFolder DATETIME," +
                                                                @"Present TEXT DEFAULT 'True'," +

                                                                @"PRIMARY KEY(Filename, User, Folder, Checksum, TimestampFile)," +
                                                                @"FOREIGN KEY(Folder, User, TimestampFolder) " +
                                                                @"REFERENCES FOLDER(FolderPath, User, TimestampFolder) ON DELETE CASCADE ON UPDATE CASCADE); " +
                                                
                                              @"END TRANSACTION;";

                            com.CommandType = CommandType.Text;

                            lock (lockDB)
                            {
                                com.ExecuteNonQuery();
                                Console.WriteLine("USER Table created");
                                Console.WriteLine("FOLDER Table created");
                                Console.WriteLine("FILE Table created");
                            }
                        }

                        conn.Close();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Database not created. Exception:");
                    Console.WriteLine(e.Message);
                    File.Delete(".\\Database.db");
                }
            }
        }

        #endregion

        #region ADD METHODS

        /// <summary>
        /// Aggiungi un utente alla tabella USER e la cartella da monitorare alla tabella FOLDER. 
        /// Se esiste ritorna false senza inserirlo, se non esiste lo crea.
        /// 
        /// NOTA: Il folder da monitorare che viene inserito nella tabella FOLDER ha come parent 
        /// una cartella fittizia "." che ha come data di creazione la mia data di nascita.
        /// Fatto con una transazione che permette di mantenere le proprietà ACID.
        /// 
        /// ATTENZIONE: Non permettere all'utente di registrarsi con il carattere "'".
        /// 
        /// TESTATO!
        /// </summary>
        /// <param name="user">Utente da inserire</param>
        internal bool AddUser(UserClass user)
        {
            try
            {
                if (!Exists(user))
                {
                    using (SQLiteConnection conn = SetConnection())
                    {
                        conn.Open();
                        using (SQLiteCommand com = conn.CreateCommand())
                        {
                            com.CommandText = @"BEGIN TRANSACTION; " +
                                              // Insert di un utente
                                              @"INSERT INTO USER (Username,Password,Sale,ClientPath,ServerPath) " +
                                              @"VALUES (@Username,@Password, @Sale, @ClientPath, @ServerPath); " +
                                              // Insert della cartella da monitorare nella tabella FOLDER
                                              @"INSERT INTO FOLDER (FolderPath,User,ParentPath,TimestampParent,Present) " +
                                              @"VALUES (@ClientPath, @Username, NULL, @TimestampParent, @Present); " +
                                              @"END TRANSACTION;";

                            com.CommandType = CommandType.Text;
                            com.Parameters.Add(new SQLiteParameter("@Username", user.Username));
                            com.Parameters.Add(new SQLiteParameter("@Password", user.Password));
                            com.Parameters.Add(new SQLiteParameter("@Sale", user.Sale));
                            com.Parameters.Add(new SQLiteParameter("@ClientPath", user.ClientPath));
                            com.Parameters.Add(new SQLiteParameter("@ServerPath", user.ServerPath));
                            com.Parameters.Add(new SQLiteParameter("@TimestampParent", DateTime.MinValue.ToString("yyyy-MM-dd HH:mm:ss")));
                            com.Parameters.Add(new SQLiteParameter("@Present", true.ToString()));
                            lock (lockDB)
                            {
                                using (SQLiteCommand comFK = conn.CreateCommand())
                                {
                                    comFK.CommandText = @"PRAGMA foreign_keys = ON";
                                    comFK.ExecuteNonQuery();
                                    com.ExecuteNonQuery();
                                }
                            }
                        }

                        conn.Close();
                    }

                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return false;
        }

        /// <summary>
        /// Aggiunge una cartella alla tabela FOLDER.
        /// L'oggetto FolderClass deve avere definito:
        /// - FolderPath
        /// - User
        /// TESTATO!
        /// </summary>
        /// <param name="folder">Cartella da aggiungere</param>
        internal bool AddFolder(FolderClass folder)
        {
            string parentPath;
            DateTime timestampParent;

            if (Exists(folder)) return false;

            //Creo la cartella padre
            folder.Parent = new FolderClass();
            folder.Parent.Present = true;
            folder.Parent.User = folder.User;
            folder.Parent.FolderPath = Path.GetDirectoryName(Path.GetDirectoryName(folder.FolderPath));
            parentPath = folder.Parent.FolderPath;
            timestampParent = getTimestampFolder(folder.Parent);
            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"INSERT INTO FOLDER (FolderPath,User,ParentPath,TimestampParent,Present)" +
                                          @"VALUES (@FolderPath,@User,@ParentPath,@TimestampParent,@Present)";

                        com.CommandType = CommandType.Text;
                        com.Parameters.Add(new SQLiteParameter("@User", folder.User.Username));
                        com.Parameters.Add(new SQLiteParameter("@FolderPath", folder.FolderPath));
                        com.Parameters.Add(new SQLiteParameter("@ParentPath", parentPath));
                        com.Parameters.Add(new SQLiteParameter("@TimestampParent", timestampParent.ToString("yyyy-MM-dd HH:mm:ss")));
                        com.Parameters.Add(new SQLiteParameter("@Present", true.ToString()));

                        lock (lockDB)
                        {
                            com.ExecuteNonQuery();
                        }
                    }

                    conn.Close();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return true;
        }

        /// <summary>
        /// Aggiunge un file alla tabella FILE.
        /// Devono essere definiti:
        /// - Filename
        /// - User
        /// - Folder
        /// - Checksum
        /// A sua volta l'oggetto folder vede avere definito:
        /// - FolderPath
        /// - User
        ///  
        /// NOTA: Controllare la presenza della cartella che contiene il file.
        /// 
        /// TESTATO!
        /// </summary>
        /// <param name="file"></param>
        internal string AddFile(FileClass file)
        {
            //Se aggiungo un file la cartella deve essere presente. Esiste una sola cartella presente
            //con queste caratteristiche.
            int n = 0;

            file.Folder.Present = true;
            DateTime timestampFolder = getTimestampFolder(file.Folder);

            //ritorno un checksum se si elimina un file
            string checksum = null;

            if (this.Exists(file, file.Folder))
            {
                setFilePresent(file);
                return null;
            }

            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();

                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"SELECT COUNT(*) " +
                                          @"FROM FILE " +
                                          @"WHERE Filename = @Filename AND " +
                                                             @"User = @User AND " +
                                                             @"Folder = @Folder AND " +
                                                             @"TimestampFolder = @TimestampFolder;";

                        com.CommandType = CommandType.Text;
                        com.Parameters.Add(new SQLiteParameter("@User", file.User.Username));
                        com.Parameters.Add(new SQLiteParameter("@Filename", file.Filename));
                        com.Parameters.Add(new SQLiteParameter("@Folder", file.Folder.FolderPath));
                        com.Parameters.Add(new SQLiteParameter("@TimestampFolder", timestampFolder));

                        lock (lockDB)
                        {
                            SQLiteDataReader r = com.ExecuteReader();
                            while (r.Read())
                                n = r.GetInt32(0);
                        }
                    }

                    conn.Close();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            if (n >= MAX_NUMBER_OF_VERSIONS)
            {
                //Se raggiungo il numero massimo di versioni

                //Identifico il file con timestamp più vecchio

                checksum = getOldestChecksum(file);

                try
                {
                    using (SQLiteConnection conn = SetConnection())
                    {
                        conn.Open();
                        using (SQLiteCommand com = conn.CreateCommand())
                        {
                            com.CommandText = @"BEGIN TRANSACTION; " +

                                              //Cancello il file più vecchio
                                              @"DELETE FROM FILE " +
                                              @"WHERE Filename = @Filename AND " +
                                                    @"User = @User AND " +
                                                    @"Folder = @Folder AND " +
                                                    @"Checksum = @checksum; " +
                                              
                                              //Metto a false i più vecchi
                                              @"UPDATE FILE " +
                                              @"SET Present = @False " +
                                              @"WHERE Filename = @Filename AND User = @User AND Folder = @Folder ;" +

                                              //Inserisco
                                              @"INSERT INTO FILE (Filename,User,Folder,Checksum,TimestampFolder)" +
                                              @"VALUES (@Filename,@User,@Folder,@Checksum,@TimestampFolder);" +

                                              @"END TRANSACTION;";

                            com.CommandType = CommandType.Text;
                            com.Parameters.Add(new SQLiteParameter("@User", file.User.Username));
                            com.Parameters.Add(new SQLiteParameter("@Filename", file.Filename));
                            com.Parameters.Add(new SQLiteParameter("@Folder", file.Folder.FolderPath));
                            com.Parameters.Add(new SQLiteParameter("@TimestampFolder", timestampFolder.ToString("yyyy-MM-dd HH:mm:ss")));
                            com.Parameters.Add(new SQLiteParameter("@Checksum", file.Checksum));
                            com.Parameters.Add(new SQLiteParameter("@checksum", checksum));
                            com.Parameters.Add(new SQLiteParameter("@False", false.ToString()));

                            lock (lockDB)
                            {
                                com.ExecuteNonQuery();
                            }
                        }

                        conn.Close();
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                //se ho un file con questo checksum che punta a cartelle presenti diversa da quella di questo file
                //non devo eliminare questo file e ripongo il checksum a null


                try
                {
                    using (SQLiteConnection conn = SetConnection())
                    {
                        conn.Open();
                        using (SQLiteCommand com = conn.CreateCommand())
                        {
                            com.CommandText = @"SELECT COUNT(*) " +
                                              @"FROM FILE " +
                                              @"WHERE Checksum = @Checksum AND " +
                                              @"User = @User AND " +
                                              @"Folder <> @Folder;";

                            com.CommandType = CommandType.Text;
                            com.Parameters.Add(new SQLiteParameter("@User", file.User.Username));
                            com.Parameters.Add(new SQLiteParameter("@Folder", file.Folder.FolderPath));
                            com.Parameters.Add(new SQLiteParameter("@Checksum", file.Checksum));

                            lock (lockDB)
                            {
                                SQLiteDataReader r = com.ExecuteReader(); ;
                                while (r.Read())
                                    n = r.GetInt32(0);
                            }
                        }

                        conn.Close();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                
                if (n != 0)
                    checksum = null;
            }
            else
            {


                try
                {
                    using (SQLiteConnection conn = SetConnection())
                    {
                        conn.Open();
                        using (SQLiteCommand com = conn.CreateCommand())
                        {
                            com.CommandText = @"BEGIN TRANSACTION; " +

                                              @"UPDATE FILE " +
                                              @"SET Present = @False " +
                                              @"WHERE Filename = @Filename AND User = @User AND Folder = @Folder ;" +
                                              
                                              @"INSERT INTO FILE (Filename,User,Folder,Checksum,TimestampFolder)" +
                                              @"VALUES (@Filename,@User,@Folder,@Checksum,@TimestampFolder);" +
                                              
                                              @"END TRANSACTION;";

                            com.CommandType = CommandType.Text;
                            com.Parameters.Add(new SQLiteParameter("@User", file.User.Username));
                            com.Parameters.Add(new SQLiteParameter("@Filename", file.Filename));
                            com.Parameters.Add(new SQLiteParameter("@Folder", file.Folder.FolderPath));
                            com.Parameters.Add(new SQLiteParameter("@TimestampFolder", timestampFolder.ToString("yyyy-MM-dd HH:mm:ss")));
                            com.Parameters.Add(new SQLiteParameter("@Checksum", file.Checksum));
                            com.Parameters.Add(new SQLiteParameter("@False", false.ToString()));

                            lock (lockDB)
                            {
                                com.ExecuteNonQuery();
                            }
                        }

                        conn.Close();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            return checksum;
        }

        internal void setFilePresent(FileClass file)
        {

            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"BEGIN TRANSACTION; " +

                                          @"UPDATE FILE " +
                                          @"SET Present = @False " +
                                          @"WHERE Filename = @Filename AND User = @User AND Folder = @Folder; " +

                                          @"UPDATE FILE " +
                                          @"SET Present = @True, TimestampFile = @TimestampFile " +
                                          @"WHERE Filename = @Filename AND " +
                                                @"Folder = @Folder AND " +
                                                @"Present = @False AND " +
                                                @"User = @User AND " +
                                                @"Checksum = @Checksum; " +

                                          @"END TRANSACTION; ";


                        com.CommandType = CommandType.Text;

                        com.Parameters.Add(new SQLiteParameter("@True", true.ToString()));
                        com.Parameters.Add(new SQLiteParameter("@TimestampFile", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")));
                        com.Parameters.Add(new SQLiteParameter("@Filename", file.Filename));
                        com.Parameters.Add(new SQLiteParameter("@Folder", file.Folder.FolderPath));
                        com.Parameters.Add(new SQLiteParameter("@False", false.ToString()));
                        com.Parameters.Add(new SQLiteParameter("@User", file.User.Username));
                        com.Parameters.Add(new SQLiteParameter("@Checksum", file.Checksum));

                        lock (lockDB)
                        {
                            com.ExecuteNonQuery();
                        }
                    }

                    conn.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        #endregion

        #region GET METHODS

        internal bool IsFile(string path, UserClass user)
        {
            int n = 0;

            if (IsFolder(path, user)) return false;

            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"SELECT COUNT(*) " +
                                          @"FROM FILE " +
                                          @"WHERE Filename = @Filename AND " +
                                                @"Folder = @Folder AND " +
                                                @"Present = @Present AND " +
                                                @"User = @User;";

                        com.CommandType = CommandType.Text;

                        com.Parameters.Add(new SQLiteParameter("@Filename", Path.GetFileName(path)));
                        com.Parameters.Add(new SQLiteParameter("@Folder", Path.GetDirectoryName(path) + "\\"));
                        com.Parameters.Add(new SQLiteParameter("@Present", true.ToString()));
                        com.Parameters.Add(new SQLiteParameter("@User", user.Username));

                        lock (lockDB)
                        {
                            SQLiteDataReader r = com.ExecuteReader();
                            while (r.Read())
                                n = r.GetInt32(0);
                        }
                    }

                    conn.Close();
                    
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            if (n == 0) return false;

            return true;
        }

        internal bool IsFolder(string path, UserClass user)
        {
            int n = 0;

            if (path.Last() != '\\') path = path + "\\";

            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"SELECT COUNT(*) " +
                                          @"FROM FOLDER " +
                                          @"WHERE FolderPath = @FolderPath AND " +
                                                @"User = @User AND " +
                                                @"Present = @Present;";

                        com.CommandType = CommandType.Text;

                        com.Parameters.Add(new SQLiteParameter("@FolderPath", path));
                        com.Parameters.Add(new SQLiteParameter("@User", user.Username));
                        com.Parameters.Add(new SQLiteParameter("@Present", true.ToString()));

                        lock (lockDB)
                        {
                            SQLiteDataReader r = com.ExecuteReader();
                            while (r.Read())
                                n = r.GetInt32(0);
                        }
                    }

                    conn.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            if (n == 0) return false;

            return true;
        }

        /// <summary>
        /// Ottiene il timestamp della cartella richiesta. L'oggetto FolderCLass deve avere 
        /// definiti:
        /// - FolderPath
        /// - Present 
        /// - User
        /// 
        /// TESTATO!
        /// </summary>
        /// <param name="folder">Cartella richiesta</param>
        /// <returns></returns>
        private DateTime getTimestampFolder(FolderClass folder)
        {
            DateTime dateTime = new DateTime();

            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"SELECT TimestampFolder " +
                                          @"FROM Folder " +
                                          @"WHERE FolderPath = @FolderPath " +
                                                @"AND Present = @Present " +
                                                @"AND User = @User ";

                        com.CommandType = CommandType.Text;

                        com.Parameters.Add(new SQLiteParameter("@FolderPath", folder.FolderPath));
                        com.Parameters.Add(new SQLiteParameter("@User", folder.User.Username));
                        com.Parameters.Add(new SQLiteParameter("@Present", folder.Present.ToString()));

                        lock (lockDB)
                        {
                            SQLiteDataReader r = com.ExecuteReader();
                            while (r.Read())
                                dateTime = (DateTime)r["TimestampFolder"];
                        }
                    }

                    conn.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
            return dateTime;
        }

        /// <summary>
        /// Ottiene il timestamp del file. L'oggetto file deve avere definito:
        /// - Filename
        /// - Checksum
        /// - Username
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private DateTime getTimestampFile(FileClass file)
        {
            DateTime dateTime = new DateTime();

            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"SELECT TimestampFile " +
                                          @"FROM FILE " +
                                          @"WHERE Filename = '" + file.Filename + "' " +
                                                @"AND Checksum = '" + file.Checksum + "' " +
                                                @"AND User = '" + file.User.Username + "' ";

                        com.CommandType = CommandType.Text;

                        com.Parameters.Add(new SQLiteParameter("@Filename", file.Filename));
                        com.Parameters.Add(new SQLiteParameter("@User", file.User.Username));
                        com.Parameters.Add(new SQLiteParameter("@Checksum", file.Checksum));

                        lock (lockDB)
                        {
                            SQLiteDataReader r = com.ExecuteReader();
                            while (r.Read())
                                dateTime = (DateTime)r["TimestampFile"];
                        }
                    }

                    conn.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
            return dateTime;
        }

        /// <summary>
        /// Ritorna il checksum con timestamp più vecchio.
        /// Il file passato deve avere definito:
        /// - Filename
        /// - User
        /// - Folder
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private string getOldestChecksum(FileClass file)
        {
            string checksum = null;

            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"SELECT Checksum  " +
                                          @"FROM FILE " +
                                          @"WHERE Filename = @Filename AND " +
                                                @"User = @User AND " +
                                                @"Folder = @Folder AND " +
                                                @"TimestampFile IN (SELECT min(TimestampFile)" +
                                                                   @"FROM FILE " +
                                                                   @"WHERE Filename = @Filename AND " +
                                                                   @"User = @User AND " +
                                                                   @"Folder = @Folder);";

                        com.CommandType = CommandType.Text;

                        com.Parameters.Add(new SQLiteParameter("@Filename", file.Filename));
                        com.Parameters.Add(new SQLiteParameter("@User", file.User.Username));
                        com.Parameters.Add(new SQLiteParameter("@Folder", file.Folder.FolderPath));
                        
                        lock (lockDB)
                        {
                            SQLiteDataReader r = com.ExecuteReader();
                            while (r.Read())
                                checksum = r["Checksum"].ToString();
                        }
                    }

                    conn.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
            return checksum;
        }

        /// <summary>
        /// Ritorna la password dell'utente. Prima di chiamare questa funzione verificare che l'utente esista!
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        internal string getPassword(UserClass user)
        {
            string psw = null;

            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"SELECT Password " +
                                          @"FROM USER " +
                                          @"WHERE Username = @Username";

                        com.CommandType = CommandType.Text;

                        com.Parameters.Add(new SQLiteParameter("@Username", user.Username));

                        lock (lockDB)
                        {
                            SQLiteDataReader r = com.ExecuteReader();
                            while (r.Read())
                                psw = r["Password"].ToString();
                        }
                    }

                    conn.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return psw;
        }

        /// <summary>
        /// Ritorna il Sale di un utente.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        internal string getSale(UserClass user)
        {
            string sale = null;

            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"SELECT Sale " +
                                          @"FROM USER " +
                                          @"WHERE Username = @Username";

                        com.CommandType = CommandType.Text;

                        com.Parameters.Add(new SQLiteParameter("@Username", user.Username));

                        lock (lockDB)
                        {
                            SQLiteDataReader r = com.ExecuteReader();
                            while (r.Read())
                                sale = (string)r["Sale"];
                        }
                    }

                    conn.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return sale;
        }

        /// <summary>
        /// Ritorna il path dell'utente della cartella sincronizzata.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        internal string getClientPath(UserClass user)
        {
            string path = null;

            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"SELECT ClientPath " +
                                          @"FROM USER " +
                                          @"WHERE Username = @Username";

                        com.CommandType = CommandType.Text;

                        com.Parameters.Add(new SQLiteParameter("@Username", user.Username));

                        lock (lockDB)
                        {
                            SQLiteDataReader r = com.ExecuteReader();
                            while (r.Read())
                                path = (string)r["ClientPath"];
                        }
                    }

                    conn.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
            return path;
        }

        internal FolderClass getFolder(string folderpath, UserClass user)
        {
            FolderClass folder = new FolderClass();

            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"SELECT FolderPath, Present " +
                                          @"FROM FOLDER " +
                                          @"WHERE FolderPath = @FolderPath " +
                                          @"AND Present = @Present " +
                                          @"AND User = @User ";

                        com.CommandType = CommandType.Text;

                        com.Parameters.Add(new SQLiteParameter("@FolderPath", folderpath));
                        com.Parameters.Add(new SQLiteParameter("@Present", true.ToString()));
                        com.Parameters.Add(new SQLiteParameter("@User", user.Username));

                        lock (lockDB)
                        {
                            SQLiteDataReader r = com.ExecuteReader();
                            while (r.Read())
                            {
                                folder.FolderPath = (string)r["FolderPath"];
                                folder.Present = true;
                                folder.User = user;
                            }
                        }
                    }

                    conn.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
            return folder;
        }

        internal List<FileClass> getFilePresent(UserClass user)
        {
            List<FileClass> files = new List<FileClass>();

            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"SELECT Filename, Folder, Checksum " +
                                          @"FROM FILE " +
                                          @"WHERE User = @User AND " +
                                                @"Present = @Present; ";

                        com.CommandType = CommandType.Text;
                        
                        com.Parameters.Add(new SQLiteParameter("@Present", true.ToString()));
                        com.Parameters.Add(new SQLiteParameter("@User", user.Username));

                        lock (lockDB)
                        {
                            SQLiteDataReader r = com.ExecuteReader();
                            while (r.Read())
                            {
                                FileClass file = new FileClass();
                                file.Filename = r["Filename"].ToString();
                                file.Checksum = r["Checksum"].ToString();
                                file.User = user;
                                file.Folder.FolderPath = r["Folder"].ToString();
                                file.Folder.Present = true;
                                file.Folder.User = user;
                                files.Add(file);
                            }
                        }
                    }

                    conn.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
            return files;
        }

        internal List<FolderClass> getFolderPresent(UserClass user)
        {
            List<FolderClass> folders = new List<FolderClass>();

            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"SELECT FolderPath, ParentPath " +
                                          @"FROM FOLDER F, USER U  " +
                                          @"WHERE F.User = U.Username AND F.FolderPath <> U.ClientPath AND " +
                                                @"F.User = @User AND " +
                                                @"F.Present = @Present; ";

                        com.CommandType = CommandType.Text;

                        com.Parameters.Add(new SQLiteParameter("@Present", true.ToString()));
                        com.Parameters.Add(new SQLiteParameter("@User", user.Username));

                        lock (lockDB)
                        {
                            SQLiteDataReader r = com.ExecuteReader();
                            while (r.Read())
                            {
                                FolderClass folder = new FolderClass();
                                folder.FolderPath = (string)r["FolderPath"];
                                folder.Parent.FolderPath = (string)r["ParentPath"];
                                folder.Parent.User = user;
                                folder.Present = true;
                                folder.User = user;
                                folders.Add(folder);
                            }
                        }
                    }

                    conn.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
            return folders;
        }

        internal List<FileClass> getAllAbsentFiles(UserClass user)
        {
            List<FileClass> allFilesVersions = new List<FileClass>();

            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"SELECT Filename, Folder, Checksum, TimestampFile  " +
                                          @"FROM FILE " +
                                          @"WHERE User = @User AND " +
                                                @"Present = @Present; ";

                        com.CommandType = CommandType.Text;

                        com.Parameters.Add(new SQLiteParameter("@Present", false.ToString()));
                        com.Parameters.Add(new SQLiteParameter("@User", user.Username));

                        lock (lockDB)
                        {
                            SQLiteDataReader r = com.ExecuteReader();
                            while (r.Read())
                            {
                                FileClass file = new FileClass();
                                file.Filename = (string)r["Filename"];
                                file.Checksum = (string)r["Checksum"];
                                file.Timestamp = r["TimestampFile"].ToString();
                                file.User = user;
                                file.Folder.FolderPath = (string)r["Folder"];
                                file.Folder.Present = true;
                                file.Folder.User = user;
                                allFilesVersions.Add(file);
                            }
                        }
                    }

                    conn.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
            return allFilesVersions;
        }

        #endregion

        #region DELETE

        /// <summary>
        /// Cancella il file più vecchio. Non vi è la necessità di eliminare un file specifico
        /// </summary>
        /// <param name="file"></param>
        private void deleteOldestFile(FileClass file)
        {
            DateTime dt = DateTime.MinValue;

            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"SELECT min(Timestamp) " +
                                          @"FROM FILE " +
                                          @"WHERE Filename = '" + file.Filename + "' AND " +
                                                @"User = '" + file.User.Username + "' AND " +
                                                @"Folder = '" + file.Folder.FolderPath + "'";

                        com.CommandType = CommandType.Text;

                        com.Parameters.Add(new SQLiteParameter("@Filename", file.Filename));
                        com.Parameters.Add(new SQLiteParameter("@Folder", file.Folder.FolderPath));
                        com.Parameters.Add(new SQLiteParameter("@User", file.User.Username));

                        lock (lockDB)
                        {
                            SQLiteDataReader r = com.ExecuteReader();
                            while (r.Read())
                            {
                                while (r.Read())
                                    dt = r.GetDateTime(0);
                            }
                        }
                    }

                    conn.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            if (dt != DateTime.MinValue)
            {
                try
                {
                    using (SQLiteConnection conn = SetConnection())
                    {
                        conn.Open();
                        using (SQLiteCommand com = conn.CreateCommand())
                        {
                            com.CommandText = @"DELETE FROM FILE " + 
                                              @"WHERE Filename = @Filename AND " +
                                                    @"User = @User AND " +
                                                    @"Folder = @Folder AND " +
                                                    @"TimestampFile = @TimestampFile";

                            com.CommandType = CommandType.Text;

                            com.Parameters.Add(new SQLiteParameter("@Filename", file.Filename));
                            com.Parameters.Add(new SQLiteParameter("@Folder", file.Folder.FolderPath));
                            com.Parameters.Add(new SQLiteParameter("@User", file.User.Username));
                            com.Parameters.Add(new SQLiteParameter("@TimestampFile", dt.ToString("yyyy-MM-dd HH:mm:ss")));

                            lock (lockDB)
                            {
                                com.ExecuteNonQuery();
                            }
                        }

                        conn.Close();

                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        /// <summary>
        /// Mette come assenti la cartella eliminata e tutte le sue sotto cartelle e i suoi file.
        /// </summary>
        /// <param name="folder"></param>
        internal void setFolderAbsent(FolderClass folder)
        {
            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"BEGIN TRANSACTION; " +

                                          @"UPDATE FOLDER " +
                                          @"SET Present = @False " +
                                          @"WHERE FolderPath LIKE @Folder AND " +
                                                @"User = @User AND " +
                                                @"Present = @True; " +

                                          @"UPDATE FILE " +
                                          @"SET Present = @False " +
                                          @"WHERE Folder LIKE @Folder AND " +
                                                @"User = @User AND " +
                                                @"Present = @True; " +

                                          @"END TRANSACTION;";

                        com.CommandType = CommandType.Text;

                        com.Parameters.Add(new SQLiteParameter("@False", false.ToString()));
                        com.Parameters.Add(new SQLiteParameter("@Folder", folder.FolderPath + "%"));
                        com.Parameters.Add(new SQLiteParameter("@User", folder.User.Username));
                        com.Parameters.Add(new SQLiteParameter("@True", true.ToString()));

                        lock (lockDB)
                        {
                            com.ExecuteNonQuery();
                        }
                    }

                    conn.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }

        /// <summary>
        /// Mette come assenti i file eliminati.
        /// Deve essere definito il folderPath del folder del file.
        /// </summary>
        /// <param name="file"></param>
        internal void setFileAbsent(FileClass file)
        {
            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"UPDATE FILE " +
                                          @"SET Present = @False " +
                                          @"WHERE Filename = @Filename AND " +
                                                @"Folder = @Folder AND " +
                                                @"Present = @True AND " +
                                                @"User = @User;";

                        com.CommandType = CommandType.Text;

                        com.Parameters.Add(new SQLiteParameter("@False", false.ToString()));
                        com.Parameters.Add(new SQLiteParameter("@Filename", file.Filename));
                        com.Parameters.Add(new SQLiteParameter("@Folder", file.Folder.FolderPath));
                        com.Parameters.Add(new SQLiteParameter("@User", file.User.Username));
                        com.Parameters.Add(new SQLiteParameter("@True", true.ToString()));

                        lock (lockDB)
                        {
                            com.ExecuteNonQuery();
                        }
                    }

                    conn.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Elimina un utente nella tabella USER.
        /// L'oggetto user deve avere definito:
        /// - Username
        /// </summary>
        /// <param name="user"></param>
        internal void deleteUser(UserClass user)
        {
            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"DELETE FROM USER WHERE Username = @Username; ";

                        com.CommandType = CommandType.Text;

                        com.Parameters.Add(new SQLiteParameter("@Username", user.Username));

                        lock (lockDB)
                        {
                            using (SQLiteCommand comFK = conn.CreateCommand())
                            {
                                comFK.CommandText = @"PRAGMA foreign_keys = ON";
                                comFK.ExecuteNonQuery();
                                com.ExecuteNonQuery();
                            }
                        }
                    }

                    conn.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
        }

        /// <summary>
        /// Cancella tutti i file e le cartelle presenti nelle due liste
        /// </summary>
        /// <param name="listFiles"></param>
        /// <param name="listFolders"></param>
        internal void realigns(List<FileClass> listFiles, List<FolderClass> listFolders)
        {

            if (listFiles.Count == 0 && listFolders.Count == 0) return;

            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"BEGIN TRANSACTION; ";

                        com.CommandType = CommandType.Text;

                        int i = 0;

                        foreach (FileClass file in listFiles)
                        {
                            com.CommandText = com.CommandText +
                                             @"UPDATE FILE " +
                                             @"SET Present = @False " +
                                             @"WHERE Filename = @Filename" + i.ToString() +  @" AND " +
                                                   @"Folder = @Folder" + i.ToString() + @" AND " +
                                                   @"Present = @True AND " +
                                                   @"User = @User; ";

                            
                            com.Parameters.Add(new SQLiteParameter("@Filename" + i.ToString(), file.Filename));
                            com.Parameters.Add(new SQLiteParameter("@Folder" + i.ToString(), file.Folder.FolderPath));
                            
                            i++;
                        }
                        foreach (FolderClass folder in listFolders)
                        {
                            com.CommandText = com.CommandText +
                                             @"UPDATE FOLDER " +
                                             @"SET Present = @False " +
                                             @"WHERE FolderPath LIKE @Folder" + i.ToString() + @" AND " +
                                                   @"User = @User AND " +
                                                   @"Present = @True; " +

                                             @"UPDATE FILE " +
                                             @"SET Present = @False " +
                                             @"WHERE Folder LIKE @Folder" + i.ToString() + @" AND " +
                                                   @"User = @User AND " +
                                                   @"Present = @True; ";

                            com.Parameters.Add(new SQLiteParameter("@Folder" + i.ToString(), folder.FolderPath + "%"));

                            i++;
                        }

                        com.CommandText = com.CommandText + @" END TRANSACTION; ";

                        com.Parameters.Add(new SQLiteParameter("@False", false.ToString()));
                        if(listFiles.Count > 0)
                            com.Parameters.Add(new SQLiteParameter("@User", listFiles[0].User.Username));
                        else
                            com.Parameters.Add(new SQLiteParameter("@User", listFolders[0].User.Username));
                        com.Parameters.Add(new SQLiteParameter("@True", true.ToString()));

                        lock (lockDB)
                        {
                            using (SQLiteCommand comFK = conn.CreateCommand())
                            {
                                comFK.CommandText = @"PRAGMA foreign_keys = ON";
                                comFK.ExecuteNonQuery();
                                com.ExecuteNonQuery();
                            }
                        }
                    }

                    conn.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
        }

        /// <summary>
        /// Elimina un utente dallla tabella LOGGED_IN
        /// Deve essere definito nell'oggetto UserClass Username.
        /// </summary>
        /// <param name="user"></param>
        internal void LogOff(UserClass user)
        {
            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"DELETE FROM LOGGED_IN WHERE Username = @Username";

                        com.CommandType = CommandType.Text;

                        com.Parameters.Add(new SQLiteParameter("@Username", user.Username));

                        lock (lockDB)
                        {
                            using (SQLiteCommand comFK = conn.CreateCommand())
                            {
                                comFK.CommandText = @"PRAGMA foreign_keys = ON";
                                comFK.ExecuteNonQuery();
                                com.ExecuteNonQuery();
                            }
                        }
                    }

                    conn.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
        }

        #endregion

        #region RENAME

        /// <summary>
        /// Vengono rinominati tutti i file con lo stesso nome contenuti nella stessa cartella.
        /// Non viene considerato il checksum in quanto si vuole mantenere allineati i nomi di tutte 
        /// le versioni precedenti del file.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="newFileName"></param>
        internal void RenameFile(FileClass file, string newFileName)
        {

            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"UPDATE FILE " +
                                          @"SET Filename = @NewFilename " +
                                          @"WHERE Filename = @Filename AND " +
                                                @"User = @User AND " +
                                                @"Folder = @Folder;";

                        com.CommandType = CommandType.Text;

                        com.Parameters.Add(new SQLiteParameter("@NewFilename", newFileName));
                        com.Parameters.Add(new SQLiteParameter("@Filename", file.Filename));
                        com.Parameters.Add(new SQLiteParameter("@User", file.User.Username));
                        com.Parameters.Add(new SQLiteParameter("@Folder", file.Folder.FolderPath));

                        lock (lockDB)
                        {
                            using (SQLiteCommand comFK = conn.CreateCommand())
                            {
                                comFK.CommandText = @"PRAGMA foreign_keys = ON";
                                comFK.ExecuteNonQuery();
                                com.ExecuteNonQuery();
                            }
                        }
                    }

                    conn.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
        }

        internal void RenameFolder(FolderClass folder, string newFolderPath)
        {

            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"UPDATE FOLDER " +
                                          @"SET FolderPath = replace( FolderPath, @OldFolderPathA, @NewFolderPath ) " +
                                          @"WHERE FolderPath LIKE @OldFolderPath AND User = @User; ";

                        com.CommandType = CommandType.Text;

                        com.Parameters.Add(new SQLiteParameter("@OldFolderPathA", folder.FolderPath));
                        com.Parameters.Add(new SQLiteParameter("@OldFolderPath", folder.FolderPath + "%"));
                        com.Parameters.Add(new SQLiteParameter("@NewFolderPath", newFolderPath + "\\"));
                        com.Parameters.Add(new SQLiteParameter("@User", folder.User.Username));

                        lock (lockDB)
                        {
                            using (SQLiteCommand comFK = conn.CreateCommand())
                            {
                                comFK.CommandText = @"PRAGMA foreign_keys = ON";
                                comFK.ExecuteNonQuery();
                                com.ExecuteNonQuery();
                            }
                        }
                    }

                    conn.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }

        #endregion

        #region PRINT METHODS

        /// <summary>
        /// Stampa la tabella USER.
        /// TESTATO!
        /// </summary>
        internal void printUsers()
        {

            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"SELECT * FROM USER ORDER BY Username";

                        com.CommandType = CommandType.Text;

                        lock (lockDB)
                        {
                            SQLiteDataReader r = com.ExecuteReader();
                            Console.WriteLine("\nUSER Table:");
                            int i = 0;
                            while (r.Read())
                            {
                                Console.WriteLine("\nUsername:\t" + r["Username"].ToString());
                                Console.WriteLine("Password:\t" + r["Password"].ToString());
                                Console.WriteLine("Sale:\t\t" + r["Sale"].ToString());
                                Console.WriteLine("Client Path:\t" + r["ClientPath"].ToString());
                                Console.WriteLine("Server Path:\t" + r["ServerPath"].ToString() + "\n");
                                i++;
                            }
                            if (i == 0)
                                Console.WriteLine("Table empty.");
                        }
                    }

                    conn.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
        }

        /// <summary>
        /// Stampa la tabella FOLDER.
        /// TESTATO!
        /// </summary>
        internal void printFolders()
        {

            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"SELECT * FROM FOLDER ORDER BY User";

                        com.CommandType = CommandType.Text;

                        lock (lockDB)
                        {
                            SQLiteDataReader r = com.ExecuteReader();
                            Console.WriteLine("\nFOLDER Table:");
                            int i = 0;
                            while (r.Read())
                            {
                                Console.WriteLine("\nFolder Path:\t\t" + r["FolderPath"].ToString());
                                Console.WriteLine("User:\t\t\t" + r["User"].ToString());
                                Console.WriteLine("Parent Path:\t\t" + r["ParentPath"].ToString());
                                Console.WriteLine("Timestamp Folder:\t" + r["TimestampFolder"].ToString());
                                Console.WriteLine("Timestamp Parent:\t" + r["TimestampParent"].ToString());
                                Console.WriteLine("Present:\t\t\t" + r["Present"].ToString() + "\n");
                                i++;
                            }
                            if (i == 0)
                                Console.WriteLine("Table empty.");
                        }
                    }

                    conn.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Stampa la tabella FILE.
        /// TESTATO!
        /// </summary>
        internal void printFiles()
        {

            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"SELECT * FROM FILE ORDER BY User,Filename";

                        com.CommandType = CommandType.Text;

                        lock (lockDB)
                        {
                            SQLiteDataReader r = com.ExecuteReader();
                            Console.WriteLine("\nFILE Table:");
                            int i = 0;
                            while (r.Read())
                            {
                                Console.WriteLine("\nFile Name:\t\t" + r["Filename"].ToString());
                                Console.WriteLine("User:\t\t\t" + r["User"].ToString());
                                Console.WriteLine("Checksum:\t\t" + r["Checksum"].ToString());
                                Console.WriteLine("Folder:\t\t\t" + r["Folder"].ToString());
                                Console.WriteLine("Timestamp File:\t\t" + r["TimestampFile"].ToString());
                                Console.WriteLine("Timestamp Folder:\t" + r["TimestampFolder"].ToString());
                                Console.WriteLine("Present:\t\t\t" + r["Present"].ToString() + "\n");
                                i++;
                            }
                            if (i == 0)
                                Console.WriteLine("Table empty.");
                        }
                    }

                    conn.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        #endregion

        #region SEARCH METHODS

        /// <summary>
        /// Cerca tutti gli utenti.
        /// TESTATO
        /// </summary>
        /// <returns></returns>
        private List<string> SearchUsers()
        {
            List<string> res = new List<string>();

            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"SELECT Username " +
                                          @"FROM USER";

                        com.CommandType = CommandType.Text;

                        lock (lockDB)
                        {
                            SQLiteDataReader r = com.ExecuteReader();
                            while (r.Read())
                                res.Add((string)r["Username"]);
                        }
                    }

                    conn.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return res;
        }

        /// <summary>
        /// Cerca tutti i file di un utente e ritorna la lista con i checksum dei file
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private List<string> SearchAllFilesOfUser(UserClass user)
        {
            List<string> res = new List<string>();

            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"SELECT Checksum " +
                                          @"FROM FILE " +
                                          @"WHERE User = '" + user.Username + "';";

                        com.Parameters.Add(new SQLiteParameter("@User", user.Username));

                        com.CommandType = CommandType.Text;

                        lock (lockDB)
                        {
                            SQLiteDataReader r = com.ExecuteReader();
                            while (r.Read())
                                res.Add((string)r["Checksum"]);
                        }
                    }

                    conn.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
            return res;
        }

        /// <summary>
        /// Cerca tutti i file di un utente specifico nella cartella folder
        /// TESTATO
        /// </summary>
        /// <param name="utente"></param>
        /// <returns></returns>
        internal List<FileClass> SearchFilesInFolderOfUser(UserClass user, FolderClass folder)
        {
            List<FileClass> res = new List<FileClass>();

            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"SELECT Filename, Checksum " +
                                          @"FROM FILE " +
                                          @"WHERE User = @User AND " +
                                                @"Folder = @Folder;";

                        com.Parameters.Add(new SQLiteParameter("@User", user.Username));
                        com.Parameters.Add(new SQLiteParameter("@Folder", folder.FolderPath));

                        com.CommandType = CommandType.Text;

                        lock (lockDB)
                        {
                            SQLiteDataReader r = com.ExecuteReader();
                            while (r.Read())
                            {
                                FileClass file = new FileClass(user);
                                file.Folder = folder;
                                file.Filename = r["Filename"].ToString();
                                file.Checksum = r["Checksum"].ToString();
                                res.Add(file);
                            }
                        }
                    }

                    conn.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return res;
        }

        internal List<string> SearchFilesPresentInFolderOfUser(UserClass user, FolderClass folder)
        {
            List<string> res = new List<string>();

            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"SELECT Checksum " +
                                          @"FROM FILE " +
                                          @"WHERE User = @User AND " +
                                                @"Folder = @Folder AND Present = @Present;";

                        com.Parameters.Add(new SQLiteParameter("@User", user.Username));
                        com.Parameters.Add(new SQLiteParameter("@Folder", folder.FolderPath));
                        com.Parameters.Add(new SQLiteParameter("@Present", true.ToString()));

                        com.CommandType = CommandType.Text;

                        lock (lockDB)
                        {
                            SQLiteDataReader r = com.ExecuteReader();
                            while (r.Read())
                                res.Add((string)r["Checksum"]);
                        }
                    }

                    conn.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return res;
        }

        internal List<string> SearchPresentFoldersOfUser(UserClass user)
        {
            List<string> res = new List<string>();

            try
            {
                using (SQLiteConnection conn = SetConnection())
                {
                    conn.Open();
                    using (SQLiteCommand com = conn.CreateCommand())
                    {
                        com.CommandText = @"SELECT FolderPath " +
                                          @"FROM FOLDER " +
                                          @"WHERE User = @User AND " +
                                                @"Present = @Present;";

                        com.Parameters.Add(new SQLiteParameter("@User", user.Username));
                        com.Parameters.Add(new SQLiteParameter("@Present", true.ToString()));

                        com.CommandType = CommandType.Text;

                        lock (lockDB)
                        {
                            SQLiteDataReader r = com.ExecuteReader();
                            while (r.Read())
                                res.Add((string)r["FolderPath"]);
                        }
                    }

                    conn.Close();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
            return res;
        }

        /// <summary>
        /// Controlla se un file esiste (true) o meno (false) nel database ignora il campo Present
        /// TESTATO!
        /// </summary>
        /// <param name="path"></param>
        /// <param name="utente"></param>
        /// <returns></returns>
        internal bool Exists(FileClass file)
        {
            List<string> ris = SearchAllFilesOfUser(file.User);
            return ris.Contains(file.Checksum);
        }

        /// <summary>
        /// Controlla se un file esiste nel DB
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        internal bool Exists(FileClass file, FolderClass folder)
        {
            List<FileClass> ris = SearchFilesInFolderOfUser(file.User, folder);
            return ris.Contains(file);
        }

        internal bool ExistsPresent(FileClass file, FolderClass folder)
        {
            List<FileClass> ris = getFilePresent(file.User);
            return ris.Contains(file);
        }

        /// <summary>
        /// Se l'utente esiste ritorna true altrimenti ritorna false.
        /// TESTATO!
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        internal bool Exists(UserClass user)
        {
            List<string> users = SearchUsers();
            return users.Contains(user.Username);
        }

        /// <summary>
        /// Se il la cartella esiste ritorna true altrimenti ritorna false.
        /// L'oggetto folder deve avere definito:
        /// - FolderPath
        /// - User
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        internal bool Exists(FolderClass folder)
        {
            List<string> folders = SearchPresentFoldersOfUser(folder.User);
            return folders.Contains(folder.FolderPath);
        }

        #endregion

        #region CONNECTION

        private SQLiteConnection SetConnection()
        {
            return new SQLiteConnection("Data Source=Database.db;Version=3;New=False;Compress=True;");
        }

        #endregion
    }
}
