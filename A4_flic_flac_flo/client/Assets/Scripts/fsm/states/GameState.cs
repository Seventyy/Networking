using shared;

/**
 * This is where we 'play' a game.
 */
public class GameState : ApplicationStateWithView<GameView>
{
    //just for fun we keep track of how many times a player clicked the board
    //note that in the current application you have no idea whether you are player 1 or 2
    //normally it would be better to maintain this sort of info on the server if it is actually important information
    private int playerId;

    public override void EnterState()
    {
        base.EnterState();
        
        view.gameBoard.OnCellClicked += _onCellClicked;
    }

    private void _onCellClicked(int pCellIndex)
    {
        MakeMoveRequest makeMoveRequest = new MakeMoveRequest();
        makeMoveRequest.move = pCellIndex;

        fsm.channel.SendMessage(makeMoveRequest);
    }

    public override void ExitState()
    {
        base.ExitState();
        view.gameBoard.OnCellClicked -= _onCellClicked;
    }

    private void Update()
    {
        receiveAndProcessNetworkMessages();
    }

    protected override void handleNetworkMessage(ASerializable pMessage)
    {
        if (pMessage is MakeMoveResult) handleMakeMoveResult(pMessage as MakeMoveResult);
        else if (pMessage is GameStartEvent) handleGameStartEvent(pMessage as GameStartEvent);
        else if (pMessage is GameEndEvent) handleGameEndEvent(pMessage as GameEndEvent);
        else if (pMessage is RoomJoinedEvent) handleRoomJoinedEvent(pMessage as RoomJoinedEvent);


    }

    private void handleMakeMoveResult(MakeMoveResult pMakeMoveResult)
    {
        view.gameBoard.SetBoardData(pMakeMoveResult.boardData);


        //some label display
        //if (pMakeMoveResult.whoMadeTheMove == 1)
        //{
        //    player1MoveCount++;
        //    view.playerLabel1.text = $"Player 1 (Movecount: {player1MoveCount})";
        //}
        //if (pMakeMoveResult.whoMadeTheMove == 2)
        //{
        //    player2MoveCount++;
        //    view.playerLabel2.text = $"Player 2 (Movecount: {player2MoveCount})";
        //}
    }

    private void handleGameStartEvent(GameStartEvent pGameStartEvent)
    {
        view.gameBoard.SetBoardData(new TicTacToeBoardData());

        if (pGameStartEvent.playerId == 1)
        {
            view.playerLabel1.text = $"Player 1 (you): " + pGameStartEvent.playerName;
            view.playerLabel2.text = $"Player 2: " + pGameStartEvent.opponentName;
        }
        else if (pGameStartEvent.playerId == 2)
        {
            view.playerLabel1.text = $"Player 1: " + pGameStartEvent.opponentName;
            view.playerLabel2.text = $"Player 2 (you): " + pGameStartEvent.playerName;
        }
    }

    private void handleGameEndEvent(GameEndEvent pGameEndEvent)
    {
        if (pGameEndEvent.whoWon == 1)
        {
            view.playerLabel1.text = $"Player 1 (you) won!";
            view.playerLabel2.text = $"Player 2 lost!";
        }
        if (pGameEndEvent.whoWon == 2)
        {
            view.playerLabel1.text = $"Player 1 lost!";
            view.playerLabel2.text = $"Player 2 (you) won!";
        }
        if (pGameEndEvent.whoWon == 0)
        {
            view.playerLabel1.text = $"It's a draw!";
            view.playerLabel2.text = $"It's a draw!";
        }
    }
    private void handleRoomJoinedEvent(RoomJoinedEvent pMessage)
    {
        if (pMessage.room == RoomJoinedEvent.Room.LOBBY_ROOM)
        {
            fsm.ChangeState<LobbyState>();
        }
    }

}
