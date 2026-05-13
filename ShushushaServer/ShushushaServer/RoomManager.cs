using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text.Json;

namespace ShushushaServer;

public class RoomManager
{
    public static RoomManager Instance { get; } = new();

    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan ObserveStageDuration = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan HideStageDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan KillStageDuration = TimeSpan.FromSeconds(40);

    private readonly ConcurrentDictionary<int, Room> rooms = new();
    private readonly ConcurrentDictionary<TcpClient, PlayerSession> playerSessions = new();
    private int nextRoomId;

    public void CreateRoom(create_room_c2s msgData, TcpClient client)
    {
        if (TryGetSessionRoom(client, out PlayerSession currentSession, out _))
        {
            Dispatcher.Send(client, Dispatcher.CreatePacket(MsgId.create_room_s2c, new create_room_s2c
            {
                ResCode = ResCode.AlreadyInRoom,
                RoomId = currentSession.RoomId
            }));
            return;
        }

        var room = new Room(msgData, client, Interlocked.Increment(ref nextRoomId));
        try
        {
            if (!rooms.TryAdd(room.RoomId, room))
            {
                throw new Exception("Room already exists");
            }

            RegisterClient(client, room.RoomId, 0);
            Console.WriteLine("CreateRoom Success.");
            Dispatcher.Send(client, Dispatcher.CreatePacket(MsgId.create_room_s2c, new create_room_s2c { ResCode = ResCode.Success, RoomId = room.RoomId }));
        }
        catch (Exception e)
        {
            Console.WriteLine(e + e.StackTrace);
            throw;
        }
    }

    public void JoinRoom(join_room_c2s msgData, TcpClient client)
    {
        if (TryGetSessionRoom(client, out var currentSession, out var currentRoom))
        {
            if (currentSession.RoomId == msgData.RoomId)
            {
                SendJoinRoomSuccess(client, currentRoom);

                return;
            }

            Dispatcher.Send(client, Dispatcher.CreatePacket(MsgId.join_room_s2c, new join_room_s2c
            {
                ResCode = ResCode.AlreadyInRoom,
                RoomId = currentSession.RoomId
            }));
            return;
        }

        if (!rooms.TryGetValue(msgData.RoomId, out var room))
        {
            Dispatcher.Send(client, Dispatcher.CreatePacket(MsgId.join_room_s2c, new join_room_s2c { ResCode = ResCode.CantFindRoom }));
            return;
        }

        var resCode = room.TryJoin(msgData, client, out var joinedPlayer, out var notifyClients);
        if (resCode != ResCode.Success)
        {
            Dispatcher.Send(client, Dispatcher.CreatePacket(MsgId.join_room_s2c, new join_room_s2c { ResCode = resCode }));
            return;
        }

        try
        {
            foreach (var notifyClient in notifyClients)
            {
                Dispatcher.Send(notifyClient, Dispatcher.CreatePacket(MsgId.JoinRoom, new JoinRoom
                {
                    Player = joinedPlayer
                }));
            }

            RegisterClient(client, room.RoomId, joinedPlayer.IdInRoom);
            SendJoinRoomSuccess(client, room);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.StackTrace);
            throw;
        }
    }

    public void Ready(ready_c2s msgData, TcpClient client)
    {
        if (!TryGetSessionRoom(client, out var session, out var room))
        {
            Console.WriteLine("Ready ignored because client has no room session.");
            return;
        }

        var resCode = room.Ready(session.IdInRoom, msgData.IsReady, out var player);
        Dispatcher.Send(client, Dispatcher.CreatePacket(MsgId.ready_s2c, new ready_s2c
        {
            ResCode = resCode
        }));

        if (resCode != ResCode.Success)
        {
            return;
        }

        var msg = Dispatcher.CreatePacket(MsgId.Ready, new Ready
        {
            Player = player
        });

        Console.WriteLine($"Broadcast {JsonSerializer.Serialize(msg)} ");
        room.Broadcast(msg);
    }

    public void GameStart(TcpClient client)
    {
        if (!TryGetSessionRoom(client, out var session, out var room))
        {
            Console.WriteLine("GameStart ignored because client has no room session.");
            return;
        }

        var resCode = room.TryStartGame(session.IdInRoom, out var gameStartResult);
        if (resCode != ResCode.Success)
        {
            Dispatcher.Send(client, Dispatcher.CreatePacket(MsgId.game_start_s2c, new game_start_s2c
            {
                ResCode = resCode,
            }));
            return;
        }

        Dispatcher.Send(client, Dispatcher.CreatePacket(MsgId.game_start_s2c, new game_start_s2c
        {
            ResCode = ResCode.Success,
        }));

        foreach (var targetClient in gameStartResult.TargetClients)
        {
            var gameStartMsg = Dispatcher.CreatePacket(MsgId.GameStart, new GameStart
            {
                Mouse = gameStartResult.Mouse,
                SharkKing = gameStartResult.SharkKing
            });

            Console.WriteLine($"Send {JsonSerializer.Serialize(gameStartMsg)} ");
            Dispatcher.Send(targetClient, gameStartMsg);
        }

        room.BroadcastStageChange(GameStage.Observe, GetStageDuration(GameStage.Observe));
    }

    public void ChangeIndicator(change_indicator_c2s msgData, TcpClient client)
    {
        if (!TryGetSessionRoom(client, out var session, out var room))
        {
            Console.WriteLine("ChangeIndicator ignored because client has no room session.");
            return;
        }

        var resCode = room.ChangeIndicator(msgData, session.IdInRoom, out var changeIndicator);
        if (resCode != ResCode.Success)
        {
            Dispatcher.Send(client, Dispatcher.CreatePacket(MsgId.change_indicator_s2c, new change_indicator_s2c
            {
                ResCode = resCode
            }));
            return;
        }

        Dispatcher.Send(client, Dispatcher.CreatePacket(MsgId.change_indicator_s2c, new change_indicator_s2c
        {
            ResCode = ResCode.Success
        }));

        var packet = Dispatcher.CreatePacket(MsgId.ChangeIndicator, changeIndicator);

        Console.WriteLine($"Broadcast {JsonSerializer.Serialize(packet)} ");
        foreach (var sharkClient in room.GetSharkClients())
        {
            Dispatcher.Send(sharkClient, packet);
        }
    }

    public void KillShark(kill_shark_c2s msgData, TcpClient client)
    {
        if (!TryGetSessionRoom(client, out var session, out var room))
        {
            Console.WriteLine("KillShark ignored because client has no room session.");
            return;
        }

        var resCode = room.KillShark(msgData, session.IdInRoom, out var killedSharks);
        Dispatcher.Send(client, Dispatcher.CreatePacket(MsgId.kill_shark_s2c, new kill_shark_s2c
        {
            ResCode = resCode,
            Magic = room.Magic,
            KilledSharks = killedSharks
        }));

        if (resCode == ResCode.KillSharkFailed)
        {
            room.BroadcastStageChange(GameStage.Hide, GetStageDuration(GameStage.Hide));
        }
    }

    public void RemoveClient(TcpClient client)
    {
        if (!playerSessions.TryRemove(client, out PlayerSession? session))
        {
            return;
        }

        if (!rooms.TryGetValue(session.RoomId, out Room? room))
        {
            return;
        }

        if (!room.TryRemoveClient(client, out var leftPlayer, out var roomIsEmpty))
        {
            return;
        }

        if (roomIsEmpty)
        {
            rooms.TryRemove(room.RoomId, out _);
            Console.WriteLine($"Room {room.RoomId} removed because it is empty.");
            return;
        }

        var packet = Dispatcher.CreatePacket(MsgId.PlayerLeft, new PlayerLeft
        {
            Player = leftPlayer
        });
        room.Broadcast(packet);
        Console.WriteLine($"Player uid={leftPlayer.Uid}, idInRoom={leftPlayer.IdInRoom} left room {room.RoomId}.");
    }

    private void RegisterClient(TcpClient client, int roomId, int idInRoom)
    {
        playerSessions[client] = new PlayerSession(roomId, idInRoom);
    }

    private static void SendJoinRoomSuccess(TcpClient client, Room room)
    {
        Dispatcher.Send(client, Dispatcher.CreatePacket(MsgId.join_room_s2c, new join_room_s2c
        {
            ResCode = ResCode.Success,
            RoomId = room.RoomId,
            Players = room.GetPlayersSnapshot(),
        }));
    }

    public async Task RunTickLoop(CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(TickInterval);
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                Tick();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void Tick()
    {
        var now = DateTime.UtcNow;
        foreach (var room in rooms.Values)
        {
            if (room.TryGetNextStage(now, out var nextStage))
            {
                room.BroadcastStageChange(nextStage, GetStageDuration(nextStage));
            }
        }
    }

    private static TimeSpan GetStageDuration(GameStage stage)
    {
        return stage switch
        {
            GameStage.Observe => ObserveStageDuration,
            GameStage.Hide => HideStageDuration,
            GameStage.Kill => KillStageDuration,
            _ => TimeSpan.Zero
        };
    }

    private bool TryGetSessionRoom(TcpClient client, out PlayerSession session, out Room room)
    {
        if (!playerSessions.TryGetValue(client, out PlayerSession? foundSession))
        {
            session = null!;
            room = null!;
            return false;
        }

        session = foundSession;
        if (!rooms.TryGetValue(session.RoomId, out Room? foundRoom))
        {
            room = null!;
            return false;
        }

        room = foundRoom;
        return true;
    }

    private sealed record PlayerSession(int RoomId, int IdInRoom);
}
