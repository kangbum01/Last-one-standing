# 서버 프로젝트 시작 가이드 (구조 & 개념 — 코드는 직접 작성)

## 1. 솔루션/프로젝트 구성 (dotnet CLI)

`Server/` 폴더 안에서:

```
dotnet new sln -n GameServer

dotnet new console  -n Server.App
dotnet new classlib -n Server.Network
dotnet new classlib -n Server.Game
dotnet new classlib -n Server.Shared

dotnet sln add Server.App Server.Network Server.Game Server.Shared
```

참조 관계(누가 누구를 참조하는지)는 이렇게 잡으면 순환 참조 없이 깔끔합니다:

```
dotnet add Server.App     reference Server.Network
dotnet add Server.App     reference Server.Game
dotnet add Server.Network reference Server.Shared
dotnet add Server.Game    reference Server.Network
dotnet add Server.Game    reference Server.Shared
```

계층 흐름: `App → (Network, Game) → Shared`

- **Server.Shared**: 아무것도 참조하지 않는 가장 아래 계층. 패킷 ID 상수, DTO처럼 여러 곳에서 같이 쓰는 것만.
- **Server.Network**: 소켓/프레이밍만 담당. 게임 로직은 몰라야 함.
- **Server.Game**: 방/플레이어/라운드/증강 같은 도메인 로직. 패킷을 보내야 하니 Network를 참조.
- **Server.App**: 진입점(Program.cs). 설정 읽고 Network+Game을 조립해서 실행만.

## 2. TCP 리스너 & 패킷 프레이밍 — 핵심 개념

**(1) 연결 수락**
`TcpListener`를 포트(9000)에서 `Start()`하고 `AcceptTcpClientAsync()`로 새 연결을 계속 받습니다. 접속마다 별도로 처리해야 여러 명이 동시 접속 가능 — 보통 연결 하나당 `Task`(또는 async 루프) 하나를 붙입니다.

**(2) TCP는 스트림이다 — 메시지 경계가 없다**
`stream.Read()`를 한 번 호출한다고 우리가 보낸 만큼 정확히 오는 게 아닙니다 (10바이트를 보냈는데 3바이트만 먼저 도착할 수도 있음). 그래서 프로토콜에 **길이(4바이트) + 패킷ID(2바이트)** 헤더를 붙인 거예요. 즉 반드시:

1. 정확히 6바이트(헤더)가 모일 때까지 반복해서 읽기
2. 헤더에서 읽은 길이(N)만큼 정확히 모일 때까지 반복해서 읽기

라는 "정확히 N바이트 읽기" 루틴이 필요합니다. `mock_client.py`의 `recv_exact()` 함수가 파이썬으로 이 로직을 구현한 예시니 참고해서 C#으로 옮기시면 됩니다(개념은 동일, `NetworkStream.ReadAsync`를 반복 호출).

**(3) 받은 패킷을 분기(dispatch)하기**
패킷ID → 처리 함수로 연결하는 라우팅 테이블이 필요합니다. 예를 들면 `Dictionary<ushort, Action<ClientConnection, byte[]>>` 같은 구조에 `PacketIds.JOIN_LOBBY`를 키로 핸들러를 등록해두고, 패킷이 오면 ID로 찾아서 실행하는 방식. (switch문으로 시작해도 되고, 나중에 커지면 테이블 방식으로 리팩터링해도 됩니다.)

**(4) 보낼 때도 대칭**
JSON payload를 UTF-8 byte[]로 만들고, 그 길이(4바이트, 빅엔디안) + 패킷ID(2바이트, 빅엔디안)를 앞에 붙여서 하나의 byte[]로 만든 뒤 전송. `mock_client.py`의 `send_packet()`이 파이썬 버전 예시입니다.

## 3. 파일 단위로 나눠보면 (전부 TODO — 뼈대만)

```
Server.Shared/
  PacketIds.cs          게임_기획서.md 3.1 표와 동일한 값으로 enum 또는 const 정의

Server.Network/
  PacketFramer.cs        길이+ID+body 인코딩/디코딩 (recv_exact에 해당하는 로직 포함)
  ClientConnection.cs    TcpClient 하나를 감싸서 읽기 루프 + Send 메서드 제공
  TcpServer.cs           Listener 시작, Accept 루프, 연결마다 ClientConnection 생성

Server.Game/
  RoomManager.cs         방 생성/참가/퇴장/ready 상태 관리
  Player.cs              연결 정보 + 닉네임 + (나중에) 위치/체력/증강 목록
  PacketHandlers.cs       패킷ID별 실제 처리 로직 — TcpServer의 라우팅 테이블에 등록

Server.App/
  Program.cs             설정(host/port) 읽고 TcpServer 생성 후 실행
```

## 4. M1 완료 기준 (여기까지 되면 다음 마일스톤으로)

1. 서버를 실행하면 9000 포트에서 대기 상태가 됨
2. `mock_client.py`로 접속하면 서버 콘솔에 "클라이언트 접속됨" 같은 로그가 찍힘
3. `JOIN_LOBBY {"nickname": "범석"}`을 보내면 서버가 (최소한) 콘솔에 로그를 찍고, `LOBBY_JOINED` 패킷으로 아무 payload나 응답
4. `mock_client.py` 화면에 `[RECV] LOBBY_JOINED ...`가 뜨면 성공

여기까지 되면 리스너 + 프레이밍 + 라우팅의 기본 골격이 다 돈다는 뜻이라, 이후 로비/라운드/증강 로직은 이 골격 위에 패킷 핸들러만 추가해나가는 작업이 됩니다.
