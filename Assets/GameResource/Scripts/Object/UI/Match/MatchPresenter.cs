using System.Collections.Generic;
using Backend.App;
using Backend.Net;
using Backend.Object.Management;

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

        private readonly HashSet<int> _selectedIds = new HashSet<int>();

        private LocalLoopback _loopback;
        private NetClient[] _clients;
        private int _viewSeat;
        private int _selectedPlayId = -1;
        private string _lastSentOp;
        private string _lastPlayedDefId;
        private MatchPrompt _prompt;
        private string _status;
        private string _result;
        private bool _surrenderArmed;

        /// <summary>
        /// 루프백 호스트를 열고 입력을 NetClient 커맨드로만 보낸다.
        /// </summary>
        public override void OnOpen()
        {
            View.EnsureLayout();
            BindView();
            StartHotseat();
        }

        /// <summary>
        /// 구독과 루프백을 해제한다.
        /// </summary>
        public override void OnClose()
        {
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
            _selectedPlayId = -1;
            _selectedIds.Clear();
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
                _result = FormatResult(ev.result);
                _status = "종료";
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
                _prompt = hand != null && hand.Count > ev.count
                    ? MatchPrompt.MirrorDiscard
                    : MatchPrompt.None;
                return;
            }

            if (_lastSentOp == OpCode.AcceptQueen && ev.ev == EvCode.TurnChanged)
            {
                _prompt = MatchPrompt.GiveCards;
                return;
            }

            if (ev.ev == EvCode.CardPlayed && _lastSentOp == OpCode.PlayCard)
            {
                if (EndsWithRank(_lastPlayedDefId, '7'))
                {
                    _prompt = MatchPrompt.Suit;
                    return;
                }

                if (EndsWithRank(_lastPlayedDefId, 'Q'))
                {
                    _prompt = MatchPrompt.QueenMode;
                    return;
                }

                if (EndsWithRank(_lastPlayedDefId, 'K'))
                {
                    _prompt = MatchPrompt.KingMode;
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
                    _selectedIds.Clear();
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
            if (_prompt == MatchPrompt.GiveCards || _prompt == MatchPrompt.MirrorDiscard)
            {
                if (!_selectedIds.Add(instanceId))
                {
                    _selectedIds.Remove(instanceId);
                }

                Refresh();
                return;
            }

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

            if (_prompt != MatchPrompt.None)
            {
                return;
            }

            if (_selectedPlayId == instanceId)
            {
                Send(ActiveClient(), OpCode.PlayCard, () => ActiveClient().PlayCard(instanceId));
                _selectedPlayId = -1;
                return;
            }

            _selectedPlayId = instanceId;
            _status = "다시 눌러 내기";
            Refresh();
        }

        private void OnDrawClicked()
        {
            if (IsLocked() || _prompt != MatchPrompt.None)
            {
                return;
            }

            _surrenderArmed = false;
            Send(ActiveClient(), OpCode.Draw, () => ActiveClient().Draw());
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
            var ids = ToArray(_selectedIds);
            if (_prompt == MatchPrompt.GiveCards)
            {
                Send(ActiveClient(), OpCode.GiveCards, () => ActiveClient().GiveCards(ids));
                _selectedIds.Clear();
                return;
            }

            if (_prompt == MatchPrompt.MirrorDiscard)
            {
                Send(ActiveClient(), OpCode.MirrorDiscard, () => ActiveClient().MirrorDiscard(ids));
                _selectedIds.Clear();
            }
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
            if (IsLocked())
            {
                return;
            }

            Send(ActiveClient(), OpCode.ChooseSuit, () => ActiveClient().ChooseSuit(suit));
        }

        private void OnQueenModeClicked(string queenMode)
        {
            if (IsLocked())
            {
                return;
            }

            Send(ActiveClient(), OpCode.ChooseQueenMode, () => ActiveClient().ChooseQueenMode(queenMode));
        }

        private void OnKingModeClicked(string kingMode)
        {
            if (IsLocked())
            {
                return;
            }

            Send(ActiveClient(), OpCode.ChooseKingMode, () => ActiveClient().ChooseKingMode(kingMode));
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
                ? _selectedIds
                : PlaySelection();

            View.Render(
                match,
                _viewSeat,
                client != null ? client.HandInstanceIds : null,
                client != null ? client.HandDefIds : null,
                selected,
                _prompt,
                _status,
                _result,
                IsLocked());
        }

        private HashSet<int> PlaySelection()
        {
            var set = new HashSet<int>();
            if (_selectedPlayId >= 0)
            {
                set.Add(_selectedPlayId);
            }

            return set;
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

        private void ReleaseHand()
        {
            if (View == null)
            {
                return;
            }

            var cards = View.HandCards;
            for (var i = 0; i < cards.Count; i++)
            {
                ObjectPoolManager.Release(cards[i]);
            }

            View.ClearHandTracking();
        }

        private static int[] ToArray(HashSet<int> ids)
        {
            var arr = new int[ids.Count];
            ids.CopyTo(arr);
            return arr;
        }

        private static bool EndsWithRank(string defId, char rank)
        {
            return !string.IsNullOrEmpty(defId) && defId[defId.Length - 1] == rank;
        }

        private static string FormatResult(MatchEndView result)
        {
            if (result == null || result.ranks == null)
            {
                return "종료";
            }

            var lines = new string[result.ranks.Length];
            for (var seat = 0; seat < result.ranks.Length; seat++)
            {
                var rank = result.ranks[seat];
                var count = result.handCounts != null && seat < result.handCounts.Length ? result.handCounts[seat] : 0;
                var score = result.scores != null && seat < result.scores.Length ? result.scores[seat] : 0;
                lines[seat] = $"P{seat} {rank}위 장수{count} 점수{score}";
            }

            return string.Join("\n", lines);
        }
    }
}
