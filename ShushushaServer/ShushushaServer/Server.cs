using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ShushushaServer;

public class Server
{
    private int port;
    private IPAddress localAddr;
    private TcpListener server;
    private int maxClient = 1024;
    private CancellationTokenSource cts = new CancellationTokenSource();

    public Server(string ip, int port)
    {
        localAddr = IPAddress.Parse(ip);
        this.port = port;
        try
        {
            server = new TcpListener(localAddr, this.port);
            server.Start(maxClient);

            // 处理Ctrl+C
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                server.Stop();
            };

            //接收终端输入
            Task.Run(ConsoleCommand);

            //循环接收连接请求并开启接收回复线程，没有请求时会阻塞
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = server.AcceptTcpClient();
                    Task.Run(() => 
                    {
                        using (client)  // 确保TcpClient被正确释放
                        {
                            Dispatcher.ReceiveRoomMsg(client);
                        }
                    });
                    Console.WriteLine("Connected!");
                }
                catch (SocketException) when (cts.Token.IsCancellationRequested)
                {
                    // 取消时退出
                    break;
                }
                catch (SocketException)
                {
                    // 其他socket错误
                    Console.WriteLine("Socket error occurred.");
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.StackTrace);
            throw;
        }
    }

    private void ConsoleCommand()
    {
        while (!cts.Token.IsCancellationRequested)
        {
            var input = Console.ReadLine();
            if (input == "check")
            {
                Console.WriteLine("checking...");
            }
        }
    }
}
