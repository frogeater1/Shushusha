using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

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
            { MsgId.change_indicator_s2c, new UniTaskCompletionSource<JsonPacket>() },
            { MsgId.kill_shark_s2c, new UniTaskCompletionSource<JsonPacket>() },
        };

        public static async UniTask<create_room_s2c> CreateRoom()
        {
            Network.Connect();

            var source = ResetTask(MsgId.create_room_s2c);
            Dispatcher.SendMsg(Dispatcher.CreatePacket(MsgId.create_room_c2s, new create_room_c2s
            {
                Player = new Player
                {
                    Uid = Game.Instance.me.Uid
                }
            }));
            return Dispatcher.GetPacketData<create_room_s2c>(await source.Task);
        }

        public static async UniTask<join_room_s2c> JoinRoom(int roomId)
        {
            Network.Connect();

            var source = ResetTask(MsgId.join_room_s2c);
            Dispatcher.SendMsg(Dispatcher.CreatePacket(MsgId.join_room_c2s, new join_room_c2s
            {
                RoomId = roomId,
                Player = new Player
                {
                    Uid = Game.Instance.me.Uid
                }
            }));
            return Dispatcher.GetPacketData<join_room_s2c>(await source.Task);
        }


        public static async UniTask<ready_s2c> Ready()
        {
            var source = ResetTask(MsgId.ready_s2c);
            Dispatcher.SendMsg(Dispatcher.CreatePacket(MsgId.ready_c2s, new ready_c2s
            {
                IsReady = Game.Instance.me.Ready
            }));
            return Dispatcher.GetPacketData<ready_s2c>(await source.Task);
        }

        public static async UniTask<game_start_s2c> GameStart()
        {
            var source = ResetTask(MsgId.game_start_s2c);
            Dispatcher.SendMsg(Dispatcher.CreatePacket(MsgId.game_start_c2s, new game_start_c2s()));
            return Dispatcher.GetPacketData<game_start_s2c>(await source.Task);
        }

        public static async UniTask<change_indicator_s2c> ChangeIndicator(int indicatorId, IndicatorChangeKind kind)
        {
            var source = ResetTask(MsgId.change_indicator_s2c);
            Dispatcher.SendMsg(Dispatcher.CreatePacket(MsgId.change_indicator_c2s, new change_indicator_c2s
            {
                IndicatorId = indicatorId,
                Kind = kind
            }));
            return Dispatcher.GetPacketData<change_indicator_s2c>(await source.Task);
        }

        public static async UniTask<kill_shark_s2c> KillShark(int indicatorId)
        {
            var source = ResetTask(MsgId.kill_shark_s2c);
            Dispatcher.SendMsg(Dispatcher.CreatePacket(MsgId.kill_shark_c2s, new kill_shark_c2s
            {
                IndicatorId = indicatorId
            }));
            return Dispatcher.GetPacketData<kill_shark_s2c>(await source.Task);
        }

        private static UniTaskCompletionSource<JsonPacket> ResetTask(MsgId msgId)
        {
            var source = new UniTaskCompletionSource<JsonPacket>();
            tasks[msgId] = source;
            return source;
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
