using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace ShushushaServer;

public class Server
{
    private int port;
    private IPAddress localAddr;
    private TcpListener server;
    private int maxClient = 1024;

    public Server(string ip, int port)
    {
        localAddr = IPAddress.Parse(ip);
        this.port = port;
        try
        {
            server = new TcpListener(localAddr, this.port);
            server.Start(maxClient);

            //接收终端输入
            Task.Run(ConsoleCommand);

            //循环接收连接请求并开启接收回复线程，没有请求时会阻塞
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                Task.Run(() => Dispacher.ReceiveRoomMsg(client));
                Console.WriteLine("Connected!");
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
        while (true)
        {
            var input = Console.ReadLine();
            if (input == "check")
            {
                Console.WriteLine("checking...");
            }
        }
    }
}
