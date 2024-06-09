namespace shared
{
    public class GameEndEvent: ASerializable
    {
        public int whoWon;

        public override void Serialize(Packet pPacket)
        {
            pPacket.Write(whoWon);
        }

        public override void Deserialize(Packet pPacket)
        {
            whoWon = pPacket.ReadInt();
        }
    }
}
