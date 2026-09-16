using System;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Server.Shared;
namespace Server.Network;

public class ClientConnection
{
    private readonly TcpClient _tcpClient;
    private readonly NetworkStream _stream;

    public ClientConnection(TcpClient tcpClient)
    {
        _tcpClient = tcpClient;
        _stream = tcpClient.GetStream();
    }

    // 패킷 보내기
    public async Task SendAsync(PacketId id, string json)
    {
        byte[] packet = PacketFramer.Encode(id, json);
        try
        {
            await _stream.WriteAsync(packet, 0, packet.Length);
        }
        catch (Exception ex)
        {
           Console.Error.WriteLine(ex.Message);
        }
    }

    // 패킷 받기
    public async Task RunReceiveLoopAsync(Action<ClientConnection, PacketId, byte[]> onPacketReceive)
    {
        while (true)
        {
            try
            {
                var result = await PacketFramer.ReadPacketAsync(_stream);
                if(result == null) 
                {
                    break;
                }
                else
                {
                    onPacketReceive(this,result.Value.id,result.Value.body);
                }
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                break;
            }
        }
    }
}