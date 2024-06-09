namespace shared
{
    public class GameStartEvent : ASerializable
    {
        public int playerId;
        public string playerName;
        public string opponentName;

        public override void Serialize(Packet pPacket)
        {
            pPacket.Write(playerId);
            pPacket.Write(playerName);
            pPacket.Write(opponentName);
        }

        public override void Deserialize(Packet pPacket)
        {
            playerId = pPacket.ReadInt();
            playerName = pPacket.ReadString();
            opponentName = pPacket.ReadString();
        }
    }
}
