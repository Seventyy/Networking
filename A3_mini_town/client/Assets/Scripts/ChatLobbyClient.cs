using shared;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;

/**
 * The main ChatLobbyClient where you will have to do most of your work.
 * 
 * @author J.C. Wichman
 */
public class ChatLobbyClient : MonoBehaviour
{
    //reference to the helper class that hides all the avatar management behind a blackbox
    private AvatarAreaManager _avatarAreaManager;
    //reference to the helper class that wraps the chat interface
    private PanelWrapper _panelWrapper;

    [SerializeField] private string _server = "localhost";
    [SerializeField] private int _port = 55555;

    private TcpClient _client;

    private void Start()
    {
        connectToServer();

        //register for the important events
        _avatarAreaManager = FindObjectOfType<AvatarAreaManager>();
        _avatarAreaManager.OnAvatarAreaClicked += onAvatarAreaClicked;

        _panelWrapper = FindObjectOfType<PanelWrapper>();
        _panelWrapper.OnChatTextEntered += onChatTextEntered;
    }

    private void connectToServer()
    {
        try
        {
            _client = new TcpClient();
            _client.Connect(_server, _port);
            Debug.Log("Connected to server.");
        }
        catch (Exception e)
        {
            Debug.Log("Could not connect to server:");
            Debug.Log(e.Message);
        }
    }

    private void onAvatarAreaClicked(Vector3 pClickPosition)
    {
        Debug.Log("ChatLobbyClient: you clicked on " + pClickPosition);
        //TODO pass data to the server so that the server can send a position update to all clients (if the position is valid!!)
    }

    private void onChatTextEntered(string pText)
    {
        _panelWrapper.ClearInput();
        sendMessage(pText);
    }

    private void sendMessage(string message)
    {
        try
        {
            Debug.Log("Sending:" + message);

            Packet outPacket = new Packet();
            outPacket.Write("send_message");
            outPacket.Write(message);
            sendPacket(outPacket);
        }
        catch (Exception e)
        {
            //for quicker testing, we reconnect if something goes wrong.
            Debug.Log(e.Message);
            _client.Close();
            connectToServer();
        }
    }

    // RECEIVING CODE

    private void Update()
    {
        try
        {
            if (_client.Available > 0)
            {
                //get a packet
                byte[] inBytes = StreamUtil.Read(_client.GetStream());
                if (inBytes.Length > 0)
                {
                    Packet inPacket = new Packet(inBytes);

                    //get the command
                    string command = inPacket.ReadString();
                    Debug.Log("Received command:" + command);

                    //process it
                    if (command == "get_message")
                    {
                        showMessage(inPacket.ReadInt(), inPacket.ReadString());
                    }
                    else if (command == "spawn_new")
                    {
                        AvatarView avatarView = _avatarAreaManager.AddAvatarView(inPacket.ReadInt());
                        avatarView.SetSkin(inPacket.ReadInt());
                        avatarView.transform.localPosition = new Vector3(
                            (float)inPacket.ReadDouble(),
                            (float)inPacket.ReadDouble(),
                            (float)inPacket.ReadDouble()
                        );
                    }
                    else if (command == "spawn_exising")
                    {
                        int count = inPacket.ReadInt();

                        for (int i = 0; i < count; i++)
                        {
                            AvatarView avatarView = _avatarAreaManager.AddAvatarView(inPacket.ReadInt());
                            avatarView.SetSkin(inPacket.ReadInt());
                            avatarView.transform.localPosition = new Vector3(
                                (float)inPacket.ReadDouble(),
                                (float)inPacket.ReadDouble(),
                                (float)inPacket.ReadDouble()
                            );
                        }


                    }
                }
            }
        }
        catch (Exception e)
        {
            //for quicker testing, we reconnect if something goes wrong.
            Debug.Log(e.Message);
            _client.Close();
            connectToServer();
        }
    }

    private void showMessage(int id, string message)
    {
        AvatarView avatarView = _avatarAreaManager.GetAvatarView(id);
        avatarView.Say(message);
    }

    private void sendPacket(Packet pOutPacket)
    {
        try
        {
            StreamUtil.Write(_client.GetStream(), pOutPacket.GetBytes());
        }

        catch (Exception e)
        {
            //for quicker testing, we reconnect if something goes wrong.
            Debug.Log(e.Message);
            _client.Close();
            connectToServer();
        }
    }
}
