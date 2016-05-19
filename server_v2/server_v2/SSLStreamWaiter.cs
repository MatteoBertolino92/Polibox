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
    public sealed class SSLStreamWaiter
    {
        public static X509Certificate serverCertificate = null;
        TcpListener listener;
        TcpClient client;

        // The certificate parameter specifies the name of the file 
        // containing the machine certificate.

        public SSLStreamWaiter(string certificate)
        { //Same of: constructor of CSocket

            serverCertificate = X509Certificate.CreateFromCertFile(certificate);
            // Create a TCP/IP (IPv4) socket and listen for incoming connections.
            listener = new TcpListener(IPAddress.Any, 8780);
            //listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listener.Start();
        }

        public TcpClient waitNewConnection() //Same of: wait new connection
        {
            Console.WriteLine("Waiting for a client to connect...");
            // Application blocks while waiting for an incoming connection.
            // Type CNTL-C to terminate the server.
            client = listener.AcceptTcpClient();
            return client;
        }
    }
}
