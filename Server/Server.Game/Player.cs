using System;
using Server.Network;

namespace Server.Game;
public class Player
{
    public int PlayerId { get; set; }
    public string Nickname {get; set; }
    public ClientConnection Connection { get; set; }
    public bool IsReady {get; set; }

    public Room? CurrentRoom {get; set;} 
}