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


            Thread.Sleep(1000);
        }
    }

    private void processNewClients()
    {
        while (_listener.Pending())
        {
            Avatar avatar = new Avatar(_nextAvatarId++, new Random().Next(0, 100), GetRandomPosition());
            TcpClient this_client = _listener.AcceptTcpClient();
            _clients.Add(this_client, avatar);

            sendNewPlayerToExistingClients(avatar, this_client);
            sendExisingPlayersToNewClient(this_client);

            Console.WriteLine("Accepted new client.");
        }
    }

    private void sendExisingPlayersToNewClient(TcpClient pClient)
    {
        Packet outPacket = new Packet();
        outPacket.Write("spawn_exising");
        outPacket.Write(_clients.Count);
        foreach (Avatar avatar in _clients.Values)
        {
            outPacket.Write(avatar.id);
            outPacket.Write(avatar.skin_id);
            outPacket.Write(avatar.position.Item1);
            outPacket.Write(avatar.position.Item2);
            outPacket.Write(avatar.position.Item3);
        }
        StreamUtil.Write(pClient.GetStream(), outPacket.GetBytes());
    }

    private void sendNewPlayerToExistingClients(Avatar pAvatar, TcpClient pClient)
    {
        foreach (TcpClient client in _clients.Keys)
        {
            if (client == pClient) continue;
            Packet outPacket = new Packet();
            outPacket.Write("spawn_new");
            outPacket.Write(pAvatar.id);
            outPacket.Write(pAvatar.skin_id);
            outPacket.Write(pAvatar.position.Item1);
            outPacket.Write(pAvatar.position.Item2);
            outPacket.Write(pAvatar.position.Item3);
            StreamUtil.Write(client.GetStream(), outPacket.GetBytes());
        }
    }

    private void processExistingClients()
    {
        foreach (TcpClient client in _clients.Keys)
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
            _clients.Remove(client);
            Console.WriteLine("Removed client.");
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
