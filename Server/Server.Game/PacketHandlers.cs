using System.Net;
using System.Text.Json;
using Server.Shared;
using Server.Network;
using System.Text;

namespace Server.Game;

public class PacketHandlers
{
    public void Handle(ClientConnection conn, PacketId id, byte[] body)
    {
        switch(id)
        {
            case PacketId.JoinLobby:
                _ = HandleJoinLobby(conn, body);
                break;
        }
    }

    private async Task HandleJoinLobby(ClientConnection conn , byte[] body)
    {
        string json = Encoding.UTF8.GetString(body);
        using JsonDocument doc = JsonDocument.Parse(json);
        string nickname = doc.RootElement.GetProperty("nickname").GetString();
        Console.WriteLine($"닉네임: {nickname}");

        var response = new {playerId=1, rooms = Array.Empty<string>() };
        string responseJson = JsonSerializer.Serialize(response);
        await conn.SendAsync(PacketId.LobbyJoined, responseJson);
    }
}