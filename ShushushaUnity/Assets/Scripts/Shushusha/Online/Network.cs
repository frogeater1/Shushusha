using System;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using ShushushaServer;
using UnityEngine;

namespace ShushushaServer
{
    public static class Network
    {
        public static TcpClient client;
        private static string ip = "127.0.0.1";
        private static int port = 13000;

        public static void Connect()
        {
            if (client is { Connected: true })
            {
                return;
            }

            var cancellationTokenSource = new CancellationTokenSource();

            //这个事件在意外退出，ios，和uwp上不会触发,详见unity文档
            Application.quitting += () =>
            {
                client.Close();
                client = null;
                cancellationTokenSource.Cancel();
            };


            try
            {
                client = new TcpClient(ip, port);

                UniTask.RunOnThreadPool(() =>
                    {
                        while (true)
                        {
                            if (client is not { Connected: true })
                            {
                                Debug.Log("连接断开");
                                return;
                            }

                            //循环把待发送队列里的消息发出去
                            while (Dispatcher.GetWaitingSendMsg() is { } msg)
                            {
                                Debug.Log(msg.MsgId.ToString() + msg.Data);
                                Dispatcher.Send(client.GetStream(), msg);
                            }
                        }
                    }, cancellationToken:
                    cancellationTokenSource.Token).Forget();

                //接收消息并存到待分发队列
                UniTask.RunOnThreadPool(() =>
                    {
                        var stream = client.GetStream();
                        Dispatcher.ReceiveMsg(stream);
                    },
                    cancellationToken: cancellationTokenSource.Token).Forget();
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }

        public static void Disconnect()
        {
            if (client is not { Connected: true })
                return;

            client.Close();
            Debug.Log(client.Connected);
            client = null;
        }

        // private static void HeartBeatTimer()
        // {
        //     var t = new System.Timers.Timer();
        //     t.Interval = 60000;
        //     t.Elapsed += (sender, args) =>
        //     {
        //         if (client is not { Connected: true })
        //         {
        //             Debug.Log("连接断开");
        //             t.Close();
        //             return;
        //         }
        //
        //         var msg = new heart_beat_c2s { ResCode = 1 };
        //         Dispatcher.SendMsg(msg);
        //     };
        //     t.Enabled = true;
        // }
    }
}
