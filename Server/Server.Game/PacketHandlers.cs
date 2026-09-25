using System.Net;
using System.Text.Json;
using Server.Shared;
using Server.Network;
using System.Text;

namespace Server.Game;

public class PacketHandlers
{
    private readonly RoomManager _roomManager;

    public PacketHandlers(RoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    private JsonElement ParseBody(byte[] body)
    {
        string json = Encoding.UTF8.GetString(body);
        using JsonDocument doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    public void Handle(ClientConnection conn, PacketId id, byte[] body)
    {
        switch(id)
        {
            case PacketId.JoinLobby:
                _ = HandleJoinLobby(conn, body);
                break;
            
            case PacketId.CreateRoom:
            {
                _ = HandleCreateRoom(conn, body);
                break;
            }
            case PacketId.JoinRoom:
            {
                _ = HandleJoinRoom(conn, body);
                break;
            }
        }
    }

    private async Task HandleJoinRoom(ClientConnection conn, byte[] body)
    {
        Player player = _roomManager.GetPlayer(conn);
        JsonElement root = ParseBody(body);
        int roomid = root.GetProperty("roomId").GetInt32();
        Room room = _roomManager.JoinRoom(player,roomid);
        if (room == null)
        {
            return;
        }
        var response = new
        {
            roomId = room.RoomId,
            roomName = room.RoomName,
            players = room.Players.Select(p => new {playerId = p.PlayerId, nickname = p.Nickname}) 
        };
        string responseJson = JsonSerializer.Serialize(response);
        await conn.SendAsync(PacketId.RoomState, responseJson);
    }



    private async Task HandleJoinLobby(ClientConnection conn , byte[] body)
    {
        JsonElement root = ParseBody(body);
        string nickname = root.GetProperty("nickname").GetString();
        Console.WriteLine($"닉네임: {nickname}");
        Player player = _roomManager.RegisterPlayer(conn,nickname);
        var response = new {playerId=player.PlayerId, rooms = Array.Empty<string>() };
        string responseJson = JsonSerializer.Serialize(response);
        await conn.SendAsync(PacketId.LobbyJoined, responseJson);
    }

    private async Task HandleCreateRoom(ClientConnection conn, byte[] body)
    {
        Player player = _roomManager.GetPlayer(conn);
        JsonElement root = ParseBody(body);
        string roomName = root.GetProperty("roomName").GetString();
        Room room = _roomManager.CreateRoom(roomName);
        _ = _roomManager.JoinRoom(player, room.RoomId);
        var response = new
        {
            roomId = room.RoomId,
            roomName = room.RoomName,
            players = room.Players.Select(p => new {playerId = p.PlayerId, nickname = p.Nickname}) 
        };
        string responseJson = JsonSerializer.Serialize(response);
        await conn.SendAsync(PacketId.RoomState, responseJson);
    }
}