using System;
using System.Collections.Generic;
using System.Text;

namespace WaterStrike
{
    public class GameException : Exception
    {
        public GameException() : base() { }
        public GameException(string message) : base(message) {}
    }
}
