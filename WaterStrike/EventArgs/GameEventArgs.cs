namespace WaterStrike.EventArgs
{
    public class GameEventArgs : System.EventArgs
    {
        public long FirstPlayerId { get; set; }
        public long SecondPlayerId { get; set; }
    }
}
