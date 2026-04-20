using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ShushushaServer
{
    public static class Request
    {
        private static Dictionary<MsgId, UniTaskCompletionSource<JsonPacket>> tasks = new()
        {
            { MsgId.create_room_s2c, new UniTaskCompletionSource<JsonPacket>() },
            { MsgId.join_room_s2c, new UniTaskCompletionSource<JsonPacket>() },
            { MsgId.ready_s2c, new UniTaskCompletionSource<JsonPacket>() },
            { MsgId.game_start_s2c, new UniTaskCompletionSource<JsonPacket>() },
        };

        public static async UniTask<create_room_s2c> CreateRoom()
        {
            Network.Connect();

            Dispatcher.SendMsg(Dispatcher.CreatePacket(MsgId.create_room_c2s, new create_room_c2s
            {
                Player = new Player
                {
                    Uid = Game.Instance.me.Uid
                }
            }));
            var source = tasks[MsgId.create_room_s2c];
            return (await source.Task).Data.Deserialize<create_room_s2c>();
        }

        public static async UniTask<join_room_s2c> JoinRoom(int roomId)
        {
            Network.Connect();

            Dispatcher.SendMsg(Dispatcher.CreatePacket(MsgId.join_room_c2s, new join_room_c2s
            {
                RoomId = roomId,
                Player = new Player
                {
                    Uid = Game.Instance.me.Uid
                }
            }));
            var source = tasks[MsgId.join_room_s2c];
            var data = (await source.Task).Data;
            return data.Deserialize<join_room_s2c>();
        }


        public static async UniTask<ready_s2c> Ready()
        {
            Dispatcher.SendMsg(Dispatcher.CreatePacket(MsgId.ready_c2s, new ready_c2s
            {
                RoomId = int.Parse(Game.Instance.uilobby.m_房间号.text),
                IdInRoom = Game.Instance.me.IdInRoom
            }));
            var source = tasks[MsgId.ready_s2c];
            var data = (await source.Task).Data;
            return data.Deserialize<ready_s2c>();
        }

        public static async UniTask<game_start_s2c> GameStart()
        {
            Dispatcher.SendMsg(Dispatcher.CreatePacket(MsgId.game_start_c2s, new game_start_c2s
            {
                RoomId = int.Parse(Game.Instance.uilobby.m_房间号.text),
            }));
            var source = tasks[MsgId.game_start_s2c];
            var data = (await source.Task).Data;
            return data.Deserialize<game_start_s2c>();
        }

        public static void Response(JsonPacket msg)
        {
            if (!tasks.TryGetValue(msg.MsgId, out var source))
            {
                throw new Exception("收到未知消息");
            }

            source.TrySetResult(msg);
        }

        public static void CancelCreateRoom()
        {
            tasks[MsgId.create_room_s2c].TrySetCanceled();
        }

        public static void CancelJoinRoom()
        {
            tasks[MsgId.join_room_s2c].TrySetCanceled();
        }

        // public static async UniTaskVoid ExitRoom(string roomName)
        // {
        //     Dispatcher.SendMsg(new exit_room_c2s
        //     {
        //         Name = roomName
        //     });
        //     var source = tasks[ProtoIdx.join_room_s2c];
        //     return await source.Task;
        // }
    }
}
