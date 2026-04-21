using System.Collections.Concurrent;
using System.Net.Sockets;

namespace ShushushaServer;

public static partial class Dispatcher
{
    private static readonly ConcurrentDictionary<TcpClient, SemaphoreSlim> SendLocks = new();

    public static void Send(TcpClient client, JsonPacket packet)
    {
        SemaphoreSlim sendLock = SendLocks.GetOrAdd(client, _ => new SemaphoreSlim(1, 1));
        sendLock.Wait();
        try
        {
            Send(client.GetStream(), packet);
        }
        finally
        {
            sendLock.Release();
        }
    }

    public static void RemoveSendLock(TcpClient client)
    {
        SendLocks.TryRemove(client, out _);
    }

    public static void ReceiveRoomMsg(TcpClient client)
    {
        using NetworkStream stream = client.GetStream();

        try
        {
            while (true)
            {
                JsonPacket packet = Receive(stream);
                Console.WriteLine($"Receive msgId={packet.MsgId}, data={packet.Data.ToString()}");

                switch (packet.MsgId)
                {
                    case MsgId.create_room_c2s:
                        RoomManager.CreateRoom(GetPacketData<create_room_c2s>(packet)!, client);
                        break;
                    case MsgId.join_room_c2s:
                        RoomManager.JoinRoom(GetPacketData<join_room_c2s>(packet)!, client);
                        break;
                    case MsgId.ready_c2s:
                        RoomManager.Ready(client);
                        break;
                    case MsgId.game_start_c2s:
                        RoomManager.GameStart(client);
                        break;
                    case MsgId.hide_indicator_c2s:
                        RoomManager.HideIndicator(GetPacketData<hide_indicator_c2s>(packet)!, client);
                        break;
                }
            }
        }
        catch (IOException)
        {
            Console.WriteLine("Client disconnected.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ReceiveRoomMsg error: {ex}");
        }
        finally
        {
            RoomManager.RemoveClient(client);
            client.Close();
            RemoveSendLock(client);
        }
    }

    public static void DebugLog(string log)
    {
        Console.WriteLine(log + "\n" + Environment.StackTrace);
    }
}
