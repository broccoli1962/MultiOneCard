using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Backend.Net
{
    /// <summary>
    /// 로컬 웹소켓으로 JSON 커맨드/이벤트를 주고받는 전송.
    /// PlayCard 는 instanceId 의도만 보낸다. 합법·승패는 판결하지 않는다.
    /// </summary>
    public sealed class WsNetClient : INetTransport
    {
        /// <summary>로컬 게이트웨이 기본 URL. 배포는 wss:// 를 쓴다.</summary>
        public const string DefaultLocalUrl = "ws://127.0.0.1:7777/ws";

        private const int MaxMessageBytes = 65536;

        private readonly string _baseUrl;
        private readonly int _seat;
        private readonly ConcurrentQueue<CommandMessage> _outbound = new ConcurrentQueue<CommandMessage>();

        private string _roomCode;
        private ClientWebSocket _socket;
        private CancellationTokenSource _cts;
        private Task _runTask;
        private int _nextSeq = 1;
        private volatile bool _connected;

        /// <summary>로컬 또는 지정 URL 로 붙는 클라를 만든다. 아직 Connect 전이다.</summary>
        public WsNetClient(string url = null, int seat = 0, string roomCode = null)
        {
            _baseUrl = string.IsNullOrEmpty(url) ? DefaultLocalUrl : url;
            _seat = seat;
            _roomCode = NormalizeRoomCode(roomCode) ?? "000000";
        }

        /// <summary>이 클라의 좌석.</summary>
        public int Seat => _seat;

        /// <summary>입장한 6자리 룸코드.</summary>
        public string RoomCode => _roomCode;

        /// <summary>소켓이 열려 있는지.</summary>
        public bool IsConnected
        {
            get { return _connected && _socket != null && _socket.State == WebSocketState.Open; }
        }

        /// <summary>이 좌석에 보이는 이벤트가 도착하면 발행한다.</summary>
        public event Action<EventMessage> EventReceived;

        /// <summary>로컬 게이트웨이에 붙는다. 룸코드는 쿼리로 보낸다.</summary>
        public void Connect()
        {
            if (_runTask != null && !_runTask.IsCompleted)
            {
                return;
            }

            _cts = new CancellationTokenSource();
            _runTask = RunAsync(_cts.Token);
        }

        /// <summary>소켓을 닫는다.</summary>
        public void Disconnect()
        {
            _connected = false;
            var cts = _cts;
            _cts = null;
            try
            {
                cts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }

            try
            {
                _socket?.Abort();
            }
            catch (Exception)
            {
            }

            cts?.Dispose();
            _socket = null;
        }

        /// <summary>6자리 룸코드로 입장한다. 이미 열려 있으면 다시 붙는다.</summary>
        public void JoinRoom(string roomCode)
        {
            var normalized = NormalizeRoomCode(roomCode);
            if (normalized == null)
            {
                return;
            }

            _roomCode = normalized;
            if (_runTask != null && !_runTask.IsCompleted)
            {
                Disconnect();
            }

            Connect();
        }

        /// <summary>커맨드 JSON 을 보낸다. 접속 전이면 큐에 쌓는다. 규칙은 보지 않는다.</summary>
        public void Send(CommandMessage command)
        {
            if (command == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(command.roomCode))
            {
                command.roomCode = _roomCode;
            }

            if (command.seat == 0 && _seat != 0)
            {
                command.seat = _seat;
            }

            _outbound.Enqueue(command);
        }

        /// <summary>PlayCard(instanceId) 의도만 보낸다. LegalMove/RuleEngine 를 호출하지 않는다.</summary>
        public int PlayCard(int instanceId)
        {
            var seq = _nextSeq;
            _nextSeq += 1;
            Send(CommandMessage.PlayCard(seq, _seat, instanceId));
            return seq;
        }

        /// <summary>6자리 숫자 룸코드면 그대로, 아니면 null.</summary>
        public static string NormalizeRoomCode(string roomCode)
        {
            if (string.IsNullOrEmpty(roomCode) || roomCode.Length != 6)
            {
                return null;
            }

            for (var i = 0; i < 6; i++)
            {
                if (roomCode[i] < '0' || roomCode[i] > '9')
                {
                    return null;
                }
            }

            return roomCode;
        }

        private async Task RunAsync(CancellationToken token)
        {
            var socket = new ClientWebSocket();
            _socket = socket;
            try
            {
                await socket.ConnectAsync(BuildUri(), token).ConfigureAwait(false);
                _connected = true;
                await Task.WhenAll(ReceiveLoopAsync(socket, token), SendLoopAsync(socket, token))
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
                _connected = false;
            }
            finally
            {
                _connected = false;
                if (ReferenceEquals(_socket, socket))
                {
                    _socket = null;
                }

                socket.Dispose();
            }
        }

        private Uri BuildUri()
        {
            var sep = _baseUrl.IndexOf('?') >= 0 ? "&" : "?";
            return new Uri(
                _baseUrl
                + sep
                + "room=" + _roomCode
                + "&seat=" + _seat.ToString()
                + "&major=" + ProtocolVersion.Major.ToString()
                + "&minor=" + ProtocolVersion.Minor.ToString());
        }

        private async Task SendLoopAsync(ClientWebSocket socket, CancellationToken token)
        {
            while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                if (!_outbound.TryDequeue(out var command))
                {
                    try
                    {
                        await Task.Delay(16, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }

                    continue;
                }

                var json = WireJson.SerializeCommand(command);
                var bytes = Encoding.UTF8.GetBytes(json);
                await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token)
                    .ConfigureAwait(false);
            }
        }

        private async Task ReceiveLoopAsync(ClientWebSocket socket, CancellationToken token)
        {
            var buffer = new byte[4096];
            var acc = new byte[MaxMessageBytes];
            var written = 0;
            while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (written + result.Count > MaxMessageBytes)
                {
                    break;
                }

                Buffer.BlockCopy(buffer, 0, acc, written, result.Count);
                written += result.Count;
                if (!result.EndOfMessage)
                {
                    continue;
                }

                var json = Encoding.UTF8.GetString(acc, 0, written);
                written = 0;
                var ev = WireJson.DeserializeEvent(json);
                if (ev != null)
                {
                    EventReceived?.Invoke(ev);
                }
            }
        }
    }
}
