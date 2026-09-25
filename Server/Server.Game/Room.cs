using System;
using Server.Network;

namespace Server.Game;
public class Room
{
    public int RoomId {get; set;}
    public string RoomName {get; set; }
    public List<Player> Players {get; set; } = new List<Player>();
}