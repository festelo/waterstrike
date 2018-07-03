namespace WaterStrike.EventArgs
{
    public class PlayerReadyEventArgs : System.EventArgs
    {
        public bool IsEnemyReady { get; set; }
        public long PlayerId { get; set; }
    }
}
