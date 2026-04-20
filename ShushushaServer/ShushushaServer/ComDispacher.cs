using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShushushaServer
{
// 这个 partial 放 server 和 client 共用的部分
    public static partial class Dispacher
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        };

        public static JsonPacket CreatePacket<T>(MsgId msgId, T data)
        {
            return new JsonPacket
            {
                MsgId = msgId,
                Data = JsonSerializer.SerializeToElement(data, JsonOptions)
            };
        }

        /// <summary>
        /// 不能直接用这个发消息，只能用 SendMsg 入列，然后由 socket 中开启的线程自动发出去
        /// </summary>
        public static void Send(NetworkStream stream, JsonPacket packet)
        {
            byte[] packetBytes = JsonSerializer.SerializeToUtf8Bytes(packet, JsonOptions);
            stream.Write(BitConverter.GetBytes(packetBytes.Length), 0, sizeof(int));
            stream.Write(packetBytes, 0, packetBytes.Length);
        }


        public static JsonPacket Receive(NetworkStream stream)
        {
            byte[] lengthBytes = ReadExact(stream, sizeof(int));
            int packetLength = BitConverter.ToInt32(lengthBytes, 0);

            if (packetLength <= 0)
            {
                throw new InvalidDataException($"Invalid packet length: {packetLength}");
            }

            byte[] packetBytes = ReadExact(stream, packetLength);
            JsonPacket? packet = JsonSerializer.Deserialize<JsonPacket>(packetBytes, JsonOptions);

            if (packet is null)
            {
                throw new InvalidDataException("Failed to deserialize json packet.");
            }

            return packet;
        }

        public static T? GetPacketData<T>(JsonPacket packet)
        {
            return packet.Data.Deserialize<T>(JsonOptions);
        }

        private static byte[] ReadExact(NetworkStream stream, int byteCount)
        {
            byte[] buffer = new byte[byteCount];
            int offset = 0;

            while (offset < byteCount)
            {
                int bytesRead = stream.Read(buffer, offset, byteCount - offset);
                if (bytesRead == 0)
                {
                    throw new IOException("Remote socket closed the connection.");
                }

                offset += bytesRead;
            }

            return buffer;
        }
    }

    public class JsonPacket
    {
        public MsgId MsgId { get; set; }
        public JsonElement Data { get; set; }
    }

    public enum MsgId
    {
        create_room_c2s = 1000,
        create_room_s2c = 1001,
        join_room_c2s = 1002, //这个是自己加入房间的请求
        join_room_s2c = 1003,
        ready_c2s = 1005,
        ready_s2c = 1006,
        game_start_s2c = 1007,
        game_start_c2s = 1008,
        JoinRoom = 2000, //这个是其他人加入房间时服务端主动发的
        GameStart = 2001,
        PlayerLeft = 2002,
    }

    public class create_room_s2c
    {
        public ResCode ResCode { get; set; }
        public int RoomId { get; set; }
    }

    public class create_room_c2s
    {
        public Player Player { get; set; }
    }

    public class join_room_s2c
    {
        public ResCode ResCode { get; set; }
        public int RoomId { get; set; }
        public List<Player> Players { get; set; } = new();
    }

    public class join_room_c2s
    {
        public int RoomId { get; set; }
        public Player Player { get; set; }
    }

    public class ready_c2s
    {
        public int RoomId { get; set; }
        public int IdInRoom { get; set; }
    }

    public class ready_s2c
    {
        public ResCode ResCode { get; set; }
        public Player Player { get; set; }
    }

    public class game_start_c2s
    {
        public int RoomId { get; set; }
    }

    public class game_start_s2c
    {
        public ResCode ResCode { get; set; }
    }


    public class JoinRoom
    {
        public Player Player { get; set; }
    }

    public class GameStart
    {
        public Player Mouse { get; set; }
    }


    public class PlayerLeft
    {
        public Player Player { get; set; } = null!;
    }


    public class Player
    {
        public int Uid { get; set; }
        public int IdInRoom { get; set; }

        public bool Ready { get; set; }
    }

    public enum ResCode
    {
        Success = 0,
        CantFindRoom = 1,
        RoomIsFull = 2
    }
}
