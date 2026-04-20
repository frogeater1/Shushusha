using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text.Json;

namespace ShushushaServer;

public class RoomManager
{
    public static ConcurrentDictionary<int, Room> rooms = new();
    private static readonly ConcurrentDictionary<TcpClient, PlayerSession> playerSessions = new();

    public static void CreateRoom(create_room_c2s msgData, TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        var room = new Room(msgData, client, rooms.Count + 1);
        try
        {
            if (!rooms.TryAdd(room.RoomId, room))
            {
                throw new Exception("Room already exists");
            }

            RegisterClient(client, room.RoomId, 0);
            Console.WriteLine("CreateRoom Success.");
            Dispatcher.Send(stream, Dispatcher.CreatePacket(MsgId.create_room_s2c, new create_room_s2c { ResCode = ResCode.Success, RoomId = room.RoomId }));
        }
        catch (Exception e)
        {
            Console.WriteLine(e + e.StackTrace);
            throw;
        }
    }

    public static void JoinRoom(join_room_c2s msgData, TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        if (!rooms.TryGetValue(msgData.RoomId, out Room? room))
        {
            Dispatcher.Send(stream, Dispatcher.CreatePacket(MsgId.join_room_s2c, new join_room_s2c { ResCode = ResCode.CantFindRoom }));
            return;
        }

        lock (room)
        {
            if (!room.TryJoin(msgData, client, out Player joinedPlayer))
            {
                Dispatcher.Send(stream, Dispatcher.CreatePacket(MsgId.join_room_s2c, new join_room_s2c { ResCode = ResCode.RoomIsFull }));
                return;
            }

            try
            {
                for (int i = 0; i < room.clients.Length; i++)
                {
                    if (room.clients[i] == null || room.players[i] == null || i == joinedPlayer.IdInRoom)
                    {
                        continue;
                    }

                    Dispatcher.Send(room.clients[i]!.GetStream(), Dispatcher.CreatePacket(MsgId.JoinRoom, new JoinRoom
                    {
                        Player = joinedPlayer
                    }));
                }

                RegisterClient(client, room.RoomId, joinedPlayer.IdInRoom);
                Dispatcher.Send(stream, Dispatcher.CreatePacket(MsgId.join_room_s2c, new join_room_s2c
                {
                    ResCode = ResCode.Success,
                    RoomId = room.RoomId,
                    Players = room.players.Where(x => x != null).ToList()!,
                }));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                throw;
            }
        }
    }

    public static void Ready(TcpClient client)
    {
        if (!TryGetSessionRoom(client, out PlayerSession session, out Room room))
        {
            Console.WriteLine("Ready ignored because client has no room session.");
            return;
        }

        Player player;
        lock (room)
        {
            player = room.players[session.IdInRoom]!;
            player.Ready = true;
        }

        var msg = Dispatcher.CreatePacket(MsgId.ready_s2c, new ready_s2c
        {
            ResCode = ResCode.Success,
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

        var activePlayers = room.players.OfType<Player>().ToList();
        var mouse = activePlayers[Random.Shared.Next(activePlayers.Count)];
        Dispatcher.Send(client.GetStream(), Dispatcher.CreatePacket(MsgId.game_start_s2c, new game_start_s2c
        {
            ResCode = ResCode.Success,
        }));

        for (int i = 0; i < room.clients.Length; i++)
        {
            if (room.clients[i] == null || room.players[i] == null)
            {
                continue;
            }

            var gameStartMsg = Dispatcher.CreatePacket(MsgId.GameStart, new GameStart
            {
                Mouse = mouse
            });

            Console.WriteLine($"Send {JsonSerializer.Serialize(gameStartMsg)} ");
            Dispatcher.Send(room.clients[i]!.GetStream(), gameStartMsg);
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
        bool roomIsEmpty;
        lock (room)
        {
            if (!room.TryRemoveClient(client, out leftPlayer))
            {
                return;
            }

            roomIsEmpty = room.IsEmpty();
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
