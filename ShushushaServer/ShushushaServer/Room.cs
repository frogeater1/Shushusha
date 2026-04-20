using System.Collections.Concurrent;
using System.Net.Sockets;

namespace ShushushaServer;

public class Room
{
    public const int MaxPlayers = 12;

    public int RoomId;
    public TcpClient?[] clients = new TcpClient[MaxPlayers];
    public Player?[] players = new Player[MaxPlayers];

    public ConcurrentQueue<JsonPacket> waitingMsgs = new();

    public Room(create_room_c2s msg, TcpClient client1, int roomId)
    {
        RoomId = roomId;
        msg.Player.IdInRoom = 0;
        clients[0] = client1;
        players[0] = msg.Player;
    }

    public bool TryJoin(join_room_c2s msg, TcpClient client, out Player joinedPlayer)
    {
        for (int i = 0; i < clients.Length; i++)
        {
            if (clients[i] != null)
            {
                continue;
            }

            msg.Player.IdInRoom = i;
            clients[i] = client;
            players[i] = msg.Player;
            joinedPlayer = msg.Player;
            return true;
        }

        joinedPlayer = null!;
        return false;
    }

    public bool TryRemoveClient(TcpClient client, out Player leftPlayer)
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
                Dispacher.Send(clients[i]!.GetStream(), packet);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Broadcast failed for room={RoomId}, idx={i}: {e.Message}");
            }
        }
    }

    public void GameStart()
    {
        // Console.WriteLine("GameStart!");
        // Task.Run(() => ReceiveMsg(0));
        // Task.Run(() => ReceiveMsg(1));
        // var t = new System.Timers.Timer();
        // t.Interval = 1000f / 60;
        // t.Elapsed += (sender, args) =>
        // {
        //     RpcMsg[] rpc_msgs;
        //     lock (waitingMsgs)
        //     {
        //         rpc_msgs = waitingMsgs.ToArray();
        //         waitingMsgs.Clear();
        //     }
        //
        //     var logic_update = new LogicUpdate
        //     {
        //         Rpcs = { rpc_msgs }
        //     };
        //     if (clients[0] is { Connected: true })
        //     {
        //         Dispacher.Send(clients[0].GetStream(), logic_update);
        //     }
        //
        //     if (clients[1] is { Connected: true })
        //     {
        //         Dispacher.Send(clients[1].GetStream(), logic_update);
        //     }
        // };
        // t.Enabled = true;
    }

    public void ReceiveMsg(int idx)
    {
        // var stream = clients[idx].GetStream();
        // while (true)
        // {
        //     var msg = Dispacher.Receive(stream);
        //     Console.WriteLine(msg);
        //     if (msg is RpcMsg rpcMsg)
        //     {
        //         waitingMsgs.Enqueue(rpcMsg);
        //     }
        //     else
        //     {
        //         Console.WriteLine("收到错误的消息: " + msg);
        //     }
        // }
    }
}
