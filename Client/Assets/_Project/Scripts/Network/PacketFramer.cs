using System;
using System.Text;

namespace Game.Network
{
    /// <summary>
    /// 패킷 포맷: [4바이트 payload 길이(빅엔디안)] + [2바이트 패킷ID(빅엔디안)] + [UTF-8 JSON payload]
    /// 서버 Server.Network/PacketFramer.cs, mock_client.py와 동일한 프레이밍 규칙.
    /// Unity 의존성 없는 순수 C#이라 유닛 테스트도 가능.
    /// </summary>
    public static class PacketFramer
    {
        public const int HeaderSize = 6;

        public static byte[] Encode(PacketId packetId, string jsonPayload)
        {
            byte[] body = Encoding.UTF8.GetBytes(jsonPayload ?? "{}");
            byte[] packet = new byte[HeaderSize + body.Length];

            WriteUInt32BigEndian(packet, 0, (uint)body.Length);
            WriteUInt16BigEndian(packet, 4, (ushort)packetId);
            Buffer.BlockCopy(body, 0, packet, HeaderSize, body.Length);

            return packet;
        }

        public static void WriteUInt32BigEndian(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }

        public static void WriteUInt16BigEndian(byte[] buffer, int offset, ushort value)
        {
            buffer[offset] = (byte)(value >> 8);
            buffer[offset + 1] = (byte)value;
        }

        public static uint ReadUInt32BigEndian(byte[] buffer, int offset)
        {
            return ((uint)buffer[offset] << 24) | ((uint)buffer[offset + 1] << 16) |
                   ((uint)buffer[offset + 2] << 8) | buffer[offset + 3];
        }

        public static ushort ReadUInt16BigEndian(byte[] buffer, int offset)
        {
            return (ushort)((buffer[offset] << 8) | buffer[offset + 1]);
        }
    }
}
