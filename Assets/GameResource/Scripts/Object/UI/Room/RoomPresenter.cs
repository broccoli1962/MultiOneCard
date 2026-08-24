using System;
using Backend.App;
using Backend.Net;
using Backend.Object.Management;
using Backend.Object.Net;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Backend.Object.UI
{
    /// <summary>
    /// 대기실 세션. 준비·시작·채팅은 <see cref="NetClient"/> 커맨드만 보낸다.
    /// </summary>
    public sealed class RoomPresenter : UIPresenter<RoomPanel>
    {
        private const int DefaultSeatCount = 6;
        private const int ChatMaxChars = 80;
        private const int LanEventTimeoutMs = 5000;
        private const int RelayEventTimeoutMs = 20000;

        private static string _pendingNick = "P0";
        private static string _pendingRoomCode = "000000";
        private static int _pendingSeatCount = DefaultSeatCount;
        private static bool _pendingIsHost = true;

        private PlayClientTransport _playGuest;
        private NetClient _client;
        private int _localSeat;
        private bool _isHost;
        private bool _handedToMatch;
        private bool _seatConfirmed;
        private bool _gotEvent;
        private bool _transportReady;
        private long _connectStartedMs;
        private string _status;
        private bool _rulesVisible;
        private bool _chatVisible;

        /// <summary>
        /// 로비에서 대기실을 열기 전 세션 인자를 넣는다.
        /// </summary>
        public static void Prepare(string nick, string roomCode, int seatCount, bool isHost)
        {
            _pendingNick = string.IsNullOrEmpty(nick) ? "P0" : nick;
            _pendingRoomCode = NormalizeRoomCode(roomCode);
            _pendingSeatCount = ClampSeatCount(seatCount);
            _pendingIsHost = isHost;
        }

        /// <summary>
        /// 로컬 게이트웨이에 붙고 입력을 구독한다.
        /// </summary>
        public override void OnOpen()
        {
            View.EnsureLayout();
            BindView();
            StartRoomAsync().Forget();
        }

        /// <summary>
        /// 구독과 소켓을 해제한다.
        /// </summary>
        public override void OnClose()
        {
            UnbindView();
            StopRoom();
            if (!_handedToMatch)
            {
                PlaySession.Stop();
            }
        }

        /// <summary>
        /// 수신 이벤트를 메인 스레드에서 적용한다. 화면은 판결하지 않는다.
        /// </summary>
        public void Tick()
        {
            if (GameStateUtil.IsQuitting)
            {
                return;
            }

            PlaySession.Pump();
            if (!_transportReady || _gotEvent || _connectStartedMs <= 0)
            {
                return;
            }

            var waited = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _connectStartedMs;
            var limit = GatewaySettings.Mode == ConnectionMode.Relay ? RelayEventTimeoutMs : LanEventTimeoutMs;
            if (waited > limit && _status != null && _status.IndexOf("연결할 수 없음", StringComparison.Ordinal) < 0)
            {
                _status = "서버에 연결할 수 없음  " + ConnectHint();
                Refresh();
            }
        }

        private void BindView()
        {
            View.ReadyClicked += OnReadyClicked;
            View.StartClicked += OnStartClicked;
            View.RulesClicked += OnRulesClicked;
            View.ChatClicked += OnChatClicked;
            View.BackClicked += OnBackClicked;
            View.SlotClicked += OnSlotClicked;
            if (View.Chat != null)
            {
                View.Chat.SendClicked += OnChatSendClicked;
                View.Chat.QuickClicked += OnQuickClicked;
            }
        }

        private void UnbindView()
        {
            if (View == null)
            {
                return;
            }

            View.ReadyClicked -= OnReadyClicked;
            View.StartClicked -= OnStartClicked;
            View.RulesClicked -= OnRulesClicked;
            View.ChatClicked -= OnChatClicked;
            View.BackClicked -= OnBackClicked;
            View.SlotClicked -= OnSlotClicked;
            if (View.Chat != null)
            {
                View.Chat.SendClicked -= OnChatSendClicked;
                View.Chat.QuickClicked -= OnQuickClicked;
            }
        }

        private async UniTaskVoid StartRoomAsync()
        {
            StopRoom();
            _handedToMatch = false;
            _isHost = _pendingIsHost;
            _localSeat = _isHost ? 0 : -1;
            _seatConfirmed = false;
            _transportReady = false;
            _status = GatewaySettings.Mode == ConnectionMode.Relay ? "릴레이 연결 중" : "서버에 연결 중";
            _rulesVisible = false;
            _chatVisible = false;
            View.SetRulesVisible(false);
            View.SetChatVisible(false);
            if (View.Chat != null)
            {
                View.Chat.ClearLog();
                View.Chat.ClearInput();
            }
            _connectStartedMs = 0;
            Refresh();

            try
            {
                await AttachTransportAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[RoomPresenter] Connect failed: {e}");
                _status = FormatConnectError(e.Message);
                Refresh();
                return;
            }

            _client.EventReceived += OnNetEvent;
            _client.Connect();

            if (!_isHost && _playGuest != null)
            {
                await WaitForGuestTransportAsync(_playGuest);
                await UniTask.DelayFrame(2);
            }

            SendInitialSnapshot();

            _transportReady = true;
            _connectStartedMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var lanHint = GatewaySettings.Mode == ConnectionMode.Lan && _isHost
                ? "  IP " + PlaySession.LocalIpv4()
                : string.Empty;
            View.Chat.Append(ChatType.System, null, "대기실에 입장했습니다" + lanHint, null);
            Refresh();
        }

        private async UniTask AttachTransportAsync()
        {
            var mode = GatewaySettings.Mode;
            var seats = SessionLimits.ClampPlayers(_pendingSeatCount);
            if (_isHost)
            {
                _client = await PlaySession.StartHostAsync(mode, _pendingNick, _pendingRoomCode, seats);
                _localSeat = 0;
                _seatConfirmed = true;
                if (mode == ConnectionMode.Relay
                    && !string.IsNullOrEmpty(UgsLobbyRelay.HostedJoinCode))
                {
                    _pendingRoomCode = UgsLobbyRelay.HostedJoinCode;
                }
                else if (_client?.Room != null && !string.IsNullOrEmpty(_client.Room.roomCode))
                {
                    _pendingRoomCode = _client.Room.roomCode;
                }

                return;
            }

            _playGuest = await PlaySession.StartGuestAsync(mode, _pendingNick, _pendingRoomCode);
            _client = new NetClient(_playGuest, 0);
        }

        private void StopRoom()
        {
            if (_client != null)
            {
                _client.EventReceived -= OnNetEvent;
                if (_client.IsConnected)
                {
                    _client.Disconnect();
                }
            }

            _client = null;
            _playGuest = null;
            _connectStartedMs = 0;
            _transportReady = false;
            _seatConfirmed = false;
            _gotEvent = false;
        }

        private void SendInitialSnapshot()
        {
            if (_client == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(_pendingNick))
            {
                var snap = CommandMessage.SnapshotRequest(0, _localSeat < 0 ? 0 : _localSeat);
                snap.text = _pendingNick;
                _client.Send(snap);
                return;
            }

            _client.RequestSnapshot();
        }

        private static async UniTask WaitForGuestTransportAsync(PlayClientTransport transport)
        {
            var deadline = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + RelayEventTimeoutMs;
            while (transport != null && !transport.IsConnected)
            {
                if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() >= deadline)
                {
                    throw new InvalidOperationException("릴레이 NGO 연결 시간 초과");
                }

                await UniTask.Yield();
            }
        }

        private static string FormatConnectError(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return "연결 실패";
            }

            if (message.IndexOf("Cloud", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return message;
            }

            if (message.IndexOf("방을 찾을 수 없음", StringComparison.Ordinal) >= 0)
            {
                return "방을 찾을 수 없음. 호스트 대기실에 표시된 방 코드를 그대로 입력하세요";
            }

            return message;
        }

        private static string FormatReject(string reject)
        {
            if (string.IsNullOrEmpty(reject))
            {
                return "거절";
            }

            if (reject == Backend.Net.RejectCode.NotAllReady)
            {
                return "접속한 전원 준비가 필요합니다";
            }

            if (reject == Backend.Net.RejectCode.NotYourTurn)
            {
                return "방장만 시작할 수 있습니다";
            }

            if (reject == Backend.Net.RejectCode.MatchAlreadyStarted)
            {
                return "이미 시작된 판입니다";
            }

            return reject;
        }

        private void OnNetEvent(EventMessage ev)
        {
            if (ev == null)
            {
                return;
            }

            _gotEvent = true;
            ConfirmSeat(ev);

            if (ev.ev == EvCode.Reject)
            {
                _status = FormatReject(ev.reject);
                Refresh();
                return;
            }

            if (ev.ev == EvCode.Chat)
            {
                var nick = ResolveNick(ev.seat);
                View.Chat.Append(ev.chatType, nick, ev.text, ev.quickId);
            }

            if (ev.ev == EvCode.PlayerDisconnected)
            {
                View.Chat.Append(ChatType.System, null, ResolveNick(ev.seat) + " 연결 끊김", null);
            }

            if (ev.ev == EvCode.PlayerRejoined)
            {
                View.Chat.Append(ChatType.System, null, ResolveNick(ev.seat) + " 재접속", null);
            }

            if (ev.ev == EvCode.MatchStarted)
            {
                _status = "시작";
                OpenMatchTable();
                return;
            }

            if (ev.room != null)
            {
                _status = "준비하세요";
                _isHost = ev.room.hostSeat == _localSeat;
            }

            Refresh();
        }

        private void ConfirmSeat(EventMessage ev)
        {
            if (_seatConfirmed || ev == null || ev.seat < 0)
            {
                return;
            }

            var fromSnapshot = ev.ev == EvCode.RoomUpdated && ev.ackSeq > 0;
            var fromHostPush = ev.ev == EvCode.RoomUpdated && ev.room != null && !_isHost;
            if (!fromSnapshot && !fromHostPush)
            {
                return;
            }

            _localSeat = ev.seat;
            _client?.AssignSeat(_localSeat);
            _seatConfirmed = true;
            if (ev.room != null)
            {
                _isHost = ev.room.hostSeat == _localSeat;
            }
        }

        private void OpenMatchTable()
        {
            if (_client == null)
            {
                return;
            }

            _client.EventReceived -= OnNetEvent;
            var client = _client;
            var localSeat = _localSeat < 0 ? 0 : _localSeat;
            _client = null;
            _playGuest = null;
            _handedToMatch = true;
            MatchPresenter.PrepareRemote(client, localSeat);
            UIManager.OpenAsync<MatchPanel>().Forget();
            if (View != null)
            {
                UIManager.Close(View);
            }
        }

        private void OnReadyClicked()
        {
            var client = LocalClient();
            if (client == null)
            {
                return;
            }

            client.Ready();
            Refresh();
        }

        private void OnStartClicked()
        {
            if (!_isHost)
            {
                _status = "방장만 시작";
                Refresh();
                return;
            }

            var host = LocalClient();
            if (host == null)
            {
                return;
            }

            host.StartMatch();
            Refresh();
        }

        private void OnRulesClicked()
        {
            _rulesVisible = !_rulesVisible;
            View.SetRulesVisible(_rulesVisible);
        }

        private void OnChatClicked()
        {
            _chatVisible = !_chatVisible;
            View.SetChatVisible(_chatVisible);
        }

        private void OnBackClicked()
        {
            UIManager.Close(View);
        }

        private void OnSlotClicked(int seat)
        {
            _ = seat;
        }

        private void OnChatSendClicked(string text)
        {
            var body = text != null ? text.Trim() : string.Empty;
            if (body.Length == 0)
            {
                _status = "빈 채팅";
                Refresh();
                return;
            }

            if (body.Length > ChatMaxChars)
            {
                body = body.Substring(0, ChatMaxChars);
            }

            SendChat(body, null);
        }

        private void OnQuickClicked(string quickId)
        {
            if (string.IsNullOrEmpty(quickId))
            {
                return;
            }

            SendChat(string.Empty, quickId);
        }

        private void SendChat(string text, string quickId)
        {
            var client = LocalClient();
            if (client == null)
            {
                return;
            }

            client.Chat(text, ChatChannel.Room, quickId);
            View.Chat.ClearInput();
            Refresh();
        }

        private void Refresh()
        {
            if (View == null || GameStateUtil.IsQuitting)
            {
                return;
            }

            var room = LocalClient() != null ? LocalClient().Room : null;
            if (room == null)
            {
                room = new RoomView
                {
                    roomCode = _pendingRoomCode,
                    phase = MatchPhase.Waiting,
                    nicks = BuildFallbackNicks(),
                    ready = new bool[_pendingSeatCount],
                    hostSeat = 0,
                    seatCount = _pendingSeatCount,
                };
            }

            View.Render(room, _localSeat, _isHost, _status);
        }

        private string[] BuildFallbackNicks()
        {
            var nicks = new string[_pendingSeatCount];
            for (var i = 0; i < nicks.Length; i++)
            {
                nicks[i] = i == _localSeat ? _pendingNick : string.Empty;
            }

            return nicks;
        }

        private string ResolveNick(int seat)
        {
            var room = LocalClient() != null ? LocalClient().Room : null;
            if (room != null && room.nicks != null && seat >= 0 && seat < room.nicks.Length)
            {
                return room.nicks[seat];
            }

            return "P" + seat;
        }

        private NetClient LocalClient()
        {
            return _client;
        }

        private static string ConnectHint()
        {
            if (GatewaySettings.Mode == ConnectionMode.Lan)
            {
                return "랜 " + GatewaySettings.LanHost;
            }

            return "릴레이";
        }

        private static int ClampSeatCount(int seatCount)
        {
            return SessionLimits.ClampPlayers(seatCount);
        }

        private static string NormalizeRoomCode(string roomCode)
        {
            if (string.IsNullOrEmpty(roomCode))
            {
                return RandomRoomCode();
            }

            return roomCode.Trim();
        }

        private static string RandomRoomCode()
        {
            return UnityEngine.Random.Range(0, 1000000).ToString("D6");
        }
    }
}
