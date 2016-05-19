using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.IO;

namespace Server_v2
{
    class SSLStream
    {
        private SslStream handler;
        private TcpClient client;
        private System.IO.BinaryWriter file;

        internal SSLStream(TcpClient client)
        {
            try
            {
                // A client has connected. Create the 
                // SslStream using the client's network stream.
                this.client = client;

                handler = new SslStream(client.GetStream(), false);

                handler.AuthenticateAsServer(SSLStreamWaiter.serverCertificate,
                    false, SslProtocols.Tls, true);

                // Display the properties and settings for the authenticated stream.
                //DisplaySecurityLevel(handler);
                //DisplaySecurityServices(handler);
                //DisplayCertificateInformation(handler);
                //DisplayStreamProperties(handler);            
            }

            catch (AuthenticationException e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                }
                Console.WriteLine("Authentication failed - closing the connection.");
                handler.Close();
                client.Close();
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
                handler.Write(bytes, 0, 259);
            }
            catch (Exception)
            {
                throw;
            }
        }

        internal string receiveCommand()
        {
            try
            {
                Byte[] bytes = new Byte[259];
                Byte[] dimB = new Byte[4];

                int offset = 0;
                int MESSAGE_SIZE = 259;
                int bytesRead;
                while (offset < MESSAGE_SIZE)
                {
                    bytesRead = handler.Read(bytes, offset, MESSAGE_SIZE - offset);
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

        internal void sendFile(string path)
        {
            try
            {
                handler.WriteTimeout = 10000;

                System.IO.BinaryReader file = new System.IO.BinaryReader(new FileStream(path, FileMode.Open));
                byte[] filebyte = new byte[1024];

                //Send dimension
                this.sendCommand("DIM");
                String ack = null;
                ack = this.receiveCommand();
                //Console.WriteLine("Received to DIM: " + ack);
                if (ack != "ACK")
                    throw new Exception("Error sending command");


                long x = file.BaseStream.Length;
                string xx = x.ToString();
                //handler.Write(Encoding.ASCII.GetBytes(xx), 0, xx.Length);
                this.sendCommand(xx);

                ack = this.receiveCommand();
                if (ack != "ACK")
                    throw new Exception("Error sending dim");
                //Console.WriteLine("Received: " + ack);

                //Console.WriteLine("Starting point: " + file.BaseStream.Position.ToString());
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
                    handler.Write(filebyte, 0, filebyte.Length);
                    ack = this.receiveCommand();
                }

                this.sendCommand("EOF");
                ack = this.receiveCommand();
                if (ack != "ACK")
                    throw new Exception("FILE NOT CORRECTLY RECEIVED");
                //Console.WriteLine("Reaction to EOF: " + ack);
                file.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                handler.WriteTimeout = System.Threading.Timeout.Infinite;
                throw;
            }
            finally
            {
                handler.WriteTimeout = System.Threading.Timeout.Infinite;
            }
        }

        internal void receiveFile(string filename)
        {
            try
            {
                handler.ReadTimeout = 10000;

                Byte[] bytes = new byte[1024];
                file = new System.IO.BinaryWriter(new FileStream(filename, FileMode.Create));
                string cmd = this.receiveCommand();
                //Console.WriteLine("Received: " + cmd);
                if (cmd != "DIM")
                    throw new Exception("DIM Expected");
                this.sendCommand("ACK");
                //handler.Read(bytes, 0, bytes.Length);
                cmd = this.receiveCommand();
                //cmd = null;
                //cmd = Encoding.ASCII.GetString(bytes);
                long x = Convert.ToInt64(cmd);
             //   System.Windows.MessageBox.Show("Total dimension received: " + x);
                this.sendCommand("ACK");
    
                int i = 0;
                float percentage, prev_percentage = -1;
                while (i < x)
                {
                    percentage = (float)i / x * 100;
                    if ((int)percentage % 10 == 0)
                    {
                        if ((int)percentage != (int)prev_percentage)
                            Console.WriteLine("Percentage:" + (int)percentage);
                        prev_percentage = percentage;
                    }

                    //Take dim packet
                    String dim = this.receiveCommand();
                    int k = Convert.ToInt32(dim);
                    Byte[] bytes2 = new byte[k];
                    this.sendCommand("ACK");

                    //Take data
                    int offset = 0;
                    int MESSAGE_SIZE = k;
                    int bytesRead;
                    while (offset < MESSAGE_SIZE)
                    {
                        bytesRead = handler.Read(bytes2, offset, MESSAGE_SIZE - offset);
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
                Console.WriteLine("Percentuale ricezione: 100");
                file.Close();
                this.sendCommand("ACK");
            }
            catch(System.Net.Sockets.SocketException)
            {
                file.Close();
                System.IO.File.Delete(filename);
                handler.ReadTimeout = System.Threading.Timeout.Infinite;
                throw; //rilancia al server
            }

            catch (Exception e)
            {
                //Console.Write(e.ToString());
                file.Close();
                //Delete file if something goes wrong
                System.IO.File.Delete(filename);
                handler.ReadTimeout = System.Threading.Timeout.Infinite;
                throw; //rilancia al server
            }

            finally
            {
                handler.ReadTimeout = System.Threading.Timeout.Infinite;
            }

        }

        internal void Close()
        {
            handler.Close();
            client.Close();
        }

        //Useful function about SSL
        static void DisplaySecurityLevel(SslStream stream)
        {
            Console.WriteLine("Cipher: {0} strength {1}", stream.CipherAlgorithm, stream.CipherStrength);
            Console.WriteLine("Hash: {0} strength {1}", stream.HashAlgorithm, stream.HashStrength);
            Console.WriteLine("Key exchange: {0} strength {1}", stream.KeyExchangeAlgorithm, stream.KeyExchangeStrength);
            Console.WriteLine("Protocol: {0}", stream.SslProtocol);
        }
        static void DisplaySecurityServices(SslStream stream)
        {
            Console.WriteLine("Is authenticated: {0} as server? {1}", stream.IsAuthenticated, stream.IsServer);
            Console.WriteLine("IsSigned: {0}", stream.IsSigned);
            Console.WriteLine("Is Encrypted: {0}", stream.IsEncrypted);
        }
        static void DisplayStreamProperties(SslStream stream)
        {
            Console.WriteLine("Can read: {0}, write {1}", stream.CanRead, stream.CanWrite);
            Console.WriteLine("Can timeout: {0}", stream.CanTimeout);
        }
        static void DisplayCertificateInformation(SslStream stream)
        {
            Console.WriteLine("Certificate revocation list checked: {0}", stream.CheckCertRevocationStatus);

            X509Certificate localCertificate = stream.LocalCertificate;
            if (stream.LocalCertificate != null)
            {
                Console.WriteLine("Local cert was issued to {0} and is valid from {1} until {2}.",
                    localCertificate.Subject,
                    localCertificate.GetEffectiveDateString(),
                    localCertificate.GetExpirationDateString());
            }
            else
            {
                Console.WriteLine("Local certificate is null.");
            }
            // Display the properties of the client's certificate.
            X509Certificate remoteCertificate = stream.RemoteCertificate;
            if (stream.RemoteCertificate != null)
            {
                Console.WriteLine("Remote cert was issued to {0} and is valid from {1} until {2}.",
                    remoteCertificate.Subject,
                    remoteCertificate.GetEffectiveDateString(),
                    remoteCertificate.GetExpirationDateString());
            }
            else
            {
                Console.WriteLine("Remote certificate is null.");
            }
        }
        internal static void DisplayUsage()
        {
            Console.WriteLine("To start the server specify:");
            Console.WriteLine("serverSync certificateFile.cer");
            Environment.Exit(1);
        }
    }
}

/*internal string receiveCommand()
{
    try
    {
        //Console.WriteLine("Wait a command... ");
        Byte[] bytes = new Byte[1024];
        int bytesRec = handler.Read(bytes, 0, bytes.Length);
        return Encoding.ASCII.GetString(bytes, 0, bytesRec);
    }
    catch (Exception e)
    {
        Console.Write(e.ToString());
        throw;
    }
}
internal void sendCommand(string command)
 {
     try
     {
         handler.Write(System.Text.Encoding.ASCII.GetBytes(command));
     }
     catch (Exception e)
     {
         Console.Write(e.ToString());
         throw;
     }
 }*/