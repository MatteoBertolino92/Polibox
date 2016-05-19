using System;
using System.Collections;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.IO;

namespace Client
{
    class SSLStream
    {
        private string serverCertificateName;
        private string machineName;
        private static Hashtable certificateErrors = new Hashtable();
        private TcpClient client;
        SslStream sslStream;
        private System.IO.BinaryWriter file;


        #region PROPERTIES

        internal string ServerCertificateName
        {
            set { this.serverCertificateName = value; }
            get { return this.serverCertificateName; }
        }

        internal string MachineName
        {
            set { this.machineName = value; }
            get { return this.machineName; }
        }

        #endregion

        internal SSLStream(string ip)
        {
            serverCertificateName = "SignedByCA";
            machineName = ip;
            client = new TcpClient(machineName, 8780);
            sslStream = new SslStream
                (client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
        }

        internal static bool ValidateServerCertificate
            (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {

            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);
            // Do not allow this client to communicate with unauthenticated servers.
            return false;
        }

        internal void connectToTCPServer()
        {
            try
            {
                sslStream.AuthenticateAsClient(serverCertificateName);
            }
            catch (InvalidOperationException)
            {
                try
                {
                    System.Threading.Thread.Sleep(5000);
                    sslStream.AuthenticateAsClient(serverCertificateName);
                }
                catch (Exception)
                {
                    throw;
                }
            }
            catch (Exception)
            {
                //System.Windows.MessageBox.Show(e.ToString());
                throw;
            }
        }

        internal string receiveCommand()
        {
            try
            {
                Byte[] bytes = new Byte[260];
                Byte[] dimB = new Byte[4];

                int offset = 0;
                int MESSAGE_SIZE = 259;
                int bytesRead;
                while (offset < MESSAGE_SIZE)
                {
                    bytesRead = sslStream.Read(bytes, offset, MESSAGE_SIZE - offset);
                    offset += bytesRead;
                }
                for (int i = 0; i < 4; i++)
                {
                    dimB[i] = bytes[i];
                }
                return Encoding.ASCII.GetString(bytes, 4, BitConverter.ToInt32(dimB, 0));
            }
            catch (Exception)
            {
                throw;
            }
        }

        internal void sendCommand(string command)
        {
            try
            {
                Byte[] bytes = new Byte[259];
                StringBuilder padding = new StringBuilder("");
                int paddingLength = 0;

                if (command.Length < 255 - 4)
                {
                    paddingLength = 259 - command.Length - 4;
                    for (int i = command.Length; i < 259; i++)
                    {
                        padding.Append('0');
                    }
                }
                Int32 paddingL = command.Length;
                Buffer.BlockCopy(BitConverter.GetBytes(paddingL), 0, bytes, 0, 4);
                Buffer.BlockCopy(Encoding.ASCII.GetBytes(command + padding.ToString()), 0, bytes, 4, command.Length + paddingLength);
                sslStream.Write(bytes, 0, 259);
            }
            catch (Exception)
            {
                throw;
            }
        }

        internal void sendFile(string path)
        {
            try
            {
                System.IO.BinaryReader file = new System.IO.BinaryReader(new FileStream(path, FileMode.Open));
                byte[] filebyte = new byte[1024];
                sslStream.WriteTimeout = 10000;

                //Send dimension
                this.sendCommand("DIM");
                String ack = null;
                ack = this.receiveCommand();
                Console.WriteLine("Received to DIM: " + ack);
                if (ack != "ACK")
                    throw new Exception("Error sending command");

                long x = file.BaseStream.Length;
                string xx = x.ToString();
                this.sendCommand(xx);
                ack = this.receiveCommand();
                if (ack != "ACK")
                    throw new Exception("Error sending dim");
                Console.WriteLine("Received: " + ack);

                Console.WriteLine("Starting point: " + file.BaseStream.Position.ToString());
                while (file.BaseStream.Position < file.BaseStream.Length)
                {
                    long nbyte = (file.BaseStream.Length - file.BaseStream.Position);

                    if (nbyte >= 1024)
                        nbyte = 1024;

                    this.sendCommand(nbyte.ToString());
                    ack = this.receiveCommand();
                    if (ack != "ACK")
                        throw new Exception("Expecting ack to packet dim");

                    filebyte = file.ReadBytes((int)nbyte);
                    sslStream.Write(filebyte, 0, filebyte.Length);
                    ack = this.receiveCommand();
                }

                this.sendCommand("EOF");
                ack = this.receiveCommand();
                if (ack != "ACK")
                    throw new Exception("FILE NOT CORRECTLY RECEIVED");
                Console.WriteLine("Reaction to EOF: " + ack);
                file.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                sslStream.WriteTimeout = System.Threading.Timeout.Infinite;
                throw;
            }
            finally {
                sslStream.WriteTimeout = System.Threading.Timeout.Infinite;
            }
        }
        
        internal void receiveFile(string filename)
        {
            try
            {
                sslStream.ReadTimeout = 10000;
             
                Byte[] bytes = new byte[1024];
                file = new System.IO.BinaryWriter(new FileStream(filename, FileMode.Create));
                string cmd = this.receiveCommand();
                Console.WriteLine("Received: " + cmd);
                if (cmd != "DIM")
                    throw new Exception("DIM Expected");
                this.sendCommand("ACK");

                cmd = this.receiveCommand();

                long x = Convert.ToInt64(cmd);
                if (x > 10240)
                    GlobalClass.onGoing = true;

                this.sendCommand("ACK");

                int i = 0;
                while (i < x)
                {
                    //Take dim packet
                    String dim = this.receiveCommand();
                    int k = Convert.ToInt32(dim);
                    Byte[] bytes2 = new byte[k];
                    this.sendCommand("ACK");

                    int offset = 0;
                    int MESSAGE_SIZE = k;
                    int bytesRead;
                    while (offset < MESSAGE_SIZE)
                    {
                        bytesRead = sslStream.Read(bytes2, offset, MESSAGE_SIZE - offset);
                        offset += bytesRead;
                    }

                    file.Write(bytes2);
                    this.sendCommand("ACK");
                    i += k;
                }

                cmd = receiveCommand();
                if (cmd != "EOF")
                {
                    Console.WriteLine("Received: " + cmd);
                    throw new Exception("Not EOF");
                }
                file.Close();
                Console.WriteLine("FINE RICEZIONE");
                this.sendCommand("ACK");
            }
            catch (Exception e)
            {
                Console.Write(e.ToString());
                file.Close();
                //Delete file if something goes wrong
                System.IO.File.Delete(filename);
                GlobalClass.onGoing = false;
                sslStream.ReadTimeout = System.Threading.Timeout.Infinite;
                throw; //rilancia al server
            }
            finally {
                GlobalClass.onGoing = false;
                sslStream.ReadTimeout = System.Threading.Timeout.Infinite;
            }
        }

        internal void Close()
        {
            sslStream.Close();
            client.Close();
            sslStream = null;
            client = null;
        }

        internal static bool IsSocketConnected(SSLStream k)
        {
            try
            {
                if (k == null)
                    return false;
                lock(GlobalClass.locktry) {
                    k.sendCommand("TRY");
                }
                return true;
            }

            catch (Exception)
            {
                return false;
            }
        }
    }//Class
}//Namespace

