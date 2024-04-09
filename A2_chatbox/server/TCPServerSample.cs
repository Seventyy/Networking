using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using shared;
using System.Threading;
using System.Text;
using System.Linq;

class TCPServerSample
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Server started on port 55555");

        TcpListener listener = new TcpListener(IPAddress.Any, 55555);
        listener.Start();

        Dictionary<TcpClient, string> clients = new Dictionary<TcpClient, string>();

        while (true)
        {
            while (listener.Pending())
            {
                clients.Add(listener.AcceptTcpClient(), "Guest" + (clients.Count + 1).ToString());
                foreach (TcpClient receiver in clients.Keys)
                {
                    string dataString;

                    if (receiver == clients.Last().Key)
                        dataString = "You joined the server as: " + clients.Last().Value + ".";
                    else
                        dataString = clients.Last().Value + " joined the server.";

                    StreamUtil.Write(receiver.GetStream(), Encoding.UTF8.GetBytes(dataString));
                }
            }

            foreach (TcpClient sender in clients.Keys)
            {
                if (sender.Available == 0) continue;

                byte[] data = StreamUtil.Read(sender.GetStream());
                string dataString = clients[sender] + ": " + Encoding.UTF8.GetString(data);

                foreach (TcpClient receiver in clients.Keys)
                {
                    StreamUtil.Write(receiver.GetStream(), Encoding.UTF8.GetBytes(dataString));
                }
            }

            Thread.Sleep(100);
        }
    }

    
}


