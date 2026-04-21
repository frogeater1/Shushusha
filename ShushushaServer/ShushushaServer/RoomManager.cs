using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text.Json;

namespace ShushushaServer;

public class RoomManager
{
    public static ConcurrentDictionary<int, Room> rooms = new();
    private static readonly ConcurrentDictionary<TcpClient, PlayerSession> playerSessions = new();
    private static int nextRoomId;

    public static void CreateRoom(create_room_c2s msgData, TcpClient client)
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

    public static void JoinRoom(join_room_c2s msgData, TcpClient client)
    {
        if (TryGetSessionRoom(client, out PlayerSession currentSession, out Room currentRoom))
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

        if (!rooms.TryGetValue(msgData.RoomId, out Room? room))
        {
            Dispatcher.Send(client, Dispatcher.CreatePacket(MsgId.join_room_s2c, new join_room_s2c { ResCode = ResCode.CantFindRoom }));
            return;
        }

        ResCode resCode = room.TryJoin(msgData, client, out Player joinedPlayer, out List<TcpClient> notifyClients);
        if (resCode != ResCode.Success)
        {
            Dispatcher.Send(client, Dispatcher.CreatePacket(MsgId.join_room_s2c, new join_room_s2c { ResCode = resCode }));
            return;
        }

        try
        {
            foreach (TcpClient notifyClient in notifyClients)
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

    public static void Ready(TcpClient client)
    {
        if (!TryGetSessionRoom(client, out PlayerSession session, out Room room))
        {
            Console.WriteLine("Ready ignored because client has no room session.");
            return;
        }

        ResCode resCode = room.Ready(session.IdInRoom, out Player player);
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

    public static void GameStart(TcpClient client)
    {
        if (!TryGetSessionRoom(client, out PlayerSession session, out Room room))
        {
            Console.WriteLine("GameStart ignored because client has no room session.");
            return;
        }

        ResCode resCode = room.TryStartGame(session.IdInRoom, out GameStartResult gameStartResult);
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

        foreach (TcpClient targetClient in gameStartResult.TargetClients)
        {
            var gameStartMsg = Dispatcher.CreatePacket(MsgId.GameStart, new GameStart
            {
                Mouse = gameStartResult.Mouse,
                SharkKing = gameStartResult.SharkKing
            });

            Console.WriteLine($"Send {JsonSerializer.Serialize(gameStartMsg)} ");
            Dispatcher.Send(targetClient, gameStartMsg);
        }

        Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            BroadcastStageChange(room, GameStage.Hide);
        });
    }

    public static void HideIndicator(hide_indicator_c2s msgData, TcpClient client)
    {
        if (!TryGetSessionRoom(client, out PlayerSession session, out Room room))
        {
            Console.WriteLine("HideIndicator ignored because client has no room session.");
            return;
        }

        Dispatcher.Send(client, Dispatcher.CreatePacket(MsgId.hide_indicator_s2c, new hide_indicator_s2c
        {
            ResCode = ResCode.Success
        }));

        var packet = Dispatcher.CreatePacket(MsgId.HideIndicator, new HideIndicator
        {
            IdInRoom = session.IdInRoom,
            X = msgData.X,
            Y = msgData.Y,
            Z = msgData.Z
        });

        Console.WriteLine($"Broadcast {JsonSerializer.Serialize(packet)} ");
        foreach (TcpClient sharkClient in room.GetSharkClients())
        {
            Dispatcher.Send(sharkClient, packet);
        }
    }

    public static void RemoveClient(TcpClient client)
    {
        if (!playerSessions.TryRemove(client, out PlayerSession? session))
        {
            return;
        }

        if (!rooms.TryGetValue(session.RoomId, out Room? room))
        {
            return;
        }

        Player leftPlayer;
        if (!room.TryRemoveClient(client, out leftPlayer, out bool roomIsEmpty))
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

    private static void RegisterClient(TcpClient client, int roomId, int idInRoom)
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

    private static void BroadcastStageChange(Room room, GameStage stage)
    {
        StageChangeResult result = room.ChangeStage(stage);
        var packet = Dispatcher.CreatePacket(MsgId.ChangeStage, new ChangeStage
        {
            Round = result.Round,
            Stage = result.Stage
        });

        Console.WriteLine($"Broadcast {JsonSerializer.Serialize(packet)} ");
        foreach (TcpClient targetClient in result.TargetClients)
        {
            Dispatcher.Send(targetClient, packet);
        }
    }

    private static bool TryGetSessionRoom(TcpClient client, out PlayerSession session, out Room room)
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
