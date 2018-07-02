using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace WaterStrike
{
    public class Place
    {
        public static readonly ReadOnlyCollection<char> AvailableColumns = Array.AsReadOnly( new [] { 'А','Б','В','Г','Д','Е','Ж','З','И' });
        public int Row { get; set; }
        public int ColumnInt { get; set; }
        public char Column { get; set; }

        public Place(string place)
        {
            ColumnInt = AvailableColumns.IndexOf(place[0]);
            Row = int.Parse(place[1].ToString())-1;
            Column = place[0];
        }
        public override string ToString() => $"{Column}{Row}";
    }
}
