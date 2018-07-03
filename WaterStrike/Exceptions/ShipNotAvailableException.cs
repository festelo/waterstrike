using System;
using System.Collections.Generic;
using System.Text;

namespace WaterStrike.Exceptions
{
    public class ShipNotAvailableException : GameException
    {
        public ShipNotAvailableException() : base() { }
        public ShipNotAvailableException(string message) : base(message) { }
    }
}
