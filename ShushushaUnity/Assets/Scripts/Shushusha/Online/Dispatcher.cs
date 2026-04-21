using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace ShushushaServer
{
    public static partial class Dispatcher
    {
        private static ConcurrentQueue<JsonPacket> waitingSendMsgs = new();
        private static ConcurrentQueue<JsonPacket> waitingDistributeMsgs = new();
        private static readonly SemaphoreSlim waitingSendMsgSignal = new(0);

        public static void Distribute()
        {
            while (GetWaitingDistributeMsg() is { } msg)
            {
                Debug.Log(msg.Data.ToString());
                switch (msg.MsgId)
                {
                    // case LogicUpdate data:
                    //     foreach (var rpc in data.Rpcs)
                    //     {
                    //         Debug.Log(rpc);
                    //         if (rpc.Command.Is(Command_Enter.Descriptor))
                    //         {
                    //             var command = rpc.Command.Unpack<Command_Enter>();
                    //             LocalCommander.Enter(rpc.From, command);
                    //         }
                    //         else if (rpc.Command.Is(Command_Exit.Descriptor))
                    //         {
                    //             var command = rpc.Command.Unpack<Command_Exit>();
                    //             LocalCommander.Exit(rpc.From, command);
                    //         }
                    //         else if (rpc.Command.Is(Command_Skill.Descriptor))
                    //         {
                    //             var command = rpc.Command.Unpack<Command_Skill>();
                    //             LocalCommander.Skill(rpc.From, command);
                    //         }
                    //     }
                    //
                    //     Game.Instance.logicFrame++;
                    //     EventManager.CallLogicUpdate();
                    //     break;
                    case MsgId.create_room_s2c or MsgId.join_room_s2c or MsgId.ready_s2c or MsgId.game_start_s2c
                        or MsgId.hide_indicator_s2c:
                        Request.Response(msg);
                        break;
                    case MsgId.JoinRoom:
                        Game.Instance.JoinRoom(GetPacketData<JoinRoom>(msg));
                        break;
                    case MsgId.GameStart:
                        Game.Instance.GameStart(GetPacketData<GameStart>(msg));
                        break;
                    case MsgId.PlayerLeft:
                        Game.Instance.PlayerLeft(GetPacketData<PlayerLeft>(msg));
                        break;
                    case MsgId.Ready:
                        Game.Instance.Ready(GetPacketData<Ready>(msg));
                        break;
                    case MsgId.ChangeStage:
                        Game.Instance.ChangeStage(GetPacketData<ChangeStage>(msg));
                        break;
                    case MsgId.HideIndicator:
                        Game.Instance.HideIndicator(GetPacketData<HideIndicator>(msg));
                        break;
                    // case KeepAlive:
                    //     SendMsg(new KeepAlive
                    //     {
                    //         Data = 1,
                    //     });
                    //     break;
                    default:
                        Debug.LogError("收到错误的消息: " + msg.GetType() + msg);
                        break;
                }
            }
        }


        public static JsonPacket WaitForSendMsg(CancellationToken cancellationToken)
        {
            waitingSendMsgSignal.Wait(cancellationToken);
            waitingSendMsgs.TryDequeue(out var result);
            return result;
        }

        public static JsonPacket GetWaitingDistributeMsg()
        {
#if OUTLINE_TEST
            logicUpdateMsgs.TryDequeue(out var result);
            return result;
#else
            lock (waitingDistributeMsgs)
            {
                waitingDistributeMsgs.TryDequeue(out var result);
                return result;
            }
#endif
        }


        //唯一指定发消息出口，禁止其他发消息方式
        public static void SendMsg(JsonPacket msg)
        {
            waitingSendMsgs.Enqueue(msg);
            waitingSendMsgSignal.Release();
        }

        public static void ReceiveMsg(NetworkStream stream)
        {
            // while (stream.DataAvailable)  //这个属性不可靠，微软的问题
            while (true)
            {
                var msg = Receive(stream);
                lock (waitingDistributeMsgs)
                {
                    waitingDistributeMsgs.Enqueue(msg);
                }
            }
        }


        // [MenuItem("Tools/test")]
        // public static void MyTest()
        // {
        //     Socket.Connect();
        // }
        //
        // [MenuItem("Tools/test1")]
        // public static void MyTest1()
        // {
        //     Socket.Disconnect();
        // }
    }
}
