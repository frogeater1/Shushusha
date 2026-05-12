using System.Collections.Concurrent;
using System.Net.Sockets;

namespace ShushushaServer;

public class Room
{
    public const int MaxPlayers = 12;
    private const int MinPlayersToStart = 3;
    private const int InitialIndicatorCount = 3;

    public int RoomId;
    public RoomState State = RoomState.Waiting;
    public int CurrentRound;
    public int CurrentFloor = 1;
    public int TargetFloor;
    public int Magic;
    public GameStage Stage = GameStage.None;
    public DateTime StageEndTimeUtc = DateTime.MaxValue;
    private readonly List<ServerIndicator> initialIndicators;
    public List<ServerIndicator> Indicators;
    public Player? Mouse;
    public Player? SharkKing;
    public TcpClient?[] clients = new TcpClient[MaxPlayers];
    public Player?[] players = new Player[MaxPlayers];


    public Room(create_room_c2s msg, TcpClient client1, int roomId)
    {
        RoomId = roomId;
        initialIndicators = CreateInitialIndicators(InitialIndicatorCount);
        Indicators = CloneIndicators(initialIndicators);
        clients[0] = client1;
        players[0] = CreateRoomPlayer(msg.Player.Uid, 0);
    }

    public ResCode TryJoin(join_room_c2s msg, TcpClient client, out Player joinedPlayer, out List<TcpClient> notifyClients)
    {
        lock (this)
        {
            notifyClients = [];
            if (State != RoomState.Waiting)
            {
                joinedPlayer = null!;
                return ResCode.GameAlreadyStarted;
            }

            for (var i = 0; i < clients.Length; i++)
            {
                if (clients[i] != null)
                {
                    continue;
                }

                clients[i] = client;
                joinedPlayer = CreateRoomPlayer(msg.Player.Uid, i);
                players[i] = joinedPlayer;
                var joinedIdInRoom = joinedPlayer.IdInRoom;
                notifyClients = clients
                    .Where((x, idx) => x != null && idx != joinedIdInRoom)
                    .Select(x => x!)
                    .ToList();
                return ResCode.Success;
            }

            joinedPlayer = null!;
            return ResCode.RoomIsFull;
        }
    }

    public ResCode Ready(int idInRoom, bool isReady, out Player readyPlayer)
    {
        lock (this)
        {
            readyPlayer = players[idInRoom]!;
            if (State != RoomState.Waiting)
            {
                return ResCode.InvalidRoomState;
            }

            readyPlayer.Ready = !isReady;
            return ResCode.Success;
        }
    }

    public ResCode TryStartGame(int idInRoom, out GameStartResult result)
    {
        lock (this)
        {
            result = new GameStartResult();

            if (idInRoom != 0)
            {
                return ResCode.NotRoomOwner;
            }

            if (State != RoomState.Waiting)
            {
                return ResCode.GameAlreadyStarted;
            }

            var activePlayers = players.OfType<Player>().ToList();
            if (activePlayers.Count < MinPlayersToStart)
            {
                return ResCode.NotEnoughPlayers;
            }

            if (activePlayers.Any(player => !player.Ready))
            {
                return ResCode.NotAllPlayersReady;
            }

            var mouseIndex = Random.Shared.Next(activePlayers.Count);
            var sharkKingIndex = Random.Shared.Next(activePlayers.Count - 1);
            if (sharkKingIndex >= mouseIndex)
            {
                sharkKingIndex++;
            }

            result = new GameStartResult
            {
                Mouse = activePlayers[mouseIndex],
                SharkKing = activePlayers[sharkKingIndex],
                CurrentFloor = 1,
                TargetFloor = CalculateTargetFloor(activePlayers.Count),
                TargetClients = clients.Where(x => x != null).Select(x => x!).ToList()
            };
            Mouse = result.Mouse;
            SharkKing = result.SharkKing;
            State = RoomState.Playing;
            CurrentRound = 0;
            CurrentFloor = result.CurrentFloor;
            TargetFloor = result.TargetFloor;
            Stage = GameStage.None;
            StageEndTimeUtc = DateTime.MaxValue;
            return ResCode.Success;
        }
    }

    public StageChangeResult ChangeStage(GameStage stage, TimeSpan stageDuration)
    {
        lock (this)
        {
            if (stage == GameStage.Observe || (stage == GameStage.Hide && Stage != GameStage.Observe))
            {
                CurrentRound++;
                ResetMagic();
                ResetIndicators();
            }

            Stage = stage;
            StageEndTimeUtc = DateTime.UtcNow.Add(stageDuration);
            return new StageChangeResult
            {
                Round = CurrentRound,
                Stage = Stage,
                StageSeconds = (int)stageDuration.TotalSeconds,
                CurrentFloor = CurrentFloor,
                TargetFloor = TargetFloor,
                Magic = Magic,
                Indicators = Indicators,
                TargetClients = clients.Where(x => x != null).Select(x => x!).ToList()
            };
        }
    }

    public bool ChangeIndicator(change_indicator_c2s msgData, out ChangeIndicator result)
    {
        lock (this)
        {
            result = new ChangeIndicator
            {
                IndicatorId = msgData.IndicatorId,
                Position = msgData.Position,
                Rotation = msgData.Rotation,
                Color = msgData.Color
            };

            var indicator = Indicators.FirstOrDefault(x => x.IndicatorId == msgData.IndicatorId);
            if (indicator == null)
            {
                return false;
            }

            indicator.Position = msgData.Position;
            indicator.Rotation = msgData.Rotation;
            indicator.Color = msgData.Color;
            return true;
        }
    }

    public bool TryGetNextStage(DateTime now, out GameStage nextStage)
    {
        lock (this)
        {
            nextStage = GameStage.None;
            if (State != RoomState.Playing || now < StageEndTimeUtc)
            {
                return false;
            }

            nextStage = Stage switch
            {
                GameStage.Observe => GameStage.Hide,
                GameStage.Hide => GameStage.Kill,
                GameStage.Kill => GameStage.Hide,
                _ => GameStage.None
            };

            StageEndTimeUtc = DateTime.MaxValue;
            return nextStage != GameStage.None;
        }
    }

    public List<TcpClient> GetSharkClients()
    {
        lock (this)
        {
            if (Mouse == null)
            {
                return [];
            }

            return clients
                .Where((client, idx) => client != null
                                        && players[idx] != null
                                        && players[idx]!.IdInRoom != Mouse.IdInRoom)
                .Select(client => client!)
                .ToList();
        }
    }

    public List<Player> GetPlayersSnapshot()
    {
        lock (this)
        {
            return players.OfType<Player>().ToList();
        }
    }

    public bool TryRemoveClient(TcpClient client, out Player leftPlayer, out bool roomIsEmpty)
    {
        lock (this)
        {
            if (!TryRemoveClient(client, out leftPlayer))
            {
                roomIsEmpty = false;
                return false;
            }

            roomIsEmpty = IsEmpty();
            return true;
        }
    }

    private bool TryRemoveClient(TcpClient client, out Player leftPlayer)
    {
        for (var i = 0; i < clients.Length; i++)
        {
            if (!ReferenceEquals(clients[i], client))
            {
                continue;
            }

            leftPlayer = players[i]!;
            clients[i] = null;
            players[i] = null;
            return true;
        }

        leftPlayer = null!;
        return false;
    }

    public bool IsEmpty()
    {
        return clients.All(x => x == null);
    }

    public void Broadcast(JsonPacket packet)
    {
        for (var i = 0; i < clients.Length; i++)
        {
            if (clients[i] == null || players[i] == null)
            {
                continue;
            }

            try
            {
                Dispatcher.Send(clients[i]!, packet);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Broadcast failed for room={RoomId}, idx={i}: {e.Message}");
            }
        }
    }

    private static Player CreateRoomPlayer(int uid, int idInRoom)
    {
        return new Player
        {
            Uid = uid,
            IdInRoom = idInRoom,
            Ready = false
        };
    }

    private void ResetMagic()
    {
        Magic = CalculateInitialMagic(players.OfType<Player>().Count(), CurrentRound);
    }

    private void ResetIndicators()
    {
        Indicators = CloneIndicators(initialIndicators);
    }

    private static List<ServerIndicator> CloneIndicators(List<ServerIndicator> indicators)
    {
        return indicators.Select(CloneIndicator).ToList();
    }

    private static List<ServerIndicator> CreateInitialIndicators(int count)
    {
        var indicators = new List<ServerIndicator>(count);
        for (var i = 0; i < count; i++)
        {
            indicators.Add(new ServerIndicator
            {
                IndicatorId = i,
                Position = CreateRandomIndicatorPosition(),
                Rotation = CreateRandomIndicatorRotation(),
                Color = CreateRandomIndicatorColor()
            });
        }

        return indicators;
    }

    private static ServerIndicator CloneIndicator(ServerIndicator indicator)
    {
        return new ServerIndicator
        {
            IndicatorId = indicator.IndicatorId,
            Position = indicator.Position,
            Rotation = indicator.Rotation,
            Color = indicator.Color
        };
    }

    private static ServerVector3 CreateRandomIndicatorPosition()
    {
        return new ServerVector3
        {
            X = Random.Shared.NextSingle() * 20f - 10f,
            Y = 0.5f,
            Z = Random.Shared.NextSingle() * 20f - 10f
        };
    }

    private static ServerVector3 CreateRandomIndicatorRotation()
    {
        return new ServerVector3
        {
            X = Random.Shared.NextSingle() * 360f,
            Y = Random.Shared.NextSingle() * 360f,
            Z = Random.Shared.NextSingle() * 360f
        };
    }

    private static ServerColor CreateRandomIndicatorColor()
    {
        return new ServerColor
        {
            R = Random.Shared.NextSingle(),
            G = Random.Shared.NextSingle(),
            B = Random.Shared.NextSingle(),
            A = 1f
        };
    }

    private static int CalculateInitialMagic(int playerCount, int round)
    {
        return playerCount + round - 1;
    }

    private static int CalculateTargetFloor(int playerCount)
    {
        return playerCount * playerCount + playerCount;
    }
}

public enum RoomState
{
    Waiting,
    Playing
}

public class StageChangeResult
{
    public int Round { get; set; }
    public GameStage Stage { get; set; }
    public int StageSeconds { get; set; }
    public int CurrentFloor { get; set; }
    public int TargetFloor { get; set; }
    public int Magic { get; set; }
    public List<ServerIndicator> Indicators { get; set; } = new();
    public List<TcpClient> TargetClients { get; set; } = new();
}

public class GameStartResult
{
    public Player Mouse { get; set; } = null!;
    public Player SharkKing { get; set; } = null!;
    public int CurrentFloor { get; set; }
    public int TargetFloor { get; set; }
    public List<TcpClient> TargetClients { get; set; } = new();
}
