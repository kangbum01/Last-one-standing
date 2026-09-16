using Server.Shared;
using System.Net.Sockets;
using System.Net;
using Server.Network;


public class TcpServer
{

    private readonly int _port;

    public Action<ClientConnection, PacketId, byte[]> OnPacketReceived;
    
    public TcpServer(int port)
    {
        _port = port;
    }

    public async Task StartAsync()
    {
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();

        // 접속 대기
        while(true)
        {
            try
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                ClientConnection connection = new ClientConnection(client);
                _ = connection.RunReceiveLoopAsync(OnPacketReceived);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        } 
    }
}