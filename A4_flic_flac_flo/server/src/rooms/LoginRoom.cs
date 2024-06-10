using shared;
using System;

namespace server
{
    /**
         * The LoginRoom is the first room clients 'enter' until the client identifies himself with a PlayerJoinRequest. 
         * If the client sends the wrong type of request, it will be kicked.
         *
         * A connected client that never sends anything will be stuck in here for life,
         * unless the client disconnects (that will be detected in due time).
         */
    class LoginRoom : Room
    {
        //arbitrary max amount just to demo the concept
        private const int MAX_MEMBERS = 50;

        public LoginRoom(TCPGameServer pOwner) : base(pOwner)
        {
        }

        public void AddMember(TcpMessageChannel pChannel)
        {
            try
            {
                addMember(pChannel, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred while adding member: {ex.Message}");
            }
        }

        protected override void addMember(TcpMessageChannel pMember, PlayerInfo playerInfo)
        {
            try
            {
                base.addMember(pMember, playerInfo);

                //notify the client that (s)he is now in the login room, clients can wait for that before doing anything else
                RoomJoinedEvent roomJoinedEvent = new RoomJoinedEvent();
                roomJoinedEvent.room = RoomJoinedEvent.Room.LOGIN_ROOM;
                pMember.SendMessage(roomJoinedEvent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred while adding member: {ex.Message}");
            }
        }

        protected override void handleNetworkMessage(ASerializable pMessage, TcpMessageChannel pSender)
        {
            try
            {
                if (pMessage is PlayerJoinRequest)
                {
                    handlePlayerJoinRequest(pMessage as PlayerJoinRequest, pSender);
                }
                else //if member sends something else than a PlayerJoinRequest
                {
                    Log.LogInfo("Declining client, auth request not understood", this);

                    //don't provide info back to the member on what it is we expect, just close and remove
                    removeAndCloseMember(pSender);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred while handling network message: {ex.Message}");
            }
        }

        /**
         * Tell the client he is accepted and move the client to the lobby room.
         */
        private void handlePlayerJoinRequest(PlayerJoinRequest pMessage, TcpMessageChannel pSender)
        {
            try
            {
                Log.LogInfo("Moving new client to accepted...", this);

                PlayerJoinResponse playerJoinResponse = new PlayerJoinResponse();

                _server.GetLobbyRoom().PingAll();
                _server.GetLobbyRoom().ForceRecheckClients();

                foreach (PlayerInfo playerInfo in _server.GetLobbyRoom().Members.Values)
                {
                    if (playerInfo.playerName == pMessage.name)
                    {
                        playerJoinResponse.result = PlayerJoinResponse.RequestResult.DENIED;
                        pSender.SendMessage(playerJoinResponse);

                        removeMember(pSender);
                        return;
                    }
                }

                playerJoinResponse.result = PlayerJoinResponse.RequestResult.ACCEPTED;
                pSender.SendMessage(playerJoinResponse);

                PlayerInfo newPlayerInfo = new PlayerInfo();
                newPlayerInfo.playerName = pMessage.name;

                removeMember(pSender);
                _server.GetLobbyRoom().AddMember(pSender, newPlayerInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred while handling player join request: {ex.Message}");
            }
        }
    }
}
