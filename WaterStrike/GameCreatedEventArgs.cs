using System;
using System.Collections.Generic;
using System.Text;

namespace WaterStrike
{
    public class GameCreatedEventArgs : EventArgs
    {
        public Player FirstPlayer { get; set; }
        public Player SecondPlayer { get; set; }
    }
}
