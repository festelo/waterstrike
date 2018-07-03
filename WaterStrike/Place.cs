using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace WaterStrike
{
    public class Place
    {
        public static readonly ReadOnlyCollection<char> AvailableColumns = Array.AsReadOnly( new [] { 'А','Б','В','Г','Д','Е','Ж','З','И', 'К' });
        public int Row { get; set; }
        public int ColumnInt { get; set; }
        public char Column { get; set; }

        public Place(string place)
        {
            if (int.TryParse(place[1].ToString(), out var i))
            {
                Row = i - 1;
                ColumnInt = AvailableColumns.IndexOf(place[0]);
                Column = place[0];
            }
            else
            {
                Row = int.Parse(place[0].ToString()) - 1;
                ColumnInt = AvailableColumns.IndexOf(place[1]);
                Column = place[1];
            }
        }
        public override string ToString() => $"{Column}{Row}";
    }
}
