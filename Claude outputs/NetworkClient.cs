using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Game.Network
{
    /// <summary>
    /// 서버와의 TCP 연결을 관리하는 싱글턴 컴포넌트.
    /// 백그라운드 스레드에서 패킷을 읽어 큐에 쌓고, Update()(메인 스레드)에서 안전하게 콜백을 실행한다.
    /// 씬에 빈 오브젝트 하나 만들어서 이 컴포넌트를 붙여두면 됨 (DontDestroyOnLoad로 씬 전환에도 유지).
    ///
    /// 사용 예:
    ///   NetworkClient.Instance.Connect();
    ///   NetworkClient.Instance.On(PacketId.LobbyJoined, json => { ... });
    ///   NetworkClient.Instance.Send(PacketId.JoinLobby, "{\"nickname\":\"범석\"}");
    /// </summary>
    public class NetworkClient : MonoBehaviour
    {
        public static NetworkClient Instance { get; private set; }

        [SerializeField] private string _host = "127.0.0.1";
        [SerializeField] private int _port = 9000;

        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private Thread _readThread;
        private volatile bool _running;

        private readonly ConcurrentQueue<(PacketId id, string json)> _incoming = new();
        private readonly Dictionary<PacketId, Action<string>> _handlers = new();

        public bool IsConnected => _tcpClient != null && _tcpClient.Connected;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            // 백그라운드 스레드에서 쌓인 패킷을 메인 스레드에서 안전하게 처리
            // (UnityEngine API는 메인 스레드에서만 호출 가능하기 때문에 이렇게 큐를 거침)
            while (_incoming.TryDequeue(out var packet))
            {
                if (_handlers.TryGetValue(packet.id, out var handler))
                {
                    handler.Invoke(packet.json);
                }
                else
                {
                    Debug.LogWarning($"[NetworkClient] 핸들러가 등록되지 않은 패킷: {packet.id}");
                }
            }
        }

        public void Connect(string host = null, int port = 0)
        {
            if (IsConnected)
            {
                Debug.LogWarning("[NetworkClient] 이미 연결되어 있습니다.");
                return;
            }

            if (!string.IsNullOrEmpty(host)) _host = host;
            if (port > 0) _port = port;

            try
            {
                _tcpClient = new TcpClient();
                _tcpClient.Connect(_host, _port);
                _stream = _tcpClient.GetStream();
                _running = true;

                _readThread = new Thread(ReadLoop) { IsBackground = true };
                _readThread.Start();

                Debug.Log($"[NetworkClient] {_host}:{_port} 연결됨");
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkClient] 연결 실패: {e.Message}");
            }
        }

        public void Disconnect()
        {
            _running = false;
            try { _stream?.Close(); } catch { /* ignore */ }
            try { _tcpClient?.Close(); } catch { /* ignore */ }
        }

        /// <summary>패킷ID에 대한 처리 콜백 등록. 같은 ID로 다시 등록하면 이전 콜백을 덮어씀.</summary>
        public void On(PacketId id, Action<string> handler)
        {
            _handlers[id] = handler;
        }

        public void Off(PacketId id)
        {
            _handlers.Remove(id);
        }

        /// <summary>jsonPayload는 이미 직렬화된 JSON 문자열이어야 함.</summary>
        public void Send(PacketId id, string jsonPayload)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[NetworkClient] 연결되어 있지 않아 전송할 수 없습니다.");
                return;
            }

            byte[] packet = PacketFramer.Encode(id, jsonPayload);
            try
            {
                _stream.Write(packet, 0, packet.Length);
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkClient] 전송 실패: {e.Message}");
            }
        }

        private void ReadLoop()
        {
            try
            {
                while (_running)
                {
                    byte[] header = ReadExact(PacketFramer.HeaderSize);
                    if (header == null) break;

                    uint length = PacketFramer.ReadUInt32BigEndian(header, 0);
                    ushort rawId = PacketFramer.ReadUInt16BigEndian(header, 4);

                    byte[] body = length > 0 ? ReadExact((int)length) : Array.Empty<byte>();
                    if (body == null) break;

                    string json = Encoding.UTF8.GetString(body);
                    _incoming.Enqueue(((PacketId)rawId, json));
                }
            }
            catch (Exception e)
            {
                if (_running) Debug.LogError($"[NetworkClient] 읽기 루프 오류: {e.Message}");
            }
            finally
            {
                Debug.Log("[NetworkClient] 연결 종료됨");
            }
        }

        /// <summary>정확히 count 바이트가 모일 때까지 반복해서 읽는다. 연결 종료 시 null.</summary>
        private byte[] ReadExact(int count)
        {
            byte[] buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = _stream.Read(buffer, offset, count - offset);
                if (read == 0) return null;
                offset += read;
            }
            return buffer;
        }

        private void OnApplicationQuit()
        {
            Disconnect();
        }
    }
}
