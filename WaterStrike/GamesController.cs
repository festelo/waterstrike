using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Timers;

namespace WaterStrike
{
    public class GamesController
    {
        private readonly Dictionary<long,Player> players = new Dictionary<long, Player>();
        private readonly List<long> searchQuery = new List<long>();

        public event EventHandler<GameCreatedEventArgs> GameCreated;

        private readonly Timer timer;
        public GamesController()
        {
            timer = new Timer(1000);
            timer.Elapsed += TimerElapsed;
            timer.Start();
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            bool CheckPlayer(long id, out Player player)
            {
                searchQuery.Remove(id);
                if (players.ContainsKey(id))
                {
                    player = null;
                    return false;
                }
                player = new Player(id);
                players.Add(id, player);
                return true;
            }
            for (var i = 0; i + 2 <= searchQuery.Count; i+=2)
            {
                var first = searchQuery[i];
                var second = searchQuery[i + 1];
                if (CheckPlayer(first, out var firstplayer) && CheckPlayer(second, out var secondplayer))
                {
                    firstplayer.Enemy = secondplayer;
                    secondplayer.Enemy = firstplayer;
                    GameCreated?.Invoke(this, new GameCreatedEventArgs() { FirstPlayer = firstplayer, SecondPlayer = secondplayer});
                }
            }
        }

        public bool TryToAddToSearch(long playerid)
        {
            if (players.ContainsKey(playerid))
            {
                return false;
            }
            searchQuery.Add(playerid);
            return true;
        }
        public bool TryToSetShip(long playerid, Place[] coordinates)
        {
            if (!players.ContainsKey(playerid) || players[playerid].IsReady)
            {
                return false;
            }
            if(coordinates.Length == 0) return false;
            var row = coordinates[0].Row;
            var column = coordinates[0].Column;
            foreach (var place in coordinates)
            {
                if (place.Row != row && place.Column != column)
                {
                    return false;
                }
            }
            foreach (var place in coordinates)
            {
                players[playerid].Field[place.Row, place.ColumnInt] = FieldPoint.Ship;
            }
            searchQuery.Add(playerid);
            return true;
        }
        public bool TryToGetField(long playerid, out FieldPoint[,] field)
        {
            if (!players.ContainsKey(playerid))
            {
                field = null;
                return false;
            }
            field = players[playerid].Field;
            return true;
        }
    }
}
