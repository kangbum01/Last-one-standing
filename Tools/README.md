# Tools

## mock_client.py — 서버 단독 테스트용 목업 클라이언트

Unity 클라이언트가 준비되기 전에도 서버 로직(로비, 라운드, 증강 등)을 콘솔에서 바로 테스트할 수 있는 파이썬 스크립트입니다. Python이 설치되어 있으면 바로 실행할 수 있습니다 (별도 라이브러리 설치 불필요, 표준 라이브러리만 사용).

### 실행

```
python mock_client.py
```

또는 (환경에 따라)

```
python3 mock_client.py
```

실행하면 접속할 host/port를 물어봅니다. 그냥 엔터를 치면 기본값(127.0.0.1 / 9000)으로 접속합니다.

### 사용법

접속되면 `<패킷이름> <JSON payload>` 형식으로 입력해서 서버로 패킷을 보낼 수 있습니다.

```
> JOIN_LOBBY {"nickname": "범석"}
[SEND] JOIN_LOBBY (1) -> {'nickname': '범석'}

[RECV] LOBBY_JOINED (2) <- {'playerId': 1, 'rooms': []}
> CREATE_ROOM {"roomName": "테스트방"}
> READY {"ready": true}
> exit
```

서버가 보내는 패킷은 자동으로 수신되어 화면에 표시됩니다. 종료하려면 `exit`을 입력하세요.

패킷 이름과 ID는 `게임_기획서.md`의 "3.1 패킷 ID 값"과 동일하게 맞춰져 있습니다. **프로토콜 스펙이 바뀌면 `mock_client.py`의 `PACKET_IDS` 딕셔너리도 같이 업데이트해야 합니다.**
