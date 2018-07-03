using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Timers;
using WaterStrike.EventArgs;
using WaterStrike.Exceptions;

namespace WaterStrike
{
    public class GamesController
    {
        private readonly Dictionary<long,Player> players = new Dictionary<long, Player>();
        private readonly List<long> searchQuery = new List<long>();

        public event EventHandler<GameEventArgs> GameCreated;
        public event EventHandler<GameEventArgs> GameStarted;
        public event EventHandler<GameEventArgs> GameEnded;
        public event EventHandler<PlayerReadyEventArgs> PlayerReady;
        public event EventHandler<StrokeMakedEventArgs> StrokeMaked;

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
                    GameCreated?.Invoke(this, new GameEventArgs() { FirstPlayerId = firstplayer.Id, SecondPlayerId = secondplayer.Id});
                }
            }
        }

        public bool IsInGame(long playerid)
        {
            return players.ContainsKey(playerid);
        }
        public bool IsReady(long playerid)
        {
            if (!players.ContainsKey(playerid))
            {
                throw new PlayerStatusException("User not in game");
            }
            return players[playerid].IsReady;
        }
        public bool IsPlayerStroke(long playerid)
        {
            if (!players.ContainsKey(playerid) && !players[playerid].IsReady)
            {
                throw new PlayerStatusException("User not in game or he's not ready");
            }
            return players[playerid].IsPlayerStroke;
        }

        public void AddToSearch(long playerid)
        {
            if (players.ContainsKey(playerid))
            {
                throw new PlayerStatusException("User already in game");
            }
            searchQuery.Add(playerid);
        }

        private bool CanPlaceShip(FieldPoint[,] fields, Place place)
        {
            bool CheckCoord(int coord) => !(coord < 0 || coord >= 10);
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    var row = place.Row + i;
                    var column = place.ColumnInt + j;
                    if (!CheckCoord(row) || !CheckCoord(column)) continue;
                    if (fields[row, column].HasFlag(FieldPoint.Ship))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void SetShip(long playerid, Place[] coordinates)
        {
            if (!players.ContainsKey(playerid) || players[playerid].IsReady)
            {
                throw new PlayerStatusException("User not in game or he's ready");
            }
            var field = players[playerid].Field;
            if (coordinates.Length == 0) throw new ArgumentException("Coordinates length must be more then 1");
            if (coordinates.Length != 1)
            {
                bool isrow = false;
                var row = coordinates[0].Row;
                var column = coordinates[0].ColumnInt;
                List<Place> sorted = null;
                if (row == coordinates[1].Row)
                {
                    isrow = true;
                    sorted = coordinates.OrderBy(c => c.Row).ToList();
                }
                else if (column == coordinates[1].ColumnInt)
                {
                    sorted = coordinates.OrderBy(c => c.ColumnInt).ToList();
                }
                else
                {
                    throw new ArgumentException("Incorrect coordinates");
                }
                for (var i = 1; i < sorted.Count; i++)
                {
                    if (sorted[i].Row != row && sorted[i].ColumnInt != column)
                    {
                        throw new ArgumentException("Incorrect coordinates");
                    }
                    if ((isrow && sorted[i].ColumnInt - sorted[i - 1].ColumnInt != 1)
                        || (!isrow && sorted[i].Row - sorted[i - 1].Row != 1))
                    {
                        throw new ArgumentException("Incorrect coordinates");
                    }
                    if (!CanPlaceShip(field, sorted[i])) throw new ArgumentException("Can't place ship here");
                }
            }
            else
            {
                if (!CanPlaceShip(field, coordinates[0])) throw new ArgumentException("Can't place ship here");
            }
            if (coordinates.Length > 4)
            {
                throw new ArgumentException("Ship is too large");
            }
            if (players[playerid].ShipsAvailableCount[coordinates.Length] == 0) throw new ShipNotAvailableException("This type of ship isn't available");
            players[playerid].ShipsAvailableCount[coordinates.Length]--;
            foreach (var place in coordinates)
            {
                field[place.Row, place.ColumnInt] = FieldPoint.Ship;
            }
            if (players[playerid].ShipsAvailableCount.All(s => s.Value == 0))
            {
                players[playerid].IsReady = true;
                var enemyready = players[playerid].Enemy.IsReady;
                PlayerReady?.Invoke(this, new PlayerReadyEventArgs()
                {
                    IsEnemyReady = enemyready,
                    PlayerId = playerid
                });
                if (enemyready)
                {
                    GameStarted?.Invoke(this,
                        new GameEventArgs() {FirstPlayerId = players[playerid].Enemy.Id, SecondPlayerId = playerid});
                }
                else
                {
                    players[playerid].IsPlayerStroke = true;
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="playerid"></param>
        /// <param name="place"></param>
        /// <returns>0 - miss, 1 - ship injure, 2 - ship drown</returns>
        public int Shoot(long playerid, Place place)
        {
            if (!players.ContainsKey(playerid))
            {
                throw new PlayerStatusException("User not in game");
            }
            if (!players[playerid].IsPlayerStroke) throw new PlayerStatusException("Not the user stroke");
            if (players[playerid].Enemy.Field[place.Row, place.ColumnInt].HasFlag(FieldPoint.Shooted)) throw new ArgumentException("Repeated stroke");
            var field = players[playerid].Enemy.Field[place.Row, place.ColumnInt] |= FieldPoint.Shooted;
            var res = 1;
            if (!field.HasFlag(FieldPoint.Ship))
            {
                players[playerid].Enemy.IsPlayerStroke = true;
                players[playerid].IsPlayerStroke = false;
                res = 0;
            }
            StrokeMaked?.Invoke(this, new StrokeMakedEventArgs()
            {
                EnemyId = players[playerid].Enemy.Id,
                PlayerId = playerid,
                StrokeResult = res
            });
            return res;
        }
        public FieldPoint[,] GetField(long playerid)
        {
            if (!players.ContainsKey(playerid))
            {
                throw new PlayerStatusException("User not in game");
            }
            return players[playerid].Field;
        }
        public FieldPoint[,] GetHiddenEnemyField(long playerid)
        {
            if (!players.ContainsKey(playerid))
            {
                throw new PlayerStatusException("User not in game");
            }
            var field = players[playerid].Enemy.Field;
            var newfield = new FieldPoint[10, 10];
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    if (field[i, j].HasFlag(FieldPoint.Ship) && !field[i, j].HasFlag(FieldPoint.Shooted))
                    {
                        newfield[i, j] = FieldPoint.None;
                    }
                    else
                    {
                        newfield[i, j] = field[i, j];
                    }
                }
            }
            return newfield;
        }
        public Dictionary<int, int> GetAvailableShipsCount(long playerid)
        {
            if (!players.ContainsKey(playerid))
            {
                throw new PlayerStatusException("User not in game");
            }
            return players[playerid].ShipsAvailableCount;
        }
        public void EndGame(long playerid)
        {
            if (!players.ContainsKey(playerid))
            {
                throw new PlayerStatusException("User not in game");
            }
            var enemyid = players[playerid].Enemy.Id;
            players.Remove(playerid);
            players.Remove(enemyid);
            GameEnded?.Invoke(this, new GameEventArgs() { FirstPlayerId = playerid, SecondPlayerId = enemyid});
        }
    }
}
