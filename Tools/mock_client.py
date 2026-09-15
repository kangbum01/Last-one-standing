"""
서버 단독 테스트용 목업 클라이언트

Unity 클라이언트가 준비되기 전, 서버 로직만 콘솔에서 테스트하기 위한 스크립트입니다.
게임_기획서.md의 통신 프로토콜(3.1 패킷 ID 값)과 반드시 동일하게 유지하세요 —
스펙이 바뀌면 이 파일의 PACKET_IDS도 같이 업데이트해야 합니다.

사용법:
    python mock_client.py
    host/port 입력 (엔터만 치면 기본값: 127.0.0.1 / 9000)
    이후 프롬프트에 "<패킷이름> <JSON payload>" 형식으로 입력

예시:
    > JOIN_LOBBY {"nickname": "범석"}
    > CREATE_ROOM {"roomName": "테스트방"}
    > READY {"ready": true}
    > exit    (종료)

패킷 포맷: [4바이트 payload 길이(빅엔디안)] + [2바이트 패킷 ID(빅엔디안)] + [UTF-8 JSON payload]
"""

import socket
import struct
import threading
import json

PACKET_IDS = {
    "JOIN_LOBBY": 1,
    "LOBBY_JOINED": 2,
    "CREATE_ROOM": 3,
    "JOIN_ROOM": 4,
    "ROOM_STATE": 5,
    "READY": 6,
    "MATCH_CONFIG": 10,
    "SELECT_STARTER": 11,
    "MATCH_START": 12,
    "ROUND_START": 20,
    "MOVE_INPUT": 21,
    "STATE_SNAPSHOT": 22,
    "ATTACK": 23,
    "ATTACK_RESULT": 24,
    "PLAYER_DEATH": 25,
    "ROUND_RESULT": 26,
    "AUGMENT_OFFER": 30,
    "AUGMENT_PICK": 31,
    "BUILD_UPDATE": 32,
    "MATCH_END": 40,
    "PING": 41,
    "PONG": 42,
    "PLAYER_DISCONNECTED": 43,
}
ID_TO_NAME = {v: k for k, v in PACKET_IDS.items()}


def send_packet(sock: socket.socket, packet_name: str, payload: dict) -> None:
    if packet_name not in PACKET_IDS:
        print(f"[오류] 알 수 없는 패킷 이름: {packet_name}")
        print(f"사용 가능한 패킷: {', '.join(PACKET_IDS.keys())}")
        return
    body = json.dumps(payload, ensure_ascii=False).encode("utf-8")
    header = struct.pack(">IH", len(body), PACKET_IDS[packet_name])
    sock.sendall(header + body)
    print(f"[SEND] {packet_name} ({PACKET_IDS[packet_name]}) -> {payload}")


def recv_exact(sock: socket.socket, n: int) -> bytes | None:
    buf = b""
    while len(buf) < n:
        chunk = sock.recv(n - len(buf))
        if not chunk:
            return None
        buf += chunk
    return buf


def listen_loop(sock: socket.socket) -> None:
    while True:
        header = recv_exact(sock, 6)
        if header is None:
            print("\n[연결이 종료되었습니다]")
            break
        length, pid = struct.unpack(">IH", header)
        body = recv_exact(sock, length) if length > 0 else b""
        name = ID_TO_NAME.get(pid, f"UNKNOWN({pid})")
        try:
            payload = json.loads(body.decode("utf-8")) if body else {}
        except json.JSONDecodeError:
            payload = body
        print(f"\n[RECV] {name} ({pid}) <- {payload}\n> ", end="", flush=True)


def main() -> None:
    host = input("서버 host (기본: 127.0.0.1): ").strip() or "127.0.0.1"
    port_str = input("서버 port (기본: 9000): ").strip() or "9000"
    port = int(port_str)

    sock = socket.create_connection((host, port))
    print(f"{host}:{port} 연결됨\n")

    threading.Thread(target=listen_loop, args=(sock,), daemon=True).start()

    print("사용 가능한 패킷:", ", ".join(PACKET_IDS.keys()))
    print('형식: <패킷이름> <JSON payload>  (예: JOIN_LOBBY {"nickname": "범석"})')
    print("payload 없이 보내려면 패킷 이름만 입력. 종료: exit\n")

    while True:
        try:
            line = input("> ").strip()
        except EOFError:
            break
        if line.lower() == "exit":
            break
        if not line:
            continue
        parts = line.split(" ", 1)
        packet_name = parts[0].upper()
        payload = {}
        if len(parts) > 1:
            try:
                payload = json.loads(parts[1])
            except json.JSONDecodeError as e:
                print(f"[오류] JSON 파싱 실패: {e}")
                continue
        send_packet(sock, packet_name, payload)

    sock.close()


if __name__ == "__main__":
    main()
