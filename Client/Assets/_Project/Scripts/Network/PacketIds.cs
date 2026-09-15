namespace Game.Network
{
    /// <summary>
    /// 서버 Server.Shared/PacketIds.cs와 반드시 동일하게 유지할 것.
    /// 프로토콜 스펙(게임_기획서.md 3.1 패킷 ID 값)이 바뀌면 여기도 같이 업데이트.
    /// </summary>
    public enum PacketId : ushort
    {
        // 로비
        JoinLobby = 1,
        LobbyJoined = 2,
        CreateRoom = 3,
        JoinRoom = 4,
        RoomState = 5,
        Ready = 6,

        // 매치 설정 / 캐릭터 선택
        MatchConfig = 10,
        SelectStarter = 11,
        MatchStart = 12,

        // 인게임(라운드 전투)
        RoundStart = 20,
        MoveInput = 21,
        StateSnapshot = 22,
        Attack = 23,
        AttackResult = 24,
        PlayerDeath = 25,
        RoundResult = 26,

        // 증강 선택
        AugmentOffer = 30,
        AugmentPick = 31,
        BuildUpdate = 32,

        // 매치 종료 / 기타
        MatchEnd = 40,
        Ping = 41,
        Pong = 42,
        PlayerDisconnected = 43,
    }
}
