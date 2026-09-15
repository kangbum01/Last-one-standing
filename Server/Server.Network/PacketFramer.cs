using System.Text.Json;
using System.Text;
using Server.Shared;
using System.Runtime.InteropServices;
using System.Data.Common;
namespace Server.Network;

public class PacketFramer
{
    // 소켓으로 쏘는 쪽 값을 바이트 화
    public static byte[] Encode(PacketId packetId, string jsonPayload)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(jsonPayload);
        byte[] packet = new byte[6 + bytes.Length];
        
        // 빅엔디안 방식으로 진행
        int length = bytes.Length;
        packet[0] = (byte)(length >> 24);
        packet[1] = (byte)(length >> 16);
        packet[2] = (byte)(length >> 8);
        packet[3] = (byte)length;

        ushort id = (ushort)packetId;
        packet[4] = (byte)(id >> 8);
        packet[5] = (byte)id;

        Array.Copy(bytes, 0, packet, 6, bytes.Length);

        return packet;
    }

    // 소켓에서 들어오는 바이트를 읽어내는 함수
    public static async Task<byte[]?> ReadExactAsync(Stream stream, int count)
    {
        byte[] buffer = new byte[count];
        int offset = 0;
        while( offset < count)
        {
            int read = await stream.ReadAsync(buffer, offset, count - offset);
            if (read == 0)
                return null;
            offset += read;
        }
        return buffer;
    }
    
    // 들어온 buffer 복원 함수 바이트로 구성된 값을 원래 값으로
    public static async Task<(PacketId id, byte[] body)?> ReadPacketAsync(Stream stream)
    {
        byte[]? header = await ReadExactAsync(stream, 6);
        if (header == null)
            return null;
        int length = (header[0] << 24) | (header[1] << 16) | (header[2] << 8) | header[3];
        ushort id = (ushort)((header[4] << 8) | header[5]);
        // Stream 특성 상 이미 읽은 내용을 다시 읽지 않는다. 그렇기에 length의 값이 body의 길이가 될 수 있는 거다.
        byte[]? body = await ReadExactAsync(stream, length);
        if (body == null)
            return null;
        
        return ((PacketId)id, body);
    }
}
