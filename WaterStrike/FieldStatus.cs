using System;
using System.Collections.Generic;
using System.Text;

namespace WaterStrike
{
    [Flags]
    public enum FieldPoint
    {
        None = 0x0,
        Ship = 0x1,
        Shooted = 0x2
    }
}
