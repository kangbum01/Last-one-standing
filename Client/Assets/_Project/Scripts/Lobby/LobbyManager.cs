using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Network;

namespace Game.Lobby
{
    // ===== 서버와 주고받는 JSON 페이로드 형태 =====
    // JsonUtility는 필드 이름이 JSON 키랑 정확히 똑같아야 인식하므로, 서버 스펙(camelCase)에 맞춰 작성했습니다.

    [System.Serializable]
    public class NicknameRequest
    {
        public string nickname;
    }

    [System.Serializable]
    public class RoomNameRequest
    {
        public string roomName;
    }

    [System.Serializable]
    public class RoomIdRequest
    {
        public int roomId;
    }

    [System.Serializable]
    public class LobbyJoinedPayload
    {
        public int playerId;
        public List<string> rooms;
    }

    [System.Serializable]
    public class RoomPlayerInfo
    {
        public int playerId;
        public string nickname;
        public bool isReady;
    }

    [System.Serializable]
    public class RoomStatePayload
    {
        public int roomId;
        public string roomName;
        public List<RoomPlayerInfo> players;
    }

    /// <summary>
    /// 로비 화면 UI와 서버 통신을 연결하는 컨트롤러.
    /// 버튼을 누르면 알맞은 패킷을 서버로 보내고, 서버 응답(LOBBY_JOINED, ROOM_STATE)을 받으면 UI를 갱신한다.
    ///
    /// 씬 설정 방법은 Tools/README.md 또는 대화 내역 참고. 요약하면:
    ///   1) 빈 오브젝트에 NetworkClient 컴포넌트 부착 (씬에 하나만)
    ///   2) 빈 오브젝트에 이 LobbyManager 컴포넌트 부착
    ///   3) Canvas 안에 InputField(TMP)/Button/Text(TMP)들을 만들고, 아래 [SerializeField] 필드에 인스펙터에서 드래그해서 연결
    ///
    /// 주의: Unity 6에서 GameObject/UI 메뉴로 만든 InputField, Text는 기본적으로 TextMeshPro(TMP) 버전이라서
    /// 이 스크립트도 UnityEngine.UI.InputField/Text가 아니라 TMPro.TMP_InputField/TMP_Text를 사용한다.
    /// (Button은 TMP 유무와 상관없이 컴포넌트가 동일해서 UnityEngine.UI.Button 그대로 사용)
    /// </summary>
    public class LobbyManager : MonoBehaviour
    {
        [Header("서버 연결 설정")]
        [SerializeField] private string _host = "127.0.0.1";
        [SerializeField] private int _port = 9000;

        [Header("로비 참가 UI")]
        [SerializeField] private TMP_InputField _nicknameInput;
        [SerializeField] private Button _joinLobbyButton;

        [Header("방 생성/참가 UI")]
        [SerializeField] private TMP_InputField _roomNameInput;
        [SerializeField] private Button _createRoomButton;
        [SerializeField] private TMP_InputField _roomIdInput;
        [SerializeField] private Button _joinRoomButton;

        [Header("방 안 UI")]
        [SerializeField] private Button _readyButton;
        [SerializeField] private TMP_Text _roomInfoText;

        [Header("상태 메시지")]
        [SerializeField] private TMP_Text _statusText;

        private int _myPlayerId = -1;

        private void Start()
        {
            NetworkClient.Instance.Connect(_host, _port);

            NetworkClient.Instance.On(PacketId.LobbyJoined, OnLobbyJoined);
            NetworkClient.Instance.On(PacketId.RoomState, OnRoomState);

            _joinLobbyButton.onClick.AddListener(OnClickJoinLobby);
            _createRoomButton.onClick.AddListener(OnClickCreateRoom);
            _joinRoomButton.onClick.AddListener(OnClickJoinRoom);
            _readyButton.onClick.AddListener(OnClickReady);
        }

        private void OnClickJoinLobby()
        {
            string nickname = _nicknameInput.text.Trim();
            if (string.IsNullOrEmpty(nickname))
            {
                SetStatus("닉네임을 입력해주세요.");
                return;
            }

            string json = JsonUtility.ToJson(new NicknameRequest { nickname = nickname });
            NetworkClient.Instance.Send(PacketId.JoinLobby, json);
        }

        private void OnClickCreateRoom()
        {
            string roomName = _roomNameInput.text.Trim();
            if (string.IsNullOrEmpty(roomName))
            {
                SetStatus("방 이름을 입력해주세요.");
                return;
            }

            string json = JsonUtility.ToJson(new RoomNameRequest { roomName = roomName });
            NetworkClient.Instance.Send(PacketId.CreateRoom, json);
        }

        private void OnClickJoinRoom()
        {
            if (!int.TryParse(_roomIdInput.text.Trim(), out int roomId))
            {
                SetStatus("방 번호를 숫자로 입력해주세요.");
                return;
            }

            string json = JsonUtility.ToJson(new RoomIdRequest { roomId = roomId });
            NetworkClient.Instance.Send(PacketId.JoinRoom, json);
        }

        private void OnClickReady()
        {
            // 서버는 이 패킷이 왔다는 사실 자체를 "토글하라"는 신호로만 사용하고 payload 내용은 보지 않는다.
            NetworkClient.Instance.Send(PacketId.Ready, "{}");
        }

        private void OnLobbyJoined(string json)
        {
            LobbyJoinedPayload payload = JsonUtility.FromJson<LobbyJoinedPayload>(json);
            _myPlayerId = payload.playerId;
            SetStatus($"로비 참가 완료 (내 playerId: {_myPlayerId})");
        }

        private void OnRoomState(string json)
        {
            RoomStatePayload payload = JsonUtility.FromJson<RoomStatePayload>(json);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"방: {payload.roomName} (ID: {payload.roomId})");
            foreach (RoomPlayerInfo player in payload.players)
            {
                string readyMark = player.isReady ? "[준비 완료]" : "[대기중]";
                string me = player.playerId == _myPlayerId ? " (나)" : "";
                sb.AppendLine($"- {player.nickname}{me} {readyMark}");
            }

            if (_roomInfoText != null) _roomInfoText.text = sb.ToString();
            SetStatus($"ROOM_STATE 수신 (방 {payload.roomId})");
        }

        private void SetStatus(string message)
        {
            if (_statusText != null) _statusText.text = message;
            Debug.Log($"[LobbyManager] {message}");
        }
    }
}
