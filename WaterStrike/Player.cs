using System;
using System.Collections.Generic;
using System.Text;

namespace WaterStrike
{
    public class Player
    {
        public Player(long id)
        {
            Id = id;
        }
        public bool IsReady { get; set; }
        public bool IsPlayerStroke { get; set; }
        public long Id { get; set; }
        public Player Enemy { get; set; }
        public Dictionary<int, int> ShipsAvailableCount = new Dictionary<int, int>()
        {
            {4, 1},
            {3, 2},
            {2, 3},
            {1, 4},
        };

        /// <summary>
        /// 0 - empty, 1 - shooted, 2 -
        /// </summary>
        public FieldPoint[,] Field { get; } = new FieldPoint[10,10];
    }
}
