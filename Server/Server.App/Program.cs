using Server.Network;
using Server.Shared;

byte[] packet = new PacketFramer().Encode(PacketId.JoinLobby, "{\"nickname\":\"범석\"}");
Console.WriteLine(string.Join(" ", packet.Select(b => b.ToString("X2"))));
