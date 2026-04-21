using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShushushaServer
{
// 这个 partial 放 server 和 client 共用的部分
    public static partial class Dispatcher
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
        /// 不能直接用这个发消息
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
}