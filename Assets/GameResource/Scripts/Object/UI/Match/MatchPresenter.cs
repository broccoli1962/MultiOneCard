using System.Collections.Generic;
using Backend.App;
using Backend.Net;
using Backend.Object.Management;
using Backend.Object.Net;
using Cysharp.Threading.Tasks;

namespace Backend.Object.UI
{
    /// <summary>
    /// 로컬 핫시트 매치. <see cref="NetClient"/> 로 의도만 보내고 규칙은 판결하지 않는다.
    /// 대기실에서 넘긴 루프백이 있으면 그 판을 이어 받고, 없으면 2인 더미를 연다.
    /// 현재 턴 좌석 손패만 표시한다.
    /// </summary>
    public sealed class MatchPresenter : UIPresenter<MatchPanel>
    {
        private const int DummySeatCount = 2;
        private const int DummySeed = 1;

        private static LocalLoopback _pendingLoopback;
        private static NetClient[] _pendingClients;
        private static NetClient _pendingRemote;
        private static int _pendingLocalSeat;

        private readonly GamePointer _pointer = new GamePointer();

        private LocalLoopback _loopback;
        private NetClient[] _clients;
        private bool _remote;
        private int _localSeat;
        private int _seatCount = DummySeatCount;
        private GamePointerInput _pointerInput;
        private int _viewSeat;
        private int _selectedPlayId = -1;
        private string _lastSentOp;
        private string _lastPlayedDefId;
        private MatchPrompt _prompt;
        private string _status;
        private string _lastPlay;
        private int _lastActSeat = -1;
        private string _result;
        private bool _surrenderArmed;
        private bool _resultOpened;
        private ResultPanel _resultPanel;
        private bool _rematchWaiting;
        private bool _handedToRoom;
        private bool _hostClosed;
        private int _mirrorTarget;
        private bool _chatVisible;
        private int _lastChatSeq = int.MinValue;
        private int _hoverPreviewId = -1;
        private bool _queenReceiveTravelArmed;

        /// <summary>
        /// 대기실 루프백을 테이블에 넘긴다. Open 전에 호출한다.
        /// </summary>
        public static void Prepare(LocalLoopback loopback, NetClient[] clients)
        {
            _pendingLoopback = loopback;
            _pendingClients = clients;
            _pendingRemote = null;
            _pendingLocalSeat = 0;
        }

        /// <summary>
        /// 게이트웨이에 붙은 내 좌석만 테이블에 넘긴다. Open 전에 호출한다.
        /// </summary>
        public static void PrepareRemote(NetClient client, int localSeat)
        {
            _pendingRemote = client;
            _pendingLocalSeat = localSeat;
            _pendingLoopback = null;
            _pendingClients = null;
        }

        /// <summary>
        /// 루프백 호스트를 열고 입력을 NetClient 커맨드로만 보낸다.
        /// </summary>
        public override void OnOpen()
        {
            View.EnsureLayout();
            BindView();
            BindPointer();
            _chatVisible = false;
            _lastChatSeq = int.MinValue;
            View.SetChatVisible(false);
            if (View.Chat != null)
            {
                View.Chat.ClearLog();
                View.Chat.ClearInput();
            }
            if (_pendingRemote != null)
            {
                AttachRemote();
                return;
            }

            if (_pendingLoopback != null && _pendingClients != null)
            {
                AttachPending();
                return;
            }

            StartHotseat();
        }

        /// <summary>
        /// 구독과 루프백을 해제한다.
        /// </summary>
        public override void OnClose()
        {
            UnbindPointer();
            UnbindView();
            ReleaseHand();
            var keepSession = _handedToRoom;
            StopHotseat(disconnect: !keepSession);
            if (!keepSession)
            {
                PlaySession.Stop();
            }
        }

        /// <summary>
        /// 턴 초과·유예를 호스트에 넘긴다. 화면은 판결하지 않는다.
        /// </summary>
        public void Tick()
        {
            if (GameStateUtil.IsQuitting)
            {
                return;
            }

            _loopback?.Pump();
            PlaySession.Pump();
            if (_hostClosed || _handedToRoom)
            {
                return;
            }

            if (_remote && !IsActiveTransportConnected())
            {
                HandleHostClosed();
            }
        }

        private void BindView()
        {
            View.DrawClicked += OnDrawClicked;
            View.AcceptClicked += OnAcceptClicked;
            View.ConfirmClicked += OnConfirmClicked;
            View.SurrenderClicked += OnSurrenderClicked;
            View.SuitClicked += OnSuitClicked;
            View.QueenModeClicked += OnQueenModeClicked;
            View.KingModeClicked += OnKingModeClicked;
            View.CardClicked += OnCardClicked;
            View.CardHovered += OnCardHovered;
            View.CardUnhovered += OnCardUnhovered;
            View.CardDragStarted += OnCardDragStarted;
            View.CardPlayDropped += OnCardPlayDropped;
            View.CancelPressed += OnCancelPressed;
            View.ChatClicked += OnChatClicked;
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

            View.DrawClicked -= OnDrawClicked;
            View.AcceptClicked -= OnAcceptClicked;
            View.ConfirmClicked -= OnConfirmClicked;
            View.SurrenderClicked -= OnSurrenderClicked;
            View.SuitClicked -= OnSuitClicked;
            View.QueenModeClicked -= OnQueenModeClicked;
            View.KingModeClicked -= OnKingModeClicked;
            View.CardClicked -= OnCardClicked;
            View.CardHovered -= OnCardHovered;
            View.CardUnhovered -= OnCardUnhovered;
            View.CardDragStarted -= OnCardDragStarted;
            View.CardPlayDropped -= OnCardPlayDropped;
            View.CancelPressed -= OnCancelPressed;
            View.ChatClicked -= OnChatClicked;
            if (View.Chat != null)
            {
                View.Chat.SendClicked -= OnChatSendClicked;
                View.Chat.QuickClicked -= OnQuickClicked;
            }
        }

        private void BindPointer()
        {
            UnbindPointer();
            _pointer.PlayCardRequested += OnPlayCardRequested;
            _pointer.DrawRequested += OnDrawRequested;
            _pointer.ChooseSuitRequested += OnChooseSuitRequested;
            _pointer.ChooseQueenModeRequested += OnChooseQueenModeRequested;
            _pointer.ChooseKingModeRequested += OnChooseKingModeRequested;
            _pointer.GiveCardsRequested += OnGiveCardsRequested;
            _pointer.HideUnderRequested += OnHideUnderRequested;
            _pointer.MirrorDiscardRequested += OnMirrorDiscardRequested;
            _pointer.SelectionChanged += OnPointerSelectionChanged;
            _pointerInput = new GamePointerInput(_pointer);
        }

        private void UnbindPointer()
        {
            _pointer.PlayCardRequested -= OnPlayCardRequested;
            _pointer.DrawRequested -= OnDrawRequested;
            _pointer.ChooseSuitRequested -= OnChooseSuitRequested;
            _pointer.ChooseQueenModeRequested -= OnChooseQueenModeRequested;
            _pointer.ChooseKingModeRequested -= OnChooseKingModeRequested;
            _pointer.GiveCardsRequested -= OnGiveCardsRequested;
            _pointer.HideUnderRequested -= OnHideUnderRequested;
            _pointer.MirrorDiscardRequested -= OnMirrorDiscardRequested;
            _pointer.SelectionChanged -= OnPointerSelectionChanged;
            if (_pointerInput != null)
            {
                _pointerInput.Dispose();
                _pointerInput = null;
            }

            _pointer.SetSheet(GamePointerSheet.None);
            _pointer.ClearAllSelections();
            _pointer.SetLocked(false);
            _pointer.SetPlayEnabled(true);
        }

        private void AttachPending()
        {
            StopHotseat();
            _remote = false;
            _loopback = _pendingLoopback;
            _clients = _pendingClients;
            _pendingLoopback = null;
            _pendingClients = null;
            _seatCount = _clients.Length;
            _localSeat = 0;
            BindClients();
            ResetTableUi("시작");
            ReplayChat();
            Refresh();
        }

        private void AttachRemote()
        {
            StopHotseat();
            _remote = true;
            _loopback = null;
            var client = _pendingRemote;
            _localSeat = _pendingLocalSeat < 0 ? 0 : _pendingLocalSeat;
            _pendingRemote = null;
            _pendingLocalSeat = 0;
            var room = client != null ? client.Room : null;
            var match = client != null ? client.PublicMatch : null;
            _seatCount = room != null && room.seatCount >= 2
                ? room.seatCount
                : (match != null && match.handCounts != null && match.handCounts.Length >= 2
                    ? match.handCounts.Length
                    : DummySeatCount);
            _clients = new NetClient[_seatCount];
            if (client != null && _localSeat >= 0 && _localSeat < _seatCount)
            {
                _clients[_localSeat] = client;
            }

            BindClients();
            ResetTableUi("시작");
            _viewSeat = _localSeat;
            ReplayChat();
            Refresh();
        }

        private void StartHotseat()
        {
            StartSession(DummySeatCount, DummySeed, "HOTSIT", new[] { "P0", "P1" }, autoStart: true);
        }

        private void RestartSession()
        {
            var nicks = BuildNicks();
            var seed = System.Environment.TickCount;
            if (seed == 0)
            {
                seed = 1;
            }

            StartSession(_seatCount < 2 ? DummySeatCount : _seatCount, seed, "HOTSIT", nicks, autoStart: true);
        }

        private void StartSession(int seatCount, int seed, string roomCode, string[] nicks, bool autoStart)
        {
            StopHotseat();
            _seatCount = seatCount < 2 ? DummySeatCount : seatCount;
            var runtime = new MatchRuntime(_seatCount, seed, roomCode: roomCode, nicks: nicks);
            _loopback = new LocalLoopback(runtime);
            _clients = new NetClient[_seatCount];
            for (var seat = 0; seat < _seatCount; seat++)
            {
                _clients[seat] = _loopback.CreateClient(seat);
            }

            BindClients();
            ReplayChat();
            ResetTableUi("준비");
            if (!autoStart)
            {
                Refresh();
                return;
            }

            for (var seat = 0; seat < _seatCount; seat++)
            {
                var client = _clients[seat];
                Send(client, OpCode.Ready, () => client.Ready());
            }

            Send(_clients[0], OpCode.StartMatch, () => _clients[0].StartMatch());
            Refresh();
        }

        private void BindClients()
        {
            if (_clients == null)
            {
                return;
            }

            for (var seat = 0; seat < _clients.Length; seat++)
            {
                var client = _clients[seat];
                if (client == null)
                {
                    continue;
                }

                client.EventReceived -= OnNetEvent;
                client.EventReceived += OnNetEvent;
            }
        }

        private void ResetTableUi(string status)
        {
            _viewSeat = 0;
            _prompt = MatchPrompt.None;
            _status = status;
            _lastPlay = null;
            _lastActSeat = -1;
            _result = null;
            _resultOpened = false;
            _resultPanel = null;
            _rematchWaiting = false;
            _handedToRoom = false;
            _hostClosed = false;
            _selectedPlayId = -1;
            _pointer.SetSheet(GamePointerSheet.None);
            _pointer.ClearAllSelections();
            _surrenderArmed = false;
            _mirrorTarget = 0;
            _lastSentOp = null;
            _lastPlayedDefId = null;
            _queenReceiveTravelArmed = false;
        }

        private void StopHotseat(bool disconnect = true)
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
                    if (disconnect && client.IsConnected)
                    {
                        client.Disconnect();
                    }
                }
            }

            _clients = null;
            _loopback = null;
            _remote = false;
        }

        private void OnNetEvent(EventMessage ev)
        {
            if (ev == null)
            {
                return;
            }

            if (ev.ev == EvCode.Reject)
            {
                ApplyReject(ev.reject);
                Refresh();
                return;
            }

            if (ev.ev == EvCode.Chat)
            {
                AppendChat(ev, true);
            }

            if (ev.ev == EvCode.PlayerDisconnected)
            {
                AppendSystemChat(ev.seq, NickOf(ev.seat) + " 연결 끊김", true);
            }

            if (ev.ev == EvCode.PlayerRejoined)
            {
                AppendSystemChat(ev.seq, NickOf(ev.seat) + " 재접속", true);
            }

            if (ev.ev == EvCode.RoomClosed)
            {
                HandleHostClosed();
                return;
            }

            if (ev.ev == EvCode.CardPlayed)
            {
                _lastPlayedDefId = ev.defId;
                RememberAct(ev.seat, NickOf(ev.seat) + "이 " + (string.IsNullOrEmpty(ev.defId) ? "?" : ev.defId) + " 냄");
            }

            if (ev.ev == EvCode.DrewCount)
            {
                RememberAct(ev.seat, NickOf(ev.seat) + "이 " + ev.count + "장 뽑음");
            }

            if (ev.ev == EvCode.KingHidden)
            {
                RememberAct(ev.seat, NickOf(ev.seat) + "이 한 장 숨김");
            }

            if (ev.ev == EvCode.MatchStarted)
            {
                CloseResultPanel();
                ResetTableUi("시작");
            }

            if (ev.ev == EvCode.RoomUpdated && ev.room != null && _resultOpened)
            {
                ResultPresenter.PushRoom(ev.room);
                _resultPanel?.ApplyRoom(ev.room);
                if (ev.room.phase == MatchPhase.Waiting)
                {
                    CloseResultPanel();
                    LeaveToRoom();
                    return;
                }
            }

            if (ev.ev == EvCode.MatchEnded)
            {
                _prompt = MatchPrompt.None;
                _result = null;
                _status = "종료";
                OpenResult(ev);
            }

            InferPrompt(ev);
            SyncViewSeat();
            HandleQueenReceiveFx(ev);
            RememberSuitChanged(ev);
            Refresh();
            HandleSuitChangedFx(ev);
        }

        private void InferPrompt(EventMessage ev)
        {
            if (ev.ev == EvCode.KingHidden || ev.ev == EvCode.QueenGiven
                || ev.ev == EvCode.SuitChanged || ev.ev == EvCode.QueenModeChosen)
            {
                _prompt = MatchPrompt.None;
                return;
            }

            if (ev.ev == EvCode.KingModeChosen)
            {
                _prompt = ev.kingMode == KingModeName.Hide ? MatchPrompt.HideUnder : MatchPrompt.None;
                return;
            }

            // 선택 직후 TurnChanged 만 오면(같은 무늬 7 등) 시트를 닫는다.
            if (ev.ev == EvCode.TurnChanged
                && (_prompt == MatchPrompt.Suit
                    || _prompt == MatchPrompt.QueenMode
                    || _prompt == MatchPrompt.KingMode)
                && (_lastSentOp == OpCode.ChooseSuit
                    || _lastSentOp == OpCode.ChooseQueenMode
                    || _lastSentOp == OpCode.ChooseKingMode))
            {
                _prompt = MatchPrompt.None;
                return;
            }

            if (ev.ev == EvCode.MirrorAdjusted)
            {
                SyncViewSeat();
                _mirrorTarget = ev.count;
                ApplyMirrorPrompt();
                return;
            }

            if (_lastSentOp == OpCode.MirrorDiscard
                && (ev.ev == EvCode.TurnChanged || ev.ev == EvCode.CardPlayed
                    || ev.ev == EvCode.HandGranted || ev.ev == EvCode.MatchEnded))
            {
                ApplyMirrorPrompt();
            }

            if (ev.ev == EvCode.CardPlayed && _lastSentOp == OpCode.PlayCard && IsLocalAct(ev.seat))
            {
                if (EndsWithRank(_lastPlayedDefId, '7'))
                {
                    _prompt = MatchPrompt.Suit;
                    _status = "무늬 고르기";
                    return;
                }

                if (EndsWithRank(_lastPlayedDefId, 'Q'))
                {
                    _prompt = MatchPrompt.QueenMode;
                    _status = "Q 고르기";
                    return;
                }

                if (EndsWithRank(_lastPlayedDefId, 'K'))
                {
                    _prompt = MatchPrompt.KingMode;
                    _status = "K 고르기";
                    return;
                }

                _prompt = MatchPrompt.None;
            }
        }

        private void RememberAct(int seat, string line)
        {
            _lastActSeat = seat;
            _lastPlay = line;
        }

        private void HandleQueenReceiveFx(EventMessage ev)
        {
            if (View == null || ev == null)
            {
                return;
            }

            var viewing = _remote ? _localSeat : _viewSeat;
            if (ev.ev == EvCode.CardsReceived)
            {
                var match = AnyPublicMatch();
                if (match != null && match.pendingGive)
                {
                    View.ArmQueenReceive(match.currentSeat);
                    _queenReceiveTravelArmed = true;
                }

                return;
            }

            if (ev.ev != EvCode.QueenGiven)
            {
                return;
            }

            if (viewing == ev.toSeat)
            {
                if (!_queenReceiveTravelArmed)
                {
                    View.ArmQueenReceive(ev.fromSeat);
                }
            }
            else
            {
                View.PlayQueenGiveFlight(ev.fromSeat, ev.toSeat, ev.count, ev.seq);
            }

            _queenReceiveTravelArmed = false;
        }

        private void RememberSuitChanged(EventMessage ev)
        {
            if (ev == null || ev.ev != EvCode.SuitChanged)
            {
                return;
            }

            var suit = ResolveChangedSuit(ev);
            if (string.IsNullOrEmpty(suit))
            {
                return;
            }

            RememberAct(ev.seat, NickOf(ev.seat) + "이 " + ChoiceSheet.SuitGlyph(suit) + " 지정");
        }

        private void HandleSuitChangedFx(EventMessage ev)
        {
            if (View == null || ev == null || ev.ev != EvCode.SuitChanged)
            {
                return;
            }

            var suit = ResolveChangedSuit(ev);
            if (string.IsNullOrEmpty(suit))
            {
                return;
            }

            View.PlaySuitChanged(suit, ev.seq);
        }

        private static string ResolveChangedSuit(EventMessage ev)
        {
            if (!string.IsNullOrEmpty(ev.suit))
            {
                return ev.suit;
            }

            return ev.match != null ? ev.match.requiredSuit : null;
        }

        private void ApplyReject(string reject)
        {
            _status = reject ?? "거절";
            _surrenderArmed = false;
            _selectedPlayId = -1;
            _pointer.ClearSelection();
            switch (reject)
            {
                case RejectCode.NeedSuitPick:
                    _prompt = MatchPrompt.Suit;
                    break;
                case RejectCode.NeedQueenMode:
                    _prompt = MatchPrompt.QueenMode;
                    break;
                case RejectCode.NeedKingMode:
                    _prompt = MatchPrompt.KingMode;
                    break;
                case RejectCode.NeedHideUnder:
                case RejectCode.NoCardToHide:
                    _prompt = MatchPrompt.HideUnder;
                    break;
                case RejectCode.NeedGiveCards:
                case RejectCode.GiveCountMismatch:
                    if (IsLocalGiveTurn())
                    {
                        _prompt = MatchPrompt.GiveCards;
                    }
                    break;
                case RejectCode.NeedMirrorDiscard:
                    _prompt = MatchPrompt.MirrorDiscard;
                    break;
                default:
                    _pointer.ClearAllSelections();
                    break;
            }
        }

        private void OnCardClicked(int instanceId)
        {
            if (IsPlayLocked() || instanceId < 0)
            {
                return;
            }

            _surrenderArmed = false;
            if (_prompt == MatchPrompt.None
                || _prompt == MatchPrompt.GiveCards
                || _prompt == MatchPrompt.HideUnder
                || _prompt == MatchPrompt.MirrorDiscard)
            {
                return;
            }

            _pointer.TapCard(instanceId);
        }

        private void OnCardHovered(int instanceId)
        {
            if (IsPlayLocked() || instanceId < 0 || !AllowsHandPreview(_prompt))
            {
                return;
            }

            if (_hoverPreviewId == instanceId)
            {
                return;
            }

            _hoverPreviewId = instanceId;
            RefreshHoverPreview();
        }

        private void OnCardUnhovered(int instanceId)
        {
            if (_hoverPreviewId != instanceId)
            {
                return;
            }

            _hoverPreviewId = -1;
            RefreshHoverPreview();
        }

        private void RefreshHoverPreview()
        {
            if (View == null || GameStateUtil.IsQuitting)
            {
                return;
            }

            var client = ActiveClient();
            View.BindHoverPreview(
                client != null ? client.HandInstanceIds : null,
                client != null ? client.HandDefIds : null,
                DragSelection(),
                _hoverPreviewId);
        }

        private void OnCardDragStarted(int instanceId)
        {
            if (IsPlayLocked() || instanceId < 0 || !AllowsHandPreview(_prompt))
            {
                return;
            }

            _surrenderArmed = false;
            _pointer.SelectCard(instanceId);
        }

        private void OnCardPlayDropped(int instanceId)
        {
            if (IsPlayLocked() || instanceId < 0)
            {
                View.CancelHandDrag();
                return;
            }

            if (_prompt == MatchPrompt.GiveCards)
            {
                _surrenderArmed = false;
                _pointer.RequestGive(instanceId);
                if (_pointer.MultiSelectedIds.Count > 0)
                {
                    View.CancelHandDrag();
                }

                return;
            }

            if (_prompt == MatchPrompt.HideUnder)
            {
                _surrenderArmed = false;
                _pointer.RequestHide(instanceId);
                return;
            }

            if (_prompt == MatchPrompt.MirrorDiscard)
            {
                _surrenderArmed = false;
                _pointer.RequestMirror(instanceId);
                if (_pointer.MultiSelectedIds.Count > 0)
                {
                    View.CancelHandDrag();
                }

                return;
            }

            if (_prompt != MatchPrompt.None)
            {
                View.CancelHandDrag();
                return;
            }

            _surrenderArmed = false;
            _pointer.RequestPlay(instanceId);
        }

        private void OnPlayCardRequested(int instanceId)
        {
            Send(ActiveClient(), OpCode.PlayCard, () => ActiveClient().PlayCard(instanceId));
        }

        private void OnDrawRequested()
        {
            if (IsPlayLocked() || _prompt != MatchPrompt.None)
            {
                return;
            }

            _surrenderArmed = false;
            Send(ActiveClient(), OpCode.Draw, () => ActiveClient().Draw());
        }

        private void OnPointerSelectionChanged()
        {
            Refresh();
        }

        private void OnCancelPressed()
        {
            View.CancelHandDrag();
            _pointer.Cancel();
        }

        private void OnDrawClicked()
        {
            _pointer.Draw();
        }

        private void OnAcceptClicked()
        {
            if (IsPlayLocked() || _prompt != MatchPrompt.None)
            {
                return;
            }

            _surrenderArmed = false;
            var match = ActiveClient()?.PublicMatch;
            if (match == null || !IsActingSeat(match))
            {
                return;
            }

            if (match.queenStack > 0 && !match.pendingGive)
            {
                Send(ActiveClient(), OpCode.AcceptQueen, () => ActiveClient().AcceptQueen());
                return;
            }

            if (match.attackStack <= 0)
            {
                return;
            }

            Send(ActiveClient(), OpCode.Draw, () => ActiveClient().Draw());
        }

        private void OnConfirmClicked()
        {
            if (IsPlayLocked())
            {
                return;
            }

            _surrenderArmed = false;
            _pointer.Confirm();
        }

        private void OnSurrenderClicked()
        {
            var client = ActiveClient();
            if (client == null || GameStateUtil.IsQuitting)
            {
                return;
            }

            if (!_surrenderArmed)
            {
                _surrenderArmed = true;
                _status = "기권하려면 다시";
                Refresh();
                return;
            }

            _surrenderArmed = false;
            Send(client, OpCode.Surrender, () => client.Surrender());
        }

        private void OnChatClicked()
        {
            _chatVisible = !_chatVisible;
            View.SetChatVisible(_chatVisible);
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

            if (body.Length > MatchRuntime.ChatMaxChars)
            {
                body = body.Substring(0, MatchRuntime.ChatMaxChars);
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
            var client = ActiveClient();
            if (client == null)
            {
                return;
            }

            client.Chat(text, ChatChannel.Match, quickId);
            if (View.Chat != null)
            {
                View.Chat.ClearInput();
            }
        }

        private void ReplayChat()
        {
            _lastChatSeq = int.MinValue;
            if (View == null || View.Chat == null)
            {
                return;
            }

            View.Chat.ClearLog();
            var client = ActiveClient();
            if (client == null && _clients != null)
            {
                for (var i = 0; i < _clients.Length; i++)
                {
                    if (_clients[i] != null)
                    {
                        client = _clients[i];
                        break;
                    }
                }
            }

            if (client == null)
            {
                return;
            }

            var history = client.RecentChat;
            for (var i = 0; i < history.Count; i++)
            {
                AppendChat(history[i]);
            }
        }

        private void AppendChat(EventMessage ev, bool notifyUnread = false)
        {
            if (ev == null || View == null || View.Chat == null)
            {
                return;
            }

            if (!TakeChatSeq(ev.seq))
            {
                return;
            }

            View.Chat.Append(ev.chatType, NickOf(ev.seat), ev.text, ev.quickId);
            if (notifyUnread && ShouldNotifyChat(ev.seat, ev.chatType))
            {
                View.NotifyChatArrived();
            }
        }

        private void AppendSystemChat(int seq, string text, bool notifyUnread = false)
        {
            if (View == null || View.Chat == null)
            {
                return;
            }

            if (!TakeChatSeq(seq))
            {
                return;
            }

            View.Chat.Append(ChatType.System, null, text, null);
            if (notifyUnread)
            {
                View.NotifyChatArrived();
            }
        }

        private bool ShouldNotifyChat(int seat, string chatType)
        {
            if (chatType == ChatType.System)
            {
                return true;
            }

            var local = _remote ? _localSeat : _viewSeat;
            return local < 0 || seat != local;
        }

        private bool TakeChatSeq(int seq)
        {
            if (seq == _lastChatSeq)
            {
                return false;
            }

            _lastChatSeq = seq;
            return true;
        }

        private void OnSuitClicked(string suit)
        {
            _surrenderArmed = false;
            _pointer.TapSuit(suit);
        }

        private void OnQueenModeClicked(string queenMode)
        {
            _surrenderArmed = false;
            _pointer.TapQueenMode(queenMode);
        }

        private void OnKingModeClicked(string kingMode)
        {
            _surrenderArmed = false;
            _pointer.TapKingMode(kingMode);
        }

        private void OnChooseSuitRequested(string suit)
        {
            Send(ActiveClient(), OpCode.ChooseSuit, () => ActiveClient().ChooseSuit(suit));
        }

        private void OnChooseQueenModeRequested(string queenMode)
        {
            Send(ActiveClient(), OpCode.ChooseQueenMode, () => ActiveClient().ChooseQueenMode(queenMode));
        }

        private void OnChooseKingModeRequested(string kingMode)
        {
            Send(ActiveClient(), OpCode.ChooseKingMode, () => ActiveClient().ChooseKingMode(kingMode));
        }

        private void OnGiveCardsRequested(int[] ids)
        {
            Send(ActiveClient(), OpCode.GiveCards, () => ActiveClient().GiveCards(ids));
        }

        private void OnHideUnderRequested(int instanceId)
        {
            Send(ActiveClient(), OpCode.HideUnder, () => ActiveClient().HideUnder(instanceId));
        }

        private void OnMirrorDiscardRequested(int[] ids)
        {
            Send(ActiveClient(), OpCode.MirrorDiscard, () => ActiveClient().MirrorDiscard(ids));
        }

        private void Send(NetClient client, string op, System.Action send)
        {
            if (client == null)
            {
                return;
            }

            _lastSentOp = op;
            send();
            Refresh();
        }

        private void SyncViewSeat()
        {
            if (_remote)
            {
                _viewSeat = _localSeat;
                return;
            }

            var match = AnyPublicMatch();
            if (match == null)
            {
                return;
            }

            _viewSeat = match.currentSeat;
            if (_viewSeat < 0 || _viewSeat >= _seatCount)
            {
                _viewSeat = 0;
            }
        }

        private void Refresh()
        {
            if (View == null || GameStateUtil.IsQuitting)
            {
                return;
            }

            if (!_remote)
            {
                SyncViewSeat();
            }

            var client = ActiveClient();
            var match = client != null ? client.PublicMatch : AnyPublicMatch();
            if (_hoverPreviewId >= 0 && !ContainsHandId(client != null ? client.HandInstanceIds : null, _hoverPreviewId))
            {
                _hoverPreviewId = -1;
            }
            ApplyGivePromptFromMatch(match);
            if (_prompt == MatchPrompt.MirrorDiscard)
            {
                ApplyMirrorPrompt();
            }

            var selected = DragSelection();
            var locked = IsPlayLocked();
            _pointer.SetSheet(ToSheet(_prompt));
            _pointer.SetLocked(locked);
            _pointer.SetMultiLimit(MultiLimit(match, client));
            if (_prompt == MatchPrompt.None)
            {
                _pointer.SetPlayEnabled(!locked);
            }

            View.Render(
                match,
                _remote ? _localSeat : _viewSeat,
                client != null ? client.HandInstanceIds : null,
                client != null ? client.HandDefIds : null,
                selected,
                BuildLegalFlags(match, client != null ? client.HandDefIds : null),
                _prompt,
                _status,
                _lastPlay,
                _lastActSeat,
                _result,
                locked,
                BuildNicks(),
                _hoverPreviewId);
            View.SetSurrenderArmed(_surrenderArmed);
        }

        private HashSet<int> PlaySelection()
        {
            var set = new HashSet<int>();
            if (_pointer.HasSelection)
            {
                set.Add(_pointer.SelectedInstanceId);
            }
            else if (_selectedPlayId >= 0)
            {
                set.Add(_selectedPlayId);
            }

            return set;
        }

        private HashSet<int> DragSelection()
        {
            if (_prompt != MatchPrompt.GiveCards && _prompt != MatchPrompt.MirrorDiscard)
            {
                return PlaySelection();
            }

            var set = new HashSet<int>(_pointer.MultiSelectedIds);
            if (_pointer.HasSelection)
            {
                set.Add(_pointer.SelectedInstanceId);
            }

            return set;
        }

        private static bool AllowsHandPreview(MatchPrompt prompt)
        {
            return prompt == MatchPrompt.None
                || prompt == MatchPrompt.GiveCards
                || prompt == MatchPrompt.HideUnder
                || prompt == MatchPrompt.MirrorDiscard;
        }

        private static bool ContainsHandId(IReadOnlyList<int> ids, int id)
        {
            if (ids == null || id < 0)
            {
                return false;
            }

            for (var i = 0; i < ids.Count; i++)
            {
                if (ids[i] == id)
                {
                    return true;
                }
            }

            return false;
        }

        private int MultiLimit(PublicMatchView match, NetClient client)
        {
            var hand = client != null && client.HandInstanceIds != null ? client.HandInstanceIds.Count : 0;
            if (_prompt == MatchPrompt.MirrorDiscard)
            {
                var need = hand - _mirrorTarget;
                return need > 0 ? need : 0;
            }

            if (_prompt != MatchPrompt.GiveCards)
            {
                return 0;
            }

            var give = match != null ? match.queenStack : 0;
            if (give <= 0)
            {
                return 0;
            }

            return hand > 0 && hand < give ? hand : give;
        }

        private bool[] BuildLegalFlags(PublicMatchView match, IReadOnlyList<string> handDefs)
        {
            var count = handDefs != null ? handDefs.Count : 0;
            var flags = new bool[count];
            var anyPick = _prompt == MatchPrompt.GiveCards
                || _prompt == MatchPrompt.HideUnder
                || _prompt == MatchPrompt.MirrorDiscard;
            for (var i = 0; i < count; i++)
            {
                flags[i] = anyPick || LegalHint.CanPlay(match, handDefs[i]);
            }

            return flags;
        }

        private bool IsLocked()
        {
            var client = ActiveClient();
            return client != null && client.HasPendingAck;
        }

        private bool IsPlayLocked()
        {
            return IsLocked() || !IsMyTurn();
        }

        private bool IsMyTurn()
        {
            if (!_remote)
            {
                return true;
            }

            var match = ActiveClient() != null ? ActiveClient().PublicMatch : AnyPublicMatch();
            return match != null && match.currentSeat == _localSeat;
        }

        private bool IsLocalAct(int seat)
        {
            return !_remote || seat == _localSeat;
        }

        private bool IsLocalGiveTurn()
        {
            if (!_remote)
            {
                return true;
            }

            var match = ActiveClient() != null ? ActiveClient().PublicMatch : AnyPublicMatch();
            return match != null && match.currentSeat == _localSeat;
        }

        private bool IsActingSeat(PublicMatchView match)
        {
            if (match == null)
            {
                return false;
            }

            return match.currentSeat == (_remote ? _localSeat : _viewSeat);
        }

        /// <summary>
        /// 대상이 감수해 pendingGive 가 켜지고 지금 낼 좌석이면 지급 시트를 연다.
        /// QueenModeChosen 이벤트만 믿으면 TurnChanged 뒤에 시트가 풀린다.
        /// </summary>
        private void ApplyGivePromptFromMatch(PublicMatchView match)
        {
            if (match == null)
            {
                return;
            }

            if (match.queenStack > 0 && match.pendingGive && IsActingSeat(match))
            {
                _prompt = MatchPrompt.GiveCards;
                _status = "지급할 장 1장";
                return;
            }

            if (_prompt == MatchPrompt.GiveCards && (match.queenStack <= 0 || !match.pendingGive))
            {
                _prompt = MatchPrompt.None;
            }
        }

        /// <summary>
        /// 내 손패가 목표보다 많고, 지금 버릴 좌석일 때만 미러 시트를 연다.
        /// 목표 0(처리 끝)이면 닫는다.
        /// </summary>
        private void ApplyMirrorPrompt()
        {
            if (ShouldShowMirrorDiscard())
            {
                _prompt = MatchPrompt.MirrorDiscard;
                _status = "버릴 장";
                return;
            }

            if (_prompt == MatchPrompt.MirrorDiscard)
            {
                _prompt = MatchPrompt.None;
            }
        }

        private bool ShouldShowMirrorDiscard()
        {
            if (_mirrorTarget <= 0)
            {
                return false;
            }

            var client = ActiveClient();
            var hand = client != null ? client.HandInstanceIds : null;
            if (hand == null || hand.Count <= _mirrorTarget)
            {
                return false;
            }

            var match = client != null && client.PublicMatch != null ? client.PublicMatch : AnyPublicMatch();
            if (match == null)
            {
                return true;
            }

            var seat = _remote ? _localSeat : _viewSeat;
            return match.currentSeat == seat;
        }

        private NetClient ActiveClient()
        {
            if (_clients == null)
            {
                return null;
            }

            var seat = _remote ? _localSeat : _viewSeat;
            if (seat < 0 || seat >= _clients.Length)
            {
                return null;
            }

            return _clients[seat];
        }

        private PublicMatchView AnyPublicMatch()
        {
            if (_clients == null)
            {
                return null;
            }

            for (var i = 0; i < _clients.Length; i++)
            {
                if (_clients[i] != null && _clients[i].PublicMatch != null)
                {
                    return _clients[i].PublicMatch;
                }
            }

            return null;
        }

        private void OpenResult(EventMessage ev)
        {
            if (_resultOpened || ev == null || ev.result == null)
            {
                return;
            }

            _resultOpened = true;
            _rematchWaiting = false;
            ResultPresenter.PushRoom(ActiveClient() != null ? ActiveClient().Room : AnyRoom());
            ResultPresenter.Prepare(
                ev.result,
                BuildNicks(),
                ev.deadlineMs,
                OnRematchVote);
            OpenResultAsync().Forget();
        }

        private async UniTaskVoid OpenResultAsync()
        {
            var panel = await UIManager.OpenAsync<ResultPanel>();
            if (!_resultOpened || GameStateUtil.IsQuitting)
            {
                if (panel != null)
                {
                    UIManager.Close(panel);
                }

                return;
            }

            _resultPanel = panel;
            var room = ActiveClient() != null ? ActiveClient().Room : AnyRoom();
            if (room != null)
            {
                panel.ApplyRoom(room);
            }
        }

        private void OnRematchVote(bool rematchYes)
        {
            _rematchWaiting = rematchYes;
            ActiveClient()?.RematchVote(rematchYes);
            if (!rematchYes || _remote || _loopback == null || _clients == null)
            {
                return;
            }

            for (var seat = 0; seat < _clients.Length; seat++)
            {
                var client = _clients[seat];
                if (client == null || client == ActiveClient())
                {
                    continue;
                }

                client.RematchVote(true);
            }
        }

        private void CloseResultPanel()
        {
            if (_resultPanel != null)
            {
                UIManager.Close(_resultPanel);
                _resultPanel = null;
            }

            _resultOpened = false;
            _rematchWaiting = false;
        }

        private void LeaveToRoom()
        {
            if (_handedToRoom || _hostClosed || GameStateUtil.IsQuitting)
            {
                return;
            }

            if (!_remote)
            {
                LeaveToLobby();
                return;
            }

            var client = ActiveClient();
            if (client == null)
            {
                LeaveToLobby();
                return;
            }

            _handedToRoom = true;
            CloseResultPanel();
            RoomPresenter.PrepareResume(client, _localSeat);
            if (View != null)
            {
                UIManager.Close(View);
            }

            UIManager.OpenAsync<RoomPanel>().Forget();
        }

        private void HandleHostClosed()
        {
            if (_handedToRoom || _hostClosed || GameStateUtil.IsQuitting)
            {
                return;
            }

            _hostClosed = true;
            CloseResultPanel();
            if (View != null)
            {
                UIManager.Close(View);
            }

            LobbyPresenter.OpenAfterHostClosed();
        }

        private bool IsActiveTransportConnected()
        {
            var client = ActiveClient();
            return client == null || client.IsConnected;
        }

        private void LeaveToLobby()
        {
            if (View != null)
            {
                UIManager.Close(View);
            }

            UIManager.OpenAsync<LobbyPanel>().Forget();
        }

        private string NickOf(int seat)
        {
            var nicks = BuildNicks();
            if (nicks != null && seat >= 0 && seat < nicks.Length && !string.IsNullOrEmpty(nicks[seat]))
            {
                return nicks[seat];
            }

            return "P" + seat;
        }

        private string[] BuildNicks()
        {
            var room = AnyRoom();
            var nicks = new string[_seatCount];
            for (var i = 0; i < _seatCount; i++)
            {
                if (room != null && room.nicks != null && i < room.nicks.Length && !string.IsNullOrEmpty(room.nicks[i]))
                {
                    nicks[i] = room.nicks[i];
                    continue;
                }

                nicks[i] = "P" + i;
            }

            return nicks;
        }

        private RoomView AnyRoom()
        {
            if (_clients == null)
            {
                return null;
            }

            for (var i = 0; i < _clients.Length; i++)
            {
                if (_clients[i] != null && _clients[i].Room != null)
                {
                    return _clients[i].Room;
                }
            }

            return null;
        }

        private void ReleaseHand()
        {
            if (View == null)
            {
                return;
            }

            View.ReleaseHand();
        }

        private static GamePointerSheet ToSheet(MatchPrompt prompt)
        {
            switch (prompt)
            {
                case MatchPrompt.Suit:
                    return GamePointerSheet.Suit;
                case MatchPrompt.QueenMode:
                    return GamePointerSheet.QueenMode;
                case MatchPrompt.KingMode:
                    return GamePointerSheet.KingMode;
                case MatchPrompt.GiveCards:
                    return GamePointerSheet.GiveCards;
                case MatchPrompt.HideUnder:
                    return GamePointerSheet.HideUnder;
                case MatchPrompt.MirrorDiscard:
                    return GamePointerSheet.MirrorDiscard;
                default:
                    return GamePointerSheet.None;
            }
        }

        private static bool EndsWithRank(string defId, char rank)
        {
            if (string.IsNullOrEmpty(defId) || defId[defId.Length - 1] != rank)
            {
                return false;
            }

            if (defId.IndexOf(':') >= 0)
            {
                return false;
            }

            return defId.Length >= 2 && defId.Length <= 3;
        }

    }
}
