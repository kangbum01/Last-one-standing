namespace Server.Shared;

public enum PacketId : ushort
{
    JoinLobby = 1,
    LobbyJoined = 2,
    CreateRoom = 3,
    JoinRoom = 4,
    RoomState = 5,
    Ready = 6,
    MatchConfig = 10,
    SelectStarter = 11,
    MatchStart = 12,
    RoundStart = 20,
    MoveInput = 21,
    StateSnapshot = 22,
    Attact = 23,
    AttactResult = 24,
    PlayerDeath = 25,
    RoundResult = 26,
    AugmentOffer = 30,
    AugmentPick = 31,
    BuildUpdate = 32,
    MatchEnd = 40,
    Ping = 41,
    Pong = 42,
    PlayerDisconnected = 43
}