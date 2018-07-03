using System;
using System.Collections.Generic;
using System.Text;

namespace WaterStrike.EventArgs
{
    public class StrokeMakedEventArgs
    {
        public long EnemyId { get; set; }
        public long PlayerId { get; set; }
        public int StrokeResult { get; set; }
    }
}
