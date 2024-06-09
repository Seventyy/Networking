using shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace server
{
    /**
             * This room runs a single Game (at a time). 
             * 
             * The 'Game' is very simple at the moment:
             *	- all client moves are broadcasted to all clients
             *	
             * The game has no end yet (that is up to you), in other words:
             * all players that are added to this room, stay in here indefinitely.
             */
    class GameRoom : Room
    {
        public bool IsGameInPlay { get; private set; }

        //wraps the board to play on...
        private TicTacToeBoard _board;

        public GameRoom(TCPGameServer pOwner) : base(pOwner)
        {
            _board = new TicTacToeBoard();
        }

        public void StartGame(KeyValuePair<TcpMessageChannel, PlayerInfo> pPlayer1, KeyValuePair<TcpMessageChannel, PlayerInfo> pPlayer2)
        {
            if (IsGameInPlay) throw new Exception("Programmer error duuuude.");

            IsGameInPlay = true;
            addMember(pPlayer1.Key, pPlayer1.Value);
            addMember(pPlayer2.Key, pPlayer2.Value);

            GameStartEvent player1GameStartData = new GameStartEvent();
            player1GameStartData.playerId = 1;
            player1GameStartData.playerName = pPlayer1.Value.playerName;
            player1GameStartData.opponentName = pPlayer2.Value.playerName;
            pPlayer1.Key.SendMessage(player1GameStartData);

            GameStartEvent player2GameStartData = new GameStartEvent();
            player2GameStartData.playerId = 2;
            player2GameStartData.playerName = pPlayer2.Value.playerName;
            player2GameStartData.opponentName = pPlayer1.Value.playerName;
            pPlayer2.Key.SendMessage(player2GameStartData);
        }

        protected override void addMember(TcpMessageChannel pMember, PlayerInfo playerInfo)
        {
            try
            {
                base.addMember(pMember, playerInfo);

                //notify client he has joined a game room 
                RoomJoinedEvent roomJoinedEvent = new RoomJoinedEvent();
                roomJoinedEvent.room = RoomJoinedEvent.Room.GAME_ROOM;
                pMember.SendMessage(roomJoinedEvent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while adding a member: " + ex.Message, this);
            }
        }

        public override void Update()
        {
            try
            {
                //demo of how we can tell people have left the game...
                int oldMemberCount = memberCount;
                base.Update();
                int newMemberCount = memberCount;

                if (oldMemberCount != newMemberCount)
                {
                    Log.LogInfo("People left the game...", this);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while updating the game: " + ex.Message, this);
            }
        }

        protected override void handleNetworkMessage(ASerializable pMessage, TcpMessageChannel pSender)
        {
            try
            {
                if (pMessage is MakeMoveRequest) handleMakeMoveRequest(pMessage as MakeMoveRequest, pSender);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while handling a network message: " + ex.Message, this);
            }
        }

        private void handleMakeMoveRequest(MakeMoveRequest pMessage, TcpMessageChannel pSender)
        {
            try
            {
                //we have two players, so index of sender is 0 or 1, which means playerID becomes 1 or 2
                int playerID = indexOfMember(pSender) + 1;
                //make the requested move (0-8) on the board for the player
                _board.MakeMove(pMessage.move, playerID);

                //and send the result of the boardstate back to all clients
                MakeMoveResult makeMoveResult = new MakeMoveResult();
                makeMoveResult.whoMadeTheMove = playerID;
                makeMoveResult.boardData = _board.GetBoardData();
                sendToAll(makeMoveResult);

                int winner = _board.GetBoardData().WhoHasWon();

                if (winner != 0)
                {
                    GameEndEvent gameEndEvent = new GameEndEvent();
                    gameEndEvent.whoWon = winner;
                    sendToAll(gameEndEvent);

                    IsGameInPlay = false;

                    KeyValuePair<TcpMessageChannel, PlayerInfo> player1 =
                        new KeyValuePair<TcpMessageChannel, PlayerInfo>(Members.Keys.ElementAt(0), Members[Members.Keys.ElementAt(0)]);
                    KeyValuePair<TcpMessageChannel, PlayerInfo> player2 =
                        new KeyValuePair<TcpMessageChannel, PlayerInfo>(Members.Keys.ElementAt(1), Members[Members.Keys.ElementAt(1)]);

                    removeMember(player1.Key);
                    removeMember(player2.Key);

                    _server.GetLobbyRoom().AddMember(player1.Key, player1.Value);
                    _server.GetLobbyRoom().AddMember(player2.Key, player2.Value);

                    _server.GetLobbyRoom().AnnounceWonGame(winner == 1 ? player1.Value.playerName : player2.Value.playerName);

                    _server.RemoveGameRoom(this);

                    Log.LogInfo("Game ended, players moved back to lobby", this);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while handling MakeMoveRequest: " + ex.Message, this);
            }
        }
    }
}
