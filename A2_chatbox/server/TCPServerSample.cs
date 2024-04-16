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
            try
            {
                while (listener.Pending())
                {
                    try
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
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }


            try
            {
                foreach (TcpClient sender in clients.Keys)
                {
                    try
                    {
                        if (sender.Available == 0) continue;

                        byte[] data = StreamUtil.Read(sender.GetStream());
                        string dataString = clients[sender] + ": " + Encoding.UTF8.GetString(data);

                        foreach (TcpClient receiver in clients.Keys)
                        {
                            try
                            {
                                StreamUtil.Write(receiver.GetStream(), Encoding.UTF8.GetBytes(dataString));
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }


            try
            {
                List<TcpClient> disconnectedClients = new List<TcpClient>();
                foreach (TcpClient client in clients.Keys)
                {
                    try
                    {
                        StreamUtil.Write(client.GetStream(), new byte[0]);
                    }
                    catch
                    {
                        disconnectedClients.Add(client);
                    }
                }
                foreach (TcpClient client in disconnectedClients)
                {
                    clients.Remove(client);
                    if (clients.Count == 0)
                        clients = new Dictionary<TcpClient, string>();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }



            Thread.Sleep(100);
        }
    }


}


