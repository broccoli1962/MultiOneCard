using Backend.App;
using Backend.Net;
using Backend.Object.Management;
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

        private static string _pendingNick = "P0";
        private static string _pendingRoomCode = "000000";
        private static int _pendingSeatCount = DefaultSeatCount;
        private static bool _pendingIsHost = true;

        private LocalLoopback _loopback;
        private NetClient[] _clients;
        private int _localSeat;
        private bool _isHost;
        private string _status;
        private bool _rulesVisible;

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
        /// 루프백 대기실을 열고 입력을 구독한다.
        /// </summary>
        public override void OnOpen()
        {
            View.EnsureLayout();
            BindView();
            StartRoom();
        }

        /// <summary>
        /// 구독과 루프백을 해제한다.
        /// </summary>
        public override void OnClose()
        {
            UnbindView();
            StopRoom();
        }

        /// <summary>
        /// 호스트 시계를 한 번 돌린다. 화면은 판결하지 않는다.
        /// </summary>
        public void Tick()
        {
            if (_loopback == null || GameStateUtil.IsQuitting)
            {
                return;
            }

            _loopback.Pump();
        }

        private void BindView()
        {
            View.ReadyClicked += OnReadyClicked;
            View.StartClicked += OnStartClicked;
            View.RulesClicked += OnRulesClicked;
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
            View.BackClicked -= OnBackClicked;
            View.SlotClicked -= OnSlotClicked;
            if (View.Chat != null)
            {
                View.Chat.SendClicked -= OnChatSendClicked;
                View.Chat.QuickClicked -= OnQuickClicked;
            }
        }

        private void StartRoom()
        {
            StopRoom();
            var seatCount = _pendingSeatCount;
            _isHost = _pendingIsHost;
            _localSeat = _isHost || seatCount <= 1 ? 0 : 1;
            _status = "준비하세요";
            _rulesVisible = false;
            View.SetRulesVisible(false);
            View.Chat.ClearLog();
            View.Chat.ClearInput();

            var nicks = new string[seatCount];
            for (var i = 0; i < seatCount; i++)
            {
                nicks[i] = i == _localSeat ? _pendingNick : "P" + i;
            }

            var runtime = new MatchRuntime(seatCount, _pendingRoomCode.GetHashCode(), _pendingRoomCode, hostSeat: 0, nicks: nicks);
            _loopback = new LocalLoopback(runtime);
            _clients = new NetClient[seatCount];
            for (var seat = 0; seat < seatCount; seat++)
            {
                _clients[seat] = _loopback.CreateClient(seat);
            }

            var local = LocalClient();
            if (local != null)
            {
                local.EventReceived += OnNetEvent;
            }

            View.Chat.Append(ChatType.System, null, "대기실에 입장했습니다", null);
            LocalClient()?.RequestSnapshot();
            Refresh();
        }

        private void StopRoom()
        {
            if (_clients != null)
            {
                for (var i = 0; i < _clients.Length; i++)
                {
                    var client = _clients[i];
                    if (client == null)
                    {
                        continue;
                    }

                    client.EventReceived -= OnNetEvent;
                    if (client.IsConnected)
                    {
                        client.Disconnect();
                    }
                }
            }

            _clients = null;
            _loopback = null;
        }

        private void OnNetEvent(EventMessage ev)
        {
            if (ev == null)
            {
                return;
            }

            if (ev.ev == EvCode.Reject)
            {
                _status = ev.reject ?? "거절";
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
            }

            Refresh();
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

            var host = ClientAt(0);
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

        private void OnBackClicked()
        {
            UIManager.Close(View);
        }

        private void OnSlotClicked(int seat)
        {
            var client = ClientAt(seat);
            if (client == null)
            {
                return;
            }

            client.Ready();
            Refresh();
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
                nicks[i] = i == _localSeat ? _pendingNick : "P" + i;
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
            return ClientAt(_localSeat);
        }

        private NetClient ClientAt(int seat)
        {
            if (_clients == null || seat < 0 || seat >= _clients.Length)
            {
                return null;
            }

            return _clients[seat];
        }

        private static int ClampSeatCount(int seatCount)
        {
            if (seatCount < 2)
            {
                return 2;
            }

            return seatCount > 6 ? 6 : seatCount;
        }

        private static string NormalizeRoomCode(string roomCode)
        {
            if (string.IsNullOrEmpty(roomCode) || roomCode.Length != 6)
            {
                return RandomRoomCode();
            }

            return roomCode;
        }

        private static string RandomRoomCode()
        {
            return UnityEngine.Random.Range(0, 1000000).ToString("D6");
        }
    }
}
