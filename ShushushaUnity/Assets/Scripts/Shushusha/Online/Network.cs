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
        private static CancellationTokenSource cancellationTokenSource;

        public static void Connect()
        {
            if (client is { Connected: true })
            {
                return;
            }

            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            //这个事件在意外退出，ios，和uwp上不会触发,详见unity文档
            Application.quitting += () =>
            {
                client?.Close();
                client = null;
                cancellationTokenSource.Cancel();
            };


            try
            {
                client = new TcpClient(ip, port);
                var connectedClient = client;
                var stream = connectedClient.GetStream();

                UniTask.RunOnThreadPool(() =>
                    {
                        try
                        {
                            while (true)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                if (!connectedClient.Connected)
                                {
                                    Debug.Log("连接断开");
                                    return;
                                }

                                var msg = Dispatcher.WaitForSendMsg(cancellationToken);
                                if (msg == null)
                                {
                                    continue;
                                }

                                Debug.Log(msg.MsgId.ToString() + msg.Data);
                                Dispatcher.Send(stream, msg);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                        }
                    }, cancellationToken:
                    cancellationToken).Forget();

                //接收消息并存到待分发队列
                UniTask.RunOnThreadPool(() =>
                    {
                        Dispatcher.ReceiveMsg(stream);
                    },
                    cancellationToken: cancellationToken).Forget();
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
            cancellationTokenSource?.Cancel();
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
