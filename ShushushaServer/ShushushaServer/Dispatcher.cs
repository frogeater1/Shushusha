using System.Net.Sockets;
using System.Text.Json;

namespace ShushushaServer;

public static partial class Dispatcher
{
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
                        RoomManager.CreateRoom(packet.Data.Deserialize<create_room_c2s>()!, client);
                        break;
                    case MsgId.join_room_c2s:
                        RoomManager.JoinRoom(packet.Data.Deserialize<join_room_c2s>()!, client);
                        break;
                    case MsgId.ready_c2s:
                        RoomManager.Ready(client);
                        break;
                    case MsgId.game_start_c2s:
                        RoomManager.GameStart(client);
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
        }
    }

    public static void DebugLog(string log)
    {
        Console.WriteLine(log + "\n" + Environment.StackTrace);
    }
}
