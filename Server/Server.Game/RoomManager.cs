using System;
using Server.Network;

namespace Server.Game;

public class RoomManager
{
    private List<Room> _rooms = new List<Room>();
    private int _nextRoomId = 1;

    private int _nextPlayerId = 1;

    private Dictionary<ClientConnection, Player> _playersByConnection = new Dictionary<ClientConnection, Player>();
    public Room CreateRoom(string roomName)
    {
        Room room = new Room
        {
            RoomId =  _nextRoomId++,
            RoomName = roomName
        };
        _rooms.Add(room);
        return room;
    }

    public Room? JoinRoom(Player player, int RoomId)
    {
        Room room = _rooms.Find(r => r.RoomId == RoomId);
        if (room == null)
        {
            return null;
        }
        room.Players.Add(player);
        player.CurrentRoom = room;
        return room;
    }

    public Player RegisterPlayer (ClientConnection conn, string nickname)
    {
        Player player = new Player
        {
            PlayerId = _nextPlayerId++,
            Nickname = nickname,
            Connection = conn,
            IsReady = false
        };

        _playersByConnection[conn] = player;
        return player;
    }

    public Player GetPlayer(ClientConnection conn)
    {
        _playersByConnection.TryGetValue(conn, out Player player);
        return player;
    }

}