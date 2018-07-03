using System;
using System.Collections.Generic;
using System.Text;

namespace WaterStrike.Exceptions
{
    public class PlayerStatusException : GameException
    {
        public PlayerStatusException() : base() { }
        public PlayerStatusException(string message) : base(message) { }
    }
}
