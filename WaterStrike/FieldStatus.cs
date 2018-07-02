using System;
using System.Collections.Generic;
using System.Text;

namespace WaterStrike
{
    [Flags]
    public enum FieldPoint
    {
        None = 0,
        Ship = 2,
        Shooted = 4
    }
}
