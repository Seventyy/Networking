using shared;
using System;
using System.Collections.Generic;

namespace server
{
    /**
         * The LobbyRoom is a little bit more extensive than the LoginRoom.
         * In this room clients change their 'ready status'.
         * If enough people are ready, they are automatically moved to the GameRoom to play a Game (assuming a game is not already in play).
         */
    class LobbyRoom : Room
    {
        //this list keeps tracks of which players are ready to play a game, this is a subset of the people in this room
        private List<TcpMessageChannel> _readyMembers = new List<TcpMessageChannel>();

        //private Dictionary<TcpMessageChannel, string> _playerNames = new Dictionary<TcpMessageChannel, string>();

        //public Dictionary<TcpMessageChannel, string> PlayerNames
        //{
        //    get { return _playerNames; }
        //}

        public LobbyRoom(TCPGameServer pOwner) : base(pOwner)
        {
        }

        public void AddMember(TcpMessageChannel pChannel, PlayerInfo playerInfo)
        {
            try
            {
                addMember(pChannel, playerInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while adding a member to the lobby: " + ex.Message);
            }
        }

        protected override void addMember(TcpMessageChannel pMember, PlayerInfo playerInfo)
        {
            try
            {
                base.addMember(pMember, playerInfo);

                //tell the member it has joined the lobby
                RoomJoinedEvent roomJoinedEvent = new RoomJoinedEvent();
                roomJoinedEvent.room = RoomJoinedEvent.Room.LOBBY_ROOM;
                pMember.SendMessage(roomJoinedEvent);

                //print some info in the lobby (can be made more applicable to the current member that joined)
                ChatMessage entryMessage = new ChatMessage();
                entryMessage.message = playerInfo.playerName + " has joined the lobby!";

                //safeForEach((TcpMessageChannel member) => member.SendMessage(entryMessage));
                sendToAll(entryMessage);

                //send information to all clients that the lobby count has changed
                sendLobbyUpdateCount();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while adding a member to the lobby: " + ex.Message);
            }
        }

        public void AnnounceWonGame(string pWinnerName)
        {
            try
            {
                ChatMessage entryMessage = new ChatMessage();
                entryMessage.message = pWinnerName + " has won the game!";
                sendToAll(entryMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while announcing the winner of the game: " + ex.Message);
            }
        }

        /**
         * Override removeMember so that our ready count and lobby count is updated (and sent to all clients)
         * anytime we remove a member.
         */
        protected override void removeMember(TcpMessageChannel pMember)
        {
            try
            {
                base.removeMember(pMember);
                _readyMembers.Remove(pMember);

                sendLobbyUpdateCount();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while removing a member from the lobby: " + ex.Message);
            }
        }

        protected override void handleNetworkMessage(ASerializable pMessage, TcpMessageChannel pSender)
        {
            try
            {
                if (pMessage is ChangeReadyStatusRequest) handleReadyNotification(pMessage as ChangeReadyStatusRequest, pSender);
                else if (pMessage is ChatMessage) handleChatMessage(pMessage as ChatMessage, pSender);
                else
                {

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while handling a network message: " + ex.Message);
            }
        }

        private void handleReadyNotification(ChangeReadyStatusRequest pReadyNotification, TcpMessageChannel pSender)
        {
            try
            {
                //if the given client was not marked as ready yet, mark the client as ready
                if (pReadyNotification.ready)
                {
                    if (!_readyMembers.Contains(pSender)) _readyMembers.Add(pSender);
                }
                else //if the client is no longer ready, unmark it as ready
                {
                    _readyMembers.Remove(pSender);
                }

                //do we have enough people for a game and is there no game running yet?
                if (_readyMembers.Count >= 2)
                {
                    KeyValuePair<TcpMessageChannel, PlayerInfo> player1 =
                        new KeyValuePair<TcpMessageChannel, PlayerInfo>(_readyMembers[0], Members[_readyMembers[0]]);
                    KeyValuePair<TcpMessageChannel, PlayerInfo> player2 =
                        new KeyValuePair<TcpMessageChannel, PlayerInfo>(_readyMembers[1], Members[_readyMembers[1]]);
                    removeMember(player1.Key); /// not sure here
                    removeMember(player2.Key);
                    _server.GetNewGameRoom().StartGame(player1, player2);
                }

                //(un)ready-ing / starting a game changes the lobby/ready count so send out an update
                //to all clients still in the lobby
                sendLobbyUpdateCount();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while handling a ready notification: " + ex.Message);
            }
        }

        private void handleChatMessage(ChatMessage pMessage, TcpMessageChannel pSender)
        {
            try
            {
                pMessage.message = Members[pSender].playerName + ": " + pMessage.message;
                //safeForEach((TcpMessageChannel member) => member.SendMessage(pMessage));
                sendToAll(pMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while handling a chat message: " + ex.Message);
            }
        }

        private void sendLobbyUpdateCount()
        {
            try
            {
                LobbyInfoUpdate lobbyInfoMessage = new LobbyInfoUpdate();
                lobbyInfoMessage.memberCount = memberCount;
                lobbyInfoMessage.readyCount = _readyMembers.Count;
                sendToAll(lobbyInfoMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while sending a lobby update count: " + ex.Message);
            }
        }

    }
}
