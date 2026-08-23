using System.Collections.Generic;
using Backend.App;
using Backend.Net;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;

namespace Backend.Object.UI
{
    /// <summary>
    /// 2인 핫시트 더미 매치. <see cref="NetClient"/> 로 의도만 보내고 규칙은 판결하지 않는다.
    /// 현재 턴 좌석 손패만 표시한다.
    /// </summary>
    public sealed class MatchPresenter : UIPresenter<MatchPanel>
    {
        private const int SeatCount = 2;
        private const int DummySeed = 1;

        private readonly GamePointer _pointer = new GamePointer();

        private LocalLoopback _loopback;
        private NetClient[] _clients;
        private GamePointerInput _pointerInput;
        private int _viewSeat;
        private int _selectedPlayId = -1;
        private string _lastSentOp;
        private string _lastPlayedDefId;
        private MatchPrompt _prompt;
        private string _status;
        private string _result;
        private bool _surrenderArmed;
        private bool _resultOpened;

        /// <summary>
        /// 루프백 호스트를 열고 입력을 NetClient 커맨드로만 보낸다.
        /// </summary>
        public override void OnOpen()
        {
            View.EnsureLayout();
            BindView();
            BindPointer();
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
            StopHotseat();
        }

        /// <summary>
        /// 턴 초과·유예를 호스트에 넘긴다. 화면은 판결하지 않는다.
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
            View.DrawClicked += OnDrawClicked;
            View.AcceptClicked += OnAcceptClicked;
            View.ConfirmClicked += OnConfirmClicked;
            View.SurrenderClicked += OnSurrenderClicked;
            View.SuitClicked += OnSuitClicked;
            View.QueenModeClicked += OnQueenModeClicked;
            View.KingModeClicked += OnKingModeClicked;
            View.CardClicked += OnCardClicked;
            View.CancelPressed += OnCancelPressed;
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
            View.CancelPressed -= OnCancelPressed;
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

        private void StartHotseat()
        {
            StopHotseat();
            var runtime = new MatchRuntime(SeatCount, DummySeed, roomCode: "HOTSIT", nicks: new[] { "P0", "P1" });
            _loopback = new LocalLoopback(runtime);
            _clients = new NetClient[SeatCount];
            for (var seat = 0; seat < SeatCount; seat++)
            {
                var client = _loopback.CreateClient(seat);
                client.EventReceived += OnNetEvent;
                _clients[seat] = client;
            }

            _viewSeat = 0;
            _prompt = MatchPrompt.None;
            _status = "준비";
            _result = null;
            _resultOpened = false;
            _selectedPlayId = -1;
            _pointer.SetSheet(GamePointerSheet.None);
            _pointer.ClearAllSelections();
            _surrenderArmed = false;
            _lastSentOp = null;
            _lastPlayedDefId = null;

            Send(_clients[0], OpCode.Ready, () => _clients[0].Ready());
            Send(_clients[1], OpCode.Ready, () => _clients[1].Ready());
            Send(_clients[0], OpCode.StartMatch, () => _clients[0].StartMatch());
            Refresh();
        }

        private void StopHotseat()
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
                ApplyReject(ev.reject);
                Refresh();
                return;
            }

            if (ev.ev == EvCode.CardPlayed)
            {
                _lastPlayedDefId = ev.defId;
            }

            if (ev.ev == EvCode.KingHidden)
            {
                ActiveClient()?.RequestSnapshot();
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
            Refresh();
        }

        private void InferPrompt(EventMessage ev)
        {
            if (ev.ev == EvCode.KingHidden || ev.ev == EvCode.QueenGiven
                || ev.ev == EvCode.SuitChanged || ev.ev == EvCode.QueenModeChosen)
            {
                if (ev.ev == EvCode.QueenModeChosen)
                {
                    _prompt = MatchPrompt.None;
                    return;
                }

                _prompt = MatchPrompt.None;
                return;
            }

            if (ev.ev == EvCode.KingModeChosen)
            {
                _prompt = ev.kingMode == KingModeName.Hide ? MatchPrompt.HideUnder : MatchPrompt.None;
                return;
            }

            if (ev.ev == EvCode.MirrorAdjusted)
            {
                SyncViewSeat();
                var hand = ActiveClient()?.HandInstanceIds;
                if (hand != null && hand.Count > ev.count)
                {
                    _prompt = MatchPrompt.MirrorDiscard;
                    _status = "버릴 장";
                }
                else
                {
                    _prompt = MatchPrompt.None;
                }

                return;
            }

            if (_lastSentOp == OpCode.AcceptQueen && ev.ev == EvCode.TurnChanged)
            {
                _prompt = MatchPrompt.GiveCards;
                _status = "지급할 장";
                return;
            }

            if (ev.ev == EvCode.CardPlayed && _lastSentOp == OpCode.PlayCard)
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
                    _prompt = MatchPrompt.GiveCards;
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
            if (IsLocked() || instanceId < 0)
            {
                return;
            }

            _surrenderArmed = false;
            if (_prompt == MatchPrompt.HideUnder)
            {
                if (_selectedPlayId == instanceId)
                {
                    Send(ActiveClient(), OpCode.HideUnder, () => ActiveClient().HideUnder(instanceId));
                    _selectedPlayId = -1;
                    return;
                }

                _selectedPlayId = instanceId;
                Refresh();
                return;
            }

            if (_prompt == MatchPrompt.None && _pointer.SelectedInstanceId != instanceId)
            {
                _status = "다시 눌러 내기";
            }

            _pointer.TapCard(instanceId);
        }

        private void OnPlayCardRequested(int instanceId)
        {
            Send(ActiveClient(), OpCode.PlayCard, () => ActiveClient().PlayCard(instanceId));
        }

        private void OnDrawRequested()
        {
            if (IsLocked() || _prompt != MatchPrompt.None)
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
            _pointer.Cancel();
        }

        private void OnDrawClicked()
        {
            _pointer.Draw();
        }

        private void OnAcceptClicked()
        {
            if (IsLocked() || _prompt != MatchPrompt.None)
            {
                return;
            }

            _surrenderArmed = false;
            var match = ActiveClient()?.PublicMatch;
            if (match != null && match.attackStack > 0)
            {
                Send(ActiveClient(), OpCode.Draw, () => ActiveClient().Draw());
                return;
            }

            if (match != null && match.queenStack > 0)
            {
                Send(ActiveClient(), OpCode.AcceptQueen, () => ActiveClient().AcceptQueen());
            }
        }

        private void OnConfirmClicked()
        {
            if (IsLocked())
            {
                return;
            }

            _surrenderArmed = false;
            _pointer.Confirm();
        }

        private void OnSurrenderClicked()
        {
            if (IsLocked())
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
            Send(ActiveClient(), OpCode.Surrender, () => ActiveClient().Surrender());
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
            var match = AnyPublicMatch();
            if (match == null)
            {
                return;
            }

            _viewSeat = match.currentSeat;
            if (_viewSeat < 0 || _viewSeat >= SeatCount)
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

            SyncViewSeat();
            var client = ActiveClient();
            var match = client != null ? client.PublicMatch : AnyPublicMatch();
            var selected = _prompt == MatchPrompt.GiveCards || _prompt == MatchPrompt.MirrorDiscard
                ? _pointer.MultiSelectedIds
                : PlaySelection();
            var locked = IsLocked();
            _pointer.SetSheet(ToSheet(_prompt));
            _pointer.SetLocked(locked);
            if (_prompt == MatchPrompt.None || _prompt == MatchPrompt.HideUnder)
            {
                _pointer.SetPlayEnabled(_prompt == MatchPrompt.None && !locked);
            }

            View.Render(
                match,
                _viewSeat,
                client != null ? client.HandInstanceIds : null,
                client != null ? client.HandDefIds : null,
                selected,
                BuildLegalFlags(match, client != null ? client.HandDefIds : null),
                _prompt,
                _status,
                _result,
                locked);
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

        private static bool[] BuildLegalFlags(PublicMatchView match, IReadOnlyList<string> handDefs)
        {
            var count = handDefs != null ? handDefs.Count : 0;
            var flags = new bool[count];
            for (var i = 0; i < count; i++)
            {
                flags[i] = LegalHint.CanPlay(match, handDefs[i]);
            }

            return flags;
        }

        private bool IsLocked()
        {
            var client = ActiveClient();
            return client != null && client.HasPendingAck;
        }

        private NetClient ActiveClient()
        {
            if (_clients == null || _viewSeat < 0 || _viewSeat >= _clients.Length)
            {
                return null;
            }

            return _clients[_viewSeat];
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
            ResultPresenter.Prepare(
                ev.result,
                BuildNicks(),
                ev.deadlineMs,
                vote => ActiveClient()?.RematchVote(vote),
                OnResultClosed);
            UIManager.OpenAsync<ResultPanel>().Forget();
        }

        private void OnResultClosed(bool rematchYes)
        {
            _resultOpened = false;
            if (!rematchYes || GameStateUtil.IsQuitting)
            {
                return;
            }

            StartHotseat();
        }

        private static string[] BuildNicks()
        {
            var nicks = new string[SeatCount];
            for (var i = 0; i < SeatCount; i++)
            {
                nicks[i] = "P" + i;
            }

            return nicks;
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
                case MatchPrompt.MirrorDiscard:
                    return GamePointerSheet.MirrorDiscard;
                default:
                    return GamePointerSheet.None;
            }
        }

        private static bool EndsWithRank(string defId, char rank)
        {
            return !string.IsNullOrEmpty(defId) && defId[defId.Length - 1] == rank;
        }

    }
}
