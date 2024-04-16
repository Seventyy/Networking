using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using shared;
using System.Threading;
using System.Linq;
using System.Text;
using System.Diagnostics;

/**
 * This class implements a simple tcp echo server.
 * Read carefully through the comments below.
 * Note that the server does not contain any sort of error handling.
 */

class Avatar
{
    public int id;
    public int skin_id;
    public (double, double, double) position;

    public Avatar(int id = 0, int skin_id = 0, (double, double, double) position = default)
    {
        this.id = id;
        this.skin_id = skin_id;
        this.position = position;
    }
}

class TCPServerSample
{
    public static void Main(string[] args)
    {
        TCPServerSample server = new TCPServerSample();
        server.run();
    }

    private TcpListener _listener;
    private Dictionary<TcpClient, Avatar> _clients = new Dictionary<TcpClient, Avatar>();
    private int _nextAvatarId = 1;

    private void run()
    {
        Console.WriteLine("Server started on port 55555");

        _listener = new TcpListener(IPAddress.Any, 55555);
        _listener.Start();

        while (true)
        {
            processNewClients();
            processExistingClients();
            processDisconectedClients();


            Thread.Sleep(100);
        }
    }

    private void processNewClients()
    {
        try
        {
            while (_listener.Pending())
            {
                try
                {
                    Avatar avatar = new Avatar(_nextAvatarId++, new Random().Next(0, 100), GetRandomPosition());
                    TcpClient this_client = _listener.AcceptTcpClient();
                    _clients.Add(this_client, avatar);


                    Console.WriteLine("Accepted new client.");
                }
                catch
                {
                    Console.WriteLine("Client cannot connect.");
                }
            }
        }
        catch
        {
            Console.WriteLine("_listener.Pending() failed.");
        }
    }

    private void sendNewPlayerToExistingClients()
    {
        foreach (TcpClient client in _clients.Keys)
        {
            foreach (TcpClient otherClient in _clients.Keys)
            {
                if (otherClient == client) continue;

                Packet outPacket = new Packet();
                outPacket.Write("create_avatar");
                outPacket.Write(_clients[otherClient].id);
                outPacket.Write(_clients[otherClient].skin_id);
                outPacket.Write(_clients[otherClient].position.Item1);
                outPacket.Write(_clients[otherClient].position.Item2);
                outPacket.Write(_clients[otherClient].position.Item3);
                StreamUtil.Write(client.GetStream(), outPacket.GetBytes());
            }
        }
    }

    private void sendExistingPlayersToNewClient(TcpClient pClient)
    {
        foreach (TcpClient otherClient in _clients.Keys)
        {
            Packet outPacket = new Packet();
            outPacket.Write("create_avatar");
            outPacket.Write(_clients[otherClient].id);
            outPacket.Write(_clients[otherClient].skin_id);
            outPacket.Write(_clients[otherClient].position.Item1);
            outPacket.Write(_clients[otherClient].position.Item2);
            outPacket.Write(_clients[otherClient].position.Item3);
            StreamUtil.Write(pClient.GetStream(), outPacket.GetBytes());
        }
    }

    

    private void processExistingClients()
    {
        foreach (TcpClient client in _clients.Keys)
        {
            try
            {
                if (client.Available == 0) continue;

                //get a packet
                byte[] inBytes = StreamUtil.Read(client.GetStream());
                Packet inPacket = new Packet(inBytes);

                //get the command
                string command = inPacket.ReadString();
                Console.WriteLine("Received command:" + command);

                //process it
                if (command == "send_message")
                {
                    handleSendMessage(client, inPacket);
                }

            }
            catch
            {
                Console.WriteLine("process Client failed");

            }
        }
    }

    private void handleSendMessage(TcpClient pClient, Packet pInPacket)
    {
        string message = pInPacket.ReadString();

        foreach (TcpClient client in _clients.Keys)
        {
            Packet outPacket = new Packet();
            outPacket.Write("get_message");
            outPacket.Write(_clients[pClient].id);
            outPacket.Write(message);
            StreamUtil.Write(client.GetStream(), outPacket.GetBytes());
        }
    }

    private void processDisconectedClients()
    {
        try
        {
            List<TcpClient> disconnectedClients = new List<TcpClient>();
            foreach (TcpClient client in _clients.Keys)
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
                foreach (TcpClient otherClient in _clients.Keys)
                {
                    if (otherClient == client) continue;
                    Packet outPacket = new Packet();
                    outPacket.Write("destroy_avatar");
                    outPacket.Write(_clients[client].id);
                    StreamUtil.Write(otherClient.GetStream(), outPacket.GetBytes());
                }

                _clients.Remove(client);
                Console.WriteLine("Removed client.");
            }
        }
        catch
        {
            Console.WriteLine("processDisconectedClients");

        }
    }

    private (double, double, double) GetRandomPosition()
    {
        //set a random position
        double randomAngle = new Random().NextDouble() * Math.PI;
        double randomDistance = new Random().NextDouble() * 10;
        return (Math.Cos(randomAngle) * randomDistance, 0, Math.Sin(randomAngle) * randomDistance);
    }
}
