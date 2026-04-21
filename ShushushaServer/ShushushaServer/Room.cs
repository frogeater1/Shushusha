using System.Collections.Concurrent;
using System.Net.Sockets;

namespace ShushushaServer;

public class Room
{
    public const int MaxPlayers = 12;
    private const int MinPlayersToStart = 2;

    public int RoomId;
    public RoomState State = RoomState.Waiting;
    public int CurrentRound;
    public GameStage Stage = GameStage.None;
    public Player? Mouse;
    public Player? SharkKing;
    public TcpClient?[] clients = new TcpClient[MaxPlayers];
    public Player?[] players = new Player[MaxPlayers];

    public ConcurrentQueue<JsonPacket> waitingMsgs = new();

    public Room(create_room_c2s msg, TcpClient client1, int roomId)
    {
        RoomId = roomId;
        clients[0] = client1;
        players[0] = CreateRoomPlayer(msg.Player.Uid, 0);
    }

    public ResCode TryJoin(join_room_c2s msg, TcpClient client, out Player joinedPlayer, out List<TcpClient> notifyClients)
    {
        lock (this)
        {
            notifyClients = new();
            if (State != RoomState.Waiting)
            {
                joinedPlayer = null!;
                return ResCode.GameAlreadyStarted;
            }

            for (int i = 0; i < clients.Length; i++)
            {
                if (clients[i] != null)
                {
                    continue;
                }

                clients[i] = client;
                joinedPlayer = CreateRoomPlayer(msg.Player.Uid, i);
                players[i] = joinedPlayer;
                int joinedIdInRoom = joinedPlayer.IdInRoom;
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

    public ResCode Ready(int idInRoom, out Player readyPlayer)
    {
        lock (this)
        {
            readyPlayer = players[idInRoom]!;
            if (State != RoomState.Waiting)
            {
                return ResCode.InvalidRoomState;
            }

            readyPlayer.Ready = true;
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

            int mouseIndex = Random.Shared.Next(activePlayers.Count);
            int sharkKingIndex = Random.Shared.Next(activePlayers.Count - 1);
            if (sharkKingIndex >= mouseIndex)
            {
                sharkKingIndex++;
            }

            result = new GameStartResult
            {
                Mouse = activePlayers[mouseIndex],
                SharkKing = activePlayers[sharkKingIndex],
                TargetClients = clients.Where(x => x != null).Select(x => x!).ToList()
            };
            Mouse = result.Mouse;
            SharkKing = result.SharkKing;
            State = RoomState.Playing;
            CurrentRound = 0;
            Stage = GameStage.None;
            return ResCode.Success;
        }
    }

    public StageChangeResult ChangeStage(GameStage stage)
    {
        lock (this)
        {
            if (stage == GameStage.Hide)
            {
                CurrentRound++;
            }

            Stage = stage;
            return new StageChangeResult
            {
                Round = CurrentRound,
                Stage = Stage,
                TargetClients = clients.Where(x => x != null).Select(x => x!).ToList()
            };
        }
    }

    public List<TcpClient> GetSharkClients()
    {
        lock (this)
        {
            if (Mouse == null)
            {
                return new List<TcpClient>();
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
        for (int i = 0; i < clients.Length; i++)
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
        for (int i = 0; i < clients.Length; i++)
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
    public List<TcpClient> TargetClients { get; set; } = new();
}

public class GameStartResult
{
    public Player Mouse { get; set; } = null!;
    public Player SharkKing { get; set; } = null!;
    public List<TcpClient> TargetClients { get; set; } = new();
}
