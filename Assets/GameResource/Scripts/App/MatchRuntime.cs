using System;
using System.Collections.Generic;
using Backend.Net;
using Game.Rules;
using NetReject = Backend.Net.RejectCode;

namespace Backend.App
{
    /// <summary>
    /// 인프로세스 매치 호스트. 커맨드는 좌석당이 아니라 매치당 한 큐로 직렬 처리한다.
    /// 수 판결은 <see cref="RuleEngine"/> 만 사용한다. 시드는 이벤트에 넣지 않는다.
    /// 공개 <see cref="EventMessage"/> 에는 타인 손패 def 를 넣지 않고,
    /// HandGranted / CardDrawn / CardsReceived / Reject 만 좌석 개인으로 보낸다.
    /// </summary>
    public sealed class MatchRuntime
    {
        /// <summary>기획서 §5 재접속 유예(초).</summary>
        public const int ReconnectGraceSeconds = 45;

        /// <summary>기획서 §9 채팅 레이트(ms).</summary>
        public const int ChatRateLimitMs = 1200;

        /// <summary>기획서 §9 본문 최대 글자.</summary>
        public const int ChatMaxChars = 80;

        /// <summary>재접속 스냅샷에 넣는 최근 채팅 줄 수.</summary>
        public const int ChatHistoryMax = 50;

        /// <summary>기획서 §7 재대결 투표 제한(초). 미투표는 반대.</summary>
        public const int RematchSeconds = 20;

        private readonly Queue<CommandMessage> _commandQueue = new Queue<CommandMessage>();
        private readonly List<EventMessage> _emitted = new List<EventMessage>();
        private readonly List<EventMessage> _chatHistory = new List<EventMessage>();
        private readonly HouseRules _rules;
        private readonly string _roomCode;
        private readonly string[] _nicks;
        private readonly bool[] _ready;
        private readonly bool[] _connected;
        private readonly long[] _graceDeadlineMs;
        private readonly long[] _lastChatMs;
        private readonly bool[] _rematchYes;
        private readonly bool[] _rematchVoted;

        private int _seed;
        private string _phase;
        private MatchState _state;
        private long _turnDeadlineMs;
        private long _rematchDeadlineMs;
        private int _eventSeq;
        private bool _processing;
        private bool _matchEndedEmitted;

        /// <summary>
        /// 대기실 매치를 연다. seed 는 Deal 에만 쓰고 클라 이벤트로 내보내지 않는다.
        /// </summary>
        public MatchRuntime(
            int seatCount,
            int seed,
            string roomCode = null,
            int hostSeat = 0,
            HouseRules rules = null,
            string[] nicks = null,
            bool connectAllSeats = true)
        {
            if (seatCount < HouseRules.MinSeats || seatCount > HouseRules.MaxSeats)
            {
                throw new ArgumentOutOfRangeException(nameof(seatCount), seatCount, "Seat count must be 2..6.");
            }

            if (hostSeat < 0 || hostSeat >= seatCount)
            {
                throw new ArgumentOutOfRangeException(nameof(hostSeat));
            }

            SeatCount = seatCount;
            HostSeat = hostSeat;
            _seed = seed;
            _roomCode = string.IsNullOrEmpty(roomCode) ? "000000" : roomCode;
            _rules = rules ?? HouseRules.Official;
            _phase = MatchPhase.Waiting;
            _nicks = new string[seatCount];
            _ready = new bool[seatCount];
            _connected = new bool[seatCount];
            _graceDeadlineMs = new long[seatCount];
            _lastChatMs = new long[seatCount];
            _rematchYes = new bool[seatCount];
            _rematchVoted = new bool[seatCount];
            for (var i = 0; i < seatCount; i++)
            {
                _nicks[i] = nicks != null && i < nicks.Length && !string.IsNullOrEmpty(nicks[i])
                    ? nicks[i]
                    : "P" + i;
                _connected[i] = connectAllSeats;
            }
        }

        /// <summary>좌석 수. 생성 시 상한이며, 시작 직전 접속 인원으로 줄어들 수 있다.</summary>
        public int SeatCount { get; private set; }

        /// <summary>방장 좌석.</summary>
        public int HostSeat { get; private set; }

        /// <summary>Waiting / Starting / InMatch / Result.</summary>
        public string Phase => _phase;

        /// <summary>현재 턴·선택 마감(unix ms). 매치 전이면 0.</summary>
        public long TurnDeadlineMs => _turnDeadlineMs;

        /// <summary>시작된 매치. 대기실이면 null. 시드 필드는 이벤트에 복사하지 않는다.</summary>
        public MatchState Match => _state;

        /// <summary>커맨드를 매치 큐 뒤에 넣는다. 판정은 <see cref="Pump"/> 가 직렬로 한다.</summary>
        public void Enqueue(CommandMessage command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            _commandQueue.Enqueue(command);
        }

        /// <summary>
        /// 만료된 턴·유예를 적용한 뒤 큐를 한 커맨드씩 처리한다.
        /// </summary>
        public IReadOnlyList<EventMessage> Pump(long nowMs)
        {
            return RunLocked(nowMs, () => ProcessQueue(nowMs));
        }

        /// <summary>한 커맨드를 큐에 넣고 즉시 직렬 처리한다.</summary>
        public IReadOnlyList<EventMessage> Submit(CommandMessage command, long nowMs)
        {
            Enqueue(command);
            return Pump(nowMs);
        }

        /// <summary>접속 끊김. 유예 마감을 now+45초로 둔다.</summary>
        public IReadOnlyList<EventMessage> Disconnect(int seat, long nowMs)
        {
            return RunLocked(nowMs, () => Disconnect_Internal(seat, nowMs));
        }

        /// <summary>유예 안이면 재접속. 초과면 이미 기권된 좌석만 스냅샷을 받을 수 있다.</summary>
        public IReadOnlyList<EventMessage> Rejoin(int seat, long nowMs)
        {
            return RunLocked(nowMs, () => Rejoin_Internal(seat, nowMs));
        }

        /// <summary>대기실 닉을 넣고 RoomUpdated 를 낸다.</summary>
        public IReadOnlyList<EventMessage> SetNick(int seat, string nick, long nowMs)
        {
            return RunLocked(nowMs, () =>
            {
                EnsureSeat(seat);
                if (!string.IsNullOrEmpty(nick))
                {
                    _nicks[seat] = nick;
                }

                var ev = Ev(EvCode.RoomUpdated, seat);
                ev.room = BuildRoomView();
            });
        }

        /// <summary>해당 좌석이 받을 공개+개인 이벤트만 남긴다.</summary>
        public static EventMessage[] EventsForSeat(IReadOnlyList<EventMessage> events, int seat)
        {
            if (events == null)
            {
                return Array.Empty<EventMessage>();
            }

            var count = 0;
            for (var i = 0; i < events.Count; i++)
            {
                if (IsVisibleToSeat(events[i], seat))
                {
                    count++;
                }
            }

            var filtered = new EventMessage[count];
            var write = 0;
            for (var i = 0; i < events.Count; i++)
            {
                if (IsVisibleToSeat(events[i], seat))
                {
                    filtered[write] = events[i];
                    write++;
                }
            }

            return filtered;
        }

        /// <summary>기획서 §5 공개 뷰. 손패 def·덱 순서·시드 없음.</summary>
        public PublicMatchView CreatePublicMatchView()
        {
            return BuildPublicMatch();
        }

        /// <summary>좌석의 재접속 유예 마감. 접속 중이면 0.</summary>
        public long GetGraceDeadlineMs(int seat)
        {
            EnsureSeat(seat);
            return _connected[seat] ? 0 : _graceDeadlineMs[seat];
        }

        /// <summary>좌석이 현재 접속 중인지.</summary>
        public bool IsSeatConnected(int seat)
        {
            EnsureSeat(seat);
            return _connected[seat];
        }

        #region Queue / clock

        private IReadOnlyList<EventMessage> RunLocked(long nowMs, Action body)
        {
            if (_processing)
            {
                ApplyExpired(nowMs);
                body();
                return Array.Empty<EventMessage>();
            }

            _processing = true;
            try
            {
                ApplyExpired(nowMs);
                body();
                return Drain();
            }
            finally
            {
                _processing = false;
            }
        }

        private void ProcessQueue(long nowMs)
        {
            while (_commandQueue.Count > 0)
            {
                var command = _commandQueue.Dequeue();
                Dispatch(command, nowMs);
                ApplyExpired(nowMs);
            }
        }

        private void Dispatch(CommandMessage command, long nowMs)
        {
            if (command == null || string.IsNullOrEmpty(command.op))
            {
                return;
            }

            if (!IsSeatIndex(command.seat))
            {
                EmitReject(command, NetReject.RoomFull);
                return;
            }

            if (IsGraceExpiredSeat(command.seat) && command.op != OpCode.SnapshotRequest)
            {
                EmitReject(command, NetReject.GraceExpired);
                return;
            }

            if (!_connected[command.seat]
                && command.op != OpCode.Heartbeat
                && command.op != OpCode.SnapshotRequest)
            {
                Rejoin_Internal(command.seat, nowMs);
            }

            switch (command.op)
            {
                case OpCode.Ready:
                    HandleReady(command);
                    return;
                case OpCode.StartMatch:
                    HandleStartMatch(command, nowMs);
                    return;
                case OpCode.PlayCard:
                    ApplyRule(command, nowMs, state => RuleEngine.PlayCard(state, command.seat, command.instanceId));
                    return;
                case OpCode.Draw:
                    ApplyRule(command, nowMs, state => RuleEngine.DrawCard(state, command.seat));
                    return;
                case OpCode.ChooseSuit:
                    HandleChooseSuit(command, nowMs);
                    return;
                case OpCode.ChooseQueenMode:
                    HandleChooseQueenMode(command, nowMs);
                    return;
                case OpCode.AcceptQueen:
                    ApplyRule(command, nowMs, state => RuleEngine.AcceptQueen(state, command.seat));
                    return;
                case OpCode.GiveCards:
                    ApplyRule(command, nowMs, state => RuleEngine.GiveCards(state, command.seat, IdsOrEmpty(command.instanceIds)));
                    return;
                case OpCode.ChooseKingMode:
                    HandleChooseKingMode(command, nowMs);
                    return;
                case OpCode.HideUnder:
                    ApplyRule(command, nowMs, state => RuleEngine.HideUnder(state, command.seat, command.instanceId));
                    return;
                case OpCode.MirrorDiscard:
                    ApplyRule(command, nowMs, state => RuleEngine.MirrorDiscard(state, command.seat, IdsOrEmpty(command.instanceIds)));
                    return;
                case OpCode.Surrender:
                    ApplyRule(command, nowMs, state => RuleEngine.Surrender(state, command.seat));
                    return;
                case OpCode.Chat:
                    HandleChat(command, nowMs);
                    return;
                case OpCode.RematchVote:
                    HandleRematchVote(command, nowMs);
                    return;
                case OpCode.Heartbeat:
                    HandleHeartbeat(command, nowMs);
                    return;
                case OpCode.SnapshotRequest:
                    HandleSnapshotRequest(command);
                    return;
            }
        }

        private void ApplyExpired(long nowMs)
        {
            if (_phase == MatchPhase.Result && _rematchDeadlineMs > 0 && nowMs >= _rematchDeadlineMs)
            {
                ResolveRematch();
                return;
            }

            if (_state == null || _state.IsMatchOver)
            {
                return;
            }

            for (var seat = 0; seat < SeatCount; seat++)
            {
                if (_connected[seat] || _graceDeadlineMs[seat] <= 0 || nowMs < _graceDeadlineMs[seat])
                {
                    continue;
                }

                if (!_state.IsSeatActive(seat))
                {
                    continue;
                }

                var before = Capture();
                var result = RuleEngine.Surrender(_state, seat);
                if (result.IsAccepted)
                {
                    EmitAcceptedDiff(null, before, nowMs);
                    _emitted.Add(EventMessage.Reject(NextSeq(), seat, 0, NetReject.GraceExpired));
                }
            }

            if (_state == null || _state.IsMatchOver || _turnDeadlineMs <= 0 || nowMs < _turnDeadlineMs)
            {
                return;
            }

            var acting = _state.ActingSeat;
            if (!_state.IsSeatActive(acting))
            {
                return;
            }

            var timeoutBefore = Capture();
            var timeout = RuleEngine.ApplyTimeout(_state, acting);
            if (timeout.IsAccepted)
            {
                EmitAcceptedDiff(null, timeoutBefore, nowMs);
            }
        }

        #endregion

        #region Room commands

        private void HandleReady(CommandMessage command)
        {
            if (command.protocolMajor != ProtocolVersion.Major
                || command.protocolMinor != ProtocolVersion.Minor)
            {
                EmitReject(command, NetReject.VersionMismatch);
                return;
            }

            if (_phase == MatchPhase.InMatch || _phase == MatchPhase.Starting)
            {
                EmitReject(command, NetReject.MatchAlreadyStarted);
                return;
            }

            _ready[command.seat] = true;
            var ev = Ev(EvCode.RoomUpdated, command.seat);
            ev.ackSeq = command.seq;
            ev.room = BuildRoomView();
        }

        private void HandleStartMatch(CommandMessage command, long nowMs)
        {
            if (command.seat != HostSeat)
            {
                EmitReject(command, NetReject.NotYourTurn);
                return;
            }

            if (_phase == MatchPhase.InMatch || _phase == MatchPhase.Starting || _state != null)
            {
                EmitReject(command, NetReject.MatchAlreadyStarted);
                return;
            }

            if (!AllConnectedReady())
            {
                EmitReject(command, NetReject.NotAllReady);
                return;
            }

            if (!TryShrinkToConnectedSeats())
            {
                EmitReject(command, NetReject.NotAllReady);
                return;
            }

            _phase = MatchPhase.Starting;
            _state = MatchState.Deal(SeatCount, _seed, _rules);
            _matchEndedEmitted = false;
            _rematchDeadlineMs = 0;
            _phase = MatchPhase.InMatch;
            RefreshTurnDeadline(nowMs);

            var started = Ev(EvCode.MatchStarted, _state.CurrentSeat);
            started.ackSeq = command.seq;
            started.deadlineMs = _turnDeadlineMs;
            started.match = BuildPublicMatch();

            for (var seat = 0; seat < SeatCount; seat++)
            {
                EmitHandGranted(seat);
            }

            var jokers = Ev(EvCode.JokerValues, _state.CurrentSeat);
            FillJokerValues(jokers);
            EmitTurnChanged(_state.ActingSeat, command.seq);
            var room = Ev(EvCode.RoomUpdated, HostSeat);
            room.room = BuildRoomView();
        }

        private void HandleChooseSuit(CommandMessage command, long nowMs)
        {
            if (!TryParseSuit(command.suit, out var suit))
            {
                EmitReject(command, NetReject.NeedSuitPick);
                return;
            }

            ApplyRule(command, nowMs, state => RuleEngine.ChooseSuit(state, command.seat, suit));
        }

        private void HandleChooseQueenMode(CommandMessage command, long nowMs)
        {
            if (!TryParseQueenMode(command.queenMode, out var mode))
            {
                EmitReject(command, NetReject.NeedQueenMode);
                return;
            }

            ApplyRule(command, nowMs, state => RuleEngine.ChooseQueenMode(state, command.seat, mode));
        }

        private void HandleChooseKingMode(CommandMessage command, long nowMs)
        {
            if (!TryParseKingMode(command.kingMode, out var mode))
            {
                EmitReject(command, NetReject.NeedKingMode);
                return;
            }

            ApplyRule(command, nowMs, state => RuleEngine.ChooseKingMode(state, command.seat, mode));
        }

        private void HandleChat(CommandMessage command, long nowMs)
        {
            var hasQuick = !string.IsNullOrEmpty(command.quickId);
            var text = command.text ?? string.Empty;
            if (!hasQuick && string.IsNullOrWhiteSpace(text))
            {
                EmitReject(command, NetReject.ChatEmpty);
                return;
            }

            var last = _lastChatMs[command.seat];
            if (last > 0 && nowMs - last < ChatRateLimitMs)
            {
                EmitReject(command, NetReject.ChatRate);
                return;
            }

            if (text.Length > ChatMaxChars)
            {
                text = text.Substring(0, ChatMaxChars);
            }

            _lastChatMs[command.seat] = nowMs;
            var channel = string.IsNullOrEmpty(command.channel)
                ? (_phase == MatchPhase.Waiting ? ChatChannel.Room : ChatChannel.Match)
                : command.channel;
            var ev = Ev(EvCode.Chat, command.seat);
            ev.ackSeq = command.seq;
            ev.text = text;
            ev.quickId = command.quickId;
            ev.channel = channel;
            ev.chatType = hasQuick ? ChatType.Quick : ChatType.User;
            RememberChat(ev);
        }

        private void HandleRematchVote(CommandMessage command, long nowMs)
        {
            if (_phase != MatchPhase.Result)
            {
                EmitReject(command, NetReject.NotYourTurn);
                return;
            }

            _rematchVoted[command.seat] = true;
            _rematchYes[command.seat] = command.rematchYes;
            var ev = Ev(EvCode.RoomUpdated, command.seat);
            ev.ackSeq = command.seq;
            ev.room = BuildRoomView();
            if (AllRematchVoted() || nowMs >= _rematchDeadlineMs)
            {
                ResolveRematch();
            }
        }

        private bool AllRematchVoted()
        {
            for (var i = 0; i < SeatCount; i++)
            {
                if (!_rematchVoted[i])
                {
                    return false;
                }
            }

            return true;
        }

        private void ResolveRematch()
        {
            if (_phase != MatchPhase.Result)
            {
                return;
            }

            var wantRematch = true;
            for (var i = 0; i < SeatCount; i++)
            {
                if (!_connected[i])
                {
                    continue;
                }

                if (!_rematchVoted[i] || !_rematchYes[i])
                {
                    wantRematch = false;
                    break;
                }
            }

            _rematchDeadlineMs = 0;
            for (var i = 0; i < SeatCount; i++)
            {
                _rematchVoted[i] = false;
                _rematchYes[i] = false;
            }

            if (!wantRematch)
            {
                _phase = MatchPhase.Waiting;
                _state = null;
                _matchEndedEmitted = false;
                _turnDeadlineMs = 0;
                for (var i = 0; i < SeatCount; i++)
                {
                    _ready[i] = false;
                }

                var room = Ev(EvCode.RoomUpdated, HostSeat);
                room.room = BuildRoomView();
                return;
            }

            BeginRematchMatch();
        }

        /// <summary>재대결 찬성 시 새 시드로 즉시 한 판을 연다.</summary>
        private void BeginRematchMatch()
        {
            _seed = unchecked(_seed + System.Environment.TickCount + 1);
            if (_seed == 0)
            {
                _seed = 1;
            }

            for (var i = 0; i < SeatCount; i++)
            {
                _ready[i] = _connected[i];
            }

            if (!AllConnectedReady() || !TryShrinkToConnectedSeats())
            {
                _phase = MatchPhase.Waiting;
                _state = null;
                _matchEndedEmitted = false;
                _turnDeadlineMs = 0;
                for (var i = 0; i < SeatCount; i++)
                {
                    _ready[i] = false;
                }

                var room = Ev(EvCode.RoomUpdated, HostSeat);
                room.room = BuildRoomView();
                return;
            }

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _phase = MatchPhase.Starting;
            _state = MatchState.Deal(SeatCount, _seed, _rules);
            _matchEndedEmitted = false;
            _turnDeadlineMs = 0;
            _phase = MatchPhase.InMatch;
            RefreshTurnDeadline(nowMs);

            var started = Ev(EvCode.MatchStarted, _state.CurrentSeat);
            started.deadlineMs = _turnDeadlineMs;
            started.match = BuildPublicMatch();

            for (var seat = 0; seat < SeatCount; seat++)
            {
                EmitHandGranted(seat);
            }

            var jokers = Ev(EvCode.JokerValues, _state.CurrentSeat);
            FillJokerValues(jokers);
            EmitTurnChanged(_state.ActingSeat, 0);
            var roomUpdated = Ev(EvCode.RoomUpdated, HostSeat);
            roomUpdated.room = BuildRoomView();
        }

        private void HandleHeartbeat(CommandMessage command, long nowMs)
        {
            if (!_connected[command.seat])
            {
                Rejoin_Internal(command.seat, nowMs);
            }
        }

        private void HandleSnapshotRequest(CommandMessage command)
        {
            if (_state != null)
            {
                var turn = Ev(EvCode.TurnChanged, command.seat);
                turn.ackSeq = command.seq;
                turn.deadlineMs = _turnDeadlineMs;
                turn.match = BuildPublicMatch();
                EmitHandGranted(command.seat);
            }
            else
            {
                var room = Ev(EvCode.RoomUpdated, command.seat);
                room.ackSeq = command.seq;
                room.room = BuildRoomView();
            }

            var replay = _chatHistory.Count < ChatHistoryMax ? _chatHistory.Count : ChatHistoryMax;
            var start = _chatHistory.Count - replay;
            for (var i = start; i < _chatHistory.Count; i++)
            {
                var src = _chatHistory[i];
                var copy = Ev(EvCode.Chat, src.seat);
                copy.text = src.text;
                copy.quickId = src.quickId;
                copy.channel = src.channel;
                copy.chatType = src.chatType;
            }
        }

        private void Disconnect_Internal(int seat, long nowMs)
        {
            if (!IsSeatIndex(seat) || !_connected[seat])
            {
                return;
            }

            _connected[seat] = false;
            _graceDeadlineMs[seat] = nowMs + ReconnectGraceSeconds * 1000L;
            Ev(EvCode.PlayerDisconnected, seat);
        }

        private void Rejoin_Internal(int seat, long nowMs)
        {
            if (!IsSeatIndex(seat) || _connected[seat])
            {
                return;
            }

            if (_graceDeadlineMs[seat] > 0 && nowMs >= _graceDeadlineMs[seat]
                && _state != null && _state.IsSeatSurrendered(seat))
            {
                var reject = EventMessage.Reject(NextSeq(), seat, 0, NetReject.GraceExpired);
                _emitted.Add(reject);
            }

            _connected[seat] = true;
            _graceDeadlineMs[seat] = 0;
            Ev(EvCode.PlayerRejoined, seat);
        }

        #endregion

        #region RuleEngine apply + event diff

        private void ApplyRule(CommandMessage command, long nowMs, Func<MatchState, RuleResult> apply)
        {
            if (_state == null || _phase != MatchPhase.InMatch)
            {
                EmitReject(command, NetReject.NotYourTurn);
                return;
            }

            var before = Capture();
            var result = apply(_state);
            if (!result.IsAccepted)
            {
                EmitReject(command, result.Reject);
                return;
            }

            EmitAcceptedDiff(command, before, nowMs);
        }

        private void EmitAcceptedDiff(CommandMessage command, StateSnap before, long nowMs)
        {
            var after = Capture();
            var ack = command != null ? command.seq : 0;

            EmitCardMoves(before, after, ack);
            EmitChoiceAndLockEvents(command, before, after, ack);

            for (var seat = 0; seat < SeatCount; seat++)
            {
                if (!before.Surrendered[seat] && after.Surrendered[seat])
                {
                    Ev(EvCode.PlayerOut, seat);
                }
            }

            if (!after.MatchOver)
            {
                RefreshTurnDeadline(nowMs);
                EmitTurnChanged(after.ActingSeat, ack);
            }
            else
            {
                _turnDeadlineMs = 0;
                EmitMatchEnded(ack, nowMs);
            }
        }

        private void EmitCardMoves(StateSnap before, StateSnap after, int ack)
        {
            var drewFromDeck = new int[SeatCount];
            for (var seat = 0; seat < SeatCount; seat++)
            {
                var gained = GainedIds(before.HandIds[seat], after.HandIds[seat]);
                var lost = GainedIds(after.HandIds[seat], before.HandIds[seat]);

                // 알약처럼 같은 행동에서 내고 뽑으면, 내기 먼저 보내야 드로우 연출이 취소되지 않는다.
                for (var i = 0; i < lost.Length; i++)
                {
                    var id = lost[i];
                    if (after.HiddenIds.Contains(id) && !before.HiddenIds.Contains(id))
                    {
                        var hidden = Ev(EvCode.KingHidden, seat);
                        hidden.ackSeq = ack;
                        hidden.instanceId = id;
                        hidden.instanceIds = new[] { id };
                        continue;
                    }

                    if (WasMovedToOtherHand(after, seat, id))
                    {
                        continue;
                    }

                    var played = Ev(EvCode.CardPlayed, seat);
                    played.ackSeq = ack;
                    played.instanceId = id;
                    played.defId = DefIdOf(id);
                }

                if (gained.Length == 0)
                {
                    continue;
                }

                var fromDeck = new List<int>();
                var fromSeats = new List<int>();
                for (var i = 0; i < gained.Length; i++)
                {
                    if (WasInAnyHand(before, gained[i]) || before.PendingGive >= 0)
                    {
                        fromSeats.Add(gained[i]);
                    }
                    else
                    {
                        fromDeck.Add(gained[i]);
                    }
                }

                if (fromDeck.Count > 0)
                {
                    EmitCardDrawn(seat, fromDeck);
                    drewFromDeck[seat] = fromDeck.Count;
                }

                if (fromSeats.Count > 0)
                {
                    EmitCardsReceived(seat, fromSeats);
                }
            }

            for (var seat = 0; seat < SeatCount; seat++)
            {
                if (drewFromDeck[seat] <= 0)
                {
                    continue;
                }

                var drew = Ev(EvCode.DrewCount, seat);
                drew.ackSeq = ack;
                drew.count = drewFromDeck[seat];
            }

            if (MovedBetweenHands(before, after))
            {
                var given = Ev(EvCode.QueenGiven, after.ActingSeat);
                given.ackSeq = ack;
                given.fromSeat = before.PendingGive >= 0 ? before.PendingGive : before.ActingSeat;
                given.toSeat = before.GiveTarget >= 0 ? before.GiveTarget : after.ActingSeat;
                given.count = CountMoved(before, after);
                // 지급은 CardPlayed 가 아니라서 준 좌석이 손패를 빼려면 instanceIds 가 필요하다.
                if (given.fromSeat >= 0 && given.fromSeat < before.HandIds.Length)
                {
                    given.instanceIds = GainedIds(after.HandIds[given.fromSeat], before.HandIds[given.fromSeat]);
                }
            }

            if (before.MirrorTarget != after.MirrorTarget
                || before.PendingMirror != after.PendingMirror
                || (before.MirrorTarget > 0 && !SameCounts(before.HandCounts, after.HandCounts)))
            {
                for (var seat = 0; seat < SeatCount; seat++)
                {
                    if (!SameCounts(before.HandIds[seat], after.HandIds[seat]))
                    {
                        EmitHandGranted(seat);
                    }
                }

                var mirror = Ev(EvCode.MirrorAdjusted, after.ActingSeat);
                mirror.ackSeq = ack;
                mirror.count = after.MirrorTarget;
                mirror.match = BuildPublicMatch();
            }
        }

        private void EmitChoiceAndLockEvents(CommandMessage command, StateSnap before, StateSnap after, int ack)
        {
            if (before.PendingQueen >= 0 && after.PendingQueen < 0)
            {
                var ev = Ev(EvCode.QueenModeChosen, before.PendingQueen);
                ev.ackSeq = ack;
                ev.queenMode = command != null && command.op == OpCode.ChooseQueenMode && !string.IsNullOrEmpty(command.queenMode)
                    ? command.queenMode
                    : after.QueenStack > before.QueenStack
                        ? QueenModeName.Give
                        : QueenModeName.Reverse;
                ev.match = BuildPublicMatch();
            }

            if (before.PendingKing >= 0 && after.PendingKing < 0 && (after.KingExtra || after.PendingHide >= 0))
            {
                var ev = Ev(EvCode.KingModeChosen, before.PendingKing);
                ev.ackSeq = ack;
                ev.kingMode = after.KingExtra ? KingModeName.Extra : KingModeName.Hide;
            }

            if (before.PendingSuit >= 0 && after.PendingSuit < 0)
            {
                var ev = Ev(EvCode.SuitChanged, before.PendingSuit);
                ev.ackSeq = ack;
                ev.suit = after.RequiredSuit;
                ev.match = BuildPublicMatch();
            }

            if (before.RequiredColor != after.RequiredColor)
            {
                var ev = Ev(EvCode.ColorLock, after.ActingSeat);
                ev.ackSeq = ack;
                ev.color = after.RequiredColor;
            }

            if (before.JokerColor != after.JokerColor
                || before.JokerBw != after.JokerBw
                || before.JokerMoon != after.JokerMoon)
            {
                var ev = Ev(EvCode.JokerValues, after.ActingSeat);
                ev.ackSeq = ack;
                ev.jokerColor = after.JokerColor;
                ev.jokerBw = after.JokerBw;
                ev.jokerMoon = after.JokerMoon;
            }
        }

        private void EmitHandGranted(int seat)
        {
            var hand = _state.Hands[seat];
            var ev = Ev(EvCode.HandGranted, seat);
            FillPrivateHand(ev, hand);
        }

        private void EmitCardDrawn(int seat, List<int> instanceIds)
        {
            var ev = Ev(EvCode.CardDrawn, seat);
            FillPrivateIds(ev, instanceIds);
        }

        private void EmitCardsReceived(int seat, List<int> instanceIds)
        {
            var ev = Ev(EvCode.CardsReceived, seat);
            FillPrivateIds(ev, instanceIds);
        }

        private void EmitTurnChanged(int seat, int ack)
        {
            var ev = Ev(EvCode.TurnChanged, seat);
            ev.ackSeq = ack;
            ev.deadlineMs = _turnDeadlineMs;
            ev.match = BuildPublicMatch();
        }

        private void EmitMatchEnded(int ack, long nowMs)
        {
            if (_matchEndedEmitted)
            {
                return;
            }

            _matchEndedEmitted = true;
            _phase = MatchPhase.Result;
            _rematchDeadlineMs = nowMs + RematchSeconds * 1000L;
            for (var i = 0; i < SeatCount; i++)
            {
                _rematchVoted[i] = false;
                _rematchYes[i] = false;
            }
            var standings = RuleEngine.ComputeStandings(_state);
            var ranks = new int[SeatCount];
            var scores = new int[SeatCount];
            var counts = new int[SeatCount];
            for (var i = 0; i < standings.Length; i++)
            {
                var row = standings[i];
                ranks[row.Seat] = row.Rank;
                scores[row.Seat] = row.Score;
                counts[row.Seat] = row.CardCount;
            }

            var ev = Ev(EvCode.MatchEnded, 0);
            ev.ackSeq = ack;
            ev.deadlineMs = _rematchDeadlineMs;
            ev.result = new MatchEndView
            {
                ranks = ranks,
                scores = scores,
                handCounts = counts,
            };
            ev.match = BuildPublicMatch();
            var room = Ev(EvCode.RoomUpdated, HostSeat);
            room.room = BuildRoomView();
        }

        private void FillPrivateHand(EventMessage ev, List<CardInstance> hand)
        {
            var ids = new int[hand.Count];
            var defs = new string[hand.Count];
            for (var i = 0; i < hand.Count; i++)
            {
                ids[i] = hand[i].InstanceId;
                defs[i] = hand[i].Def.Id;
            }

            ev.instanceIds = ids;
            ev.defIds = defs;
        }

        private void FillPrivateIds(EventMessage ev, List<int> instanceIds)
        {
            var ids = new int[instanceIds.Count];
            var defs = new string[instanceIds.Count];
            for (var i = 0; i < instanceIds.Count; i++)
            {
                ids[i] = instanceIds[i];
                defs[i] = DefIdOf(instanceIds[i]);
            }

            ev.instanceIds = ids;
            ev.defIds = defs;
        }

        #endregion

        #region Public view (no foreign hands)

        private PublicMatchView BuildPublicMatch()
        {
            if (_state == null)
            {
                return null;
            }

            var recent = _state.GetPublicRecentDiscard();
            var recentIds = new string[recent.Length];
            for (var i = 0; i < recent.Length; i++)
            {
                recentIds[i] = recent[i].Def.Id;
            }

            var counts = new int[SeatCount];
            for (var seat = 0; seat < SeatCount; seat++)
            {
                counts[seat] = _state.Hands[seat].Count;
            }

            return new PublicMatchView
            {
                discardTop = _state.DiscardTop.Def.Id,
                requiredSuit = ToSuitCode(_state.RequiredSuit),
                requiredColor = ToColorCode(_state.RequiredColor),
                attackDefendSuit = ToSuitCode(_state.AttackDefendSuit),
                attackDefendColor = ToColorCode(_state.AttackDefendColor),
                attackDefendRank = ToRankCode(_state.AttackDefendRank),
                jokerColor = _state.JokerAttack.Color,
                jokerBw = _state.JokerAttack.Bw,
                jokerMoon = _state.JokerAttack.Moon,
                direction = _state.Direction,
                currentSeat = _state.ActingSeat,
                deadlineMs = _turnDeadlineMs,
                handCounts = counts,
                attackStack = _state.AttackStack,
                spearInStack = _state.SpearInStack,
                queenStack = _state.QueenStack,
                pendingGive = _state.PendingGiveSeat.HasValue,
                deckCount = _state.Deck.Count,
                recentDiscard = recentIds,
                jokerDefendable = _state.Rules.JokerDefendable,
            };
        }

        private RoomView BuildRoomView()
        {
            var ready = new bool[SeatCount];
            Array.Copy(_ready, ready, SeatCount);
            var nicks = new string[SeatCount];
            for (var i = 0; i < SeatCount; i++)
            {
                nicks[i] = _connected[i] ? _nicks[i] : string.Empty;
            }

            return new RoomView
            {
                roomCode = _roomCode,
                phase = _phase,
                nicks = nicks,
                ready = ready,
                hostSeat = HostSeat,
                seatCount = SeatCount,
            };
        }

        #endregion

        #region Snapshot / helpers

        private StateSnap Capture()
        {
            var snap = new StateSnap
            {
                ActingSeat = _state.ActingSeat,
                QueenStack = _state.QueenStack,
                KingExtra = _state.KingExtraPending,
                RequiredSuit = ToSuitCode(_state.RequiredSuit),
                RequiredColor = ToColorCode(_state.RequiredColor),
                JokerColor = _state.JokerAttack.Color,
                JokerBw = _state.JokerAttack.Bw,
                JokerMoon = _state.JokerAttack.Moon,
                PendingSuit = _state.PendingSuitSeat ?? -1,
                PendingQueen = _state.PendingQueenModeSeat ?? -1,
                PendingGive = _state.PendingGiveSeat ?? -1,
                GiveTarget = _state.QueenGiveTargetSeat ?? -1,
                PendingKing = _state.PendingKingModeSeat ?? -1,
                PendingHide = _state.PendingHideUnderSeat ?? -1,
                PendingMirror = _state.PendingMirrorSeat ?? -1,
                MirrorTarget = _state.MirrorTargetCount,
                MatchOver = _state.IsMatchOver,
                HandCounts = new int[SeatCount],
                HandIds = new int[SeatCount][],
                Surrendered = new bool[SeatCount],
                HiddenIds = new HashSet<int>(_state.HiddenDiscardIds),
            };

            for (var seat = 0; seat < SeatCount; seat++)
            {
                var hand = _state.Hands[seat];
                snap.HandCounts[seat] = hand.Count;
                snap.HandIds[seat] = new int[hand.Count];
                for (var i = 0; i < hand.Count; i++)
                {
                    snap.HandIds[seat][i] = hand[i].InstanceId;
                }

                snap.Surrendered[seat] = _state.IsSeatSurrendered(seat);
            }

            return snap;
        }

        private string DefIdOf(int instanceId)
        {
            return _state.Catalog.GetInstance(instanceId).Def.Id;
        }

        private void FillJokerValues(EventMessage ev)
        {
            ev.jokerColor = _state.JokerAttack.Color;
            ev.jokerBw = _state.JokerAttack.Bw;
            ev.jokerMoon = _state.JokerAttack.Moon;
        }

        private void RefreshTurnDeadline(long nowMs)
        {
            _turnDeadlineMs = nowMs + _state.Rules.TurnSeconds * 1000L;
        }

        private EventMessage Ev(string ev, int seat)
        {
            var message = EventMessage.Create(ev, NextSeq(), seat);
            _emitted.Add(message);
            return message;
        }

        private void EmitReject(CommandMessage command, string reject)
        {
            _emitted.Add(EventMessage.Reject(NextSeq(), command.seat, command.seq, reject));
        }

        private int NextSeq()
        {
            _eventSeq += 1;
            return _eventSeq;
        }

        private EventMessage[] Drain()
        {
            var arr = _emitted.ToArray();
            _emitted.Clear();
            return arr;
        }

        private void RememberChat(EventMessage ev)
        {
            _chatHistory.Add(ev);
            while (_chatHistory.Count > ChatHistoryMax)
            {
                _chatHistory.RemoveAt(0);
            }
        }

        private void EnsureSeat(int seat)
        {
            if (!IsSeatIndex(seat))
            {
                throw new ArgumentOutOfRangeException(nameof(seat));
            }
        }

        private bool IsSeatIndex(int seat)
        {
            return seat >= 0 && seat < SeatCount;
        }

        private bool AllConnectedReady()
        {
            var connected = 0;
            for (var i = 0; i < SeatCount; i++)
            {
                if (!_connected[i])
                {
                    continue;
                }

                connected++;
                if (!_ready[i])
                {
                    return false;
                }
            }

            return connected >= HouseRules.MinSeats;
        }

        /// <summary>
        /// 접속 중인 좌석만 남긴다. 빈 칸(6인 방·2명 시작)을 Deal 인원에서 뺀다.
        /// PlayHost 좌석 맵과 맞추려면 접속 좌석이 앞쪽부터 빈틈 없이 있어야 한다.
        /// </summary>
        private bool TryShrinkToConnectedSeats()
        {
            var connectedCount = 0;
            for (var i = 0; i < SeatCount; i++)
            {
                if (_connected[i])
                {
                    connectedCount++;
                }
            }

            if (connectedCount < HouseRules.MinSeats || connectedCount > SeatCount)
            {
                return false;
            }

            for (var i = 0; i < connectedCount; i++)
            {
                if (!_connected[i])
                {
                    return false;
                }
            }

            for (var i = connectedCount; i < SeatCount; i++)
            {
                if (_connected[i])
                {
                    return false;
                }
            }

            if (connectedCount == SeatCount)
            {
                return true;
            }

            SeatCount = connectedCount;
            if (HostSeat >= SeatCount)
            {
                HostSeat = 0;
            }

            return true;
        }

        private bool IsGraceExpiredSeat(int seat)
        {
            return _state != null
                && _state.IsSeatSurrendered(seat)
                && !_connected[seat];
        }

        private static bool IsVisibleToSeat(EventMessage ev, int seat)
        {
            return ev != null && (!ev.isPrivate || ev.seat == seat);
        }

        private static int[] IdsOrEmpty(int[] ids)
        {
            return ids ?? Array.Empty<int>();
        }

        private static int[] GainedIds(int[] previous, int[] next)
        {
            var extras = new List<int>();
            for (var i = 0; i < next.Length; i++)
            {
                if (!ContainsId(previous, next[i]))
                {
                    extras.Add(next[i]);
                }
            }

            return extras.ToArray();
        }

        private static bool ContainsId(int[] ids, int id)
        {
            for (var i = 0; i < ids.Length; i++)
            {
                if (ids[i] == id)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool WasInAnyHand(StateSnap snap, int instanceId)
        {
            for (var seat = 0; seat < snap.HandIds.Length; seat++)
            {
                if (ContainsId(snap.HandIds[seat], instanceId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool WasMovedToOtherHand(StateSnap after, int fromSeat, int instanceId)
        {
            for (var seat = 0; seat < after.HandIds.Length; seat++)
            {
                if (seat != fromSeat && ContainsId(after.HandIds[seat], instanceId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MovedBetweenHands(StateSnap before, StateSnap after)
        {
            for (var seat = 0; seat < before.HandIds.Length; seat++)
            {
                var gained = GainedIds(before.HandIds[seat], after.HandIds[seat]);
                for (var i = 0; i < gained.Length; i++)
                {
                    if (WasInAnyHand(before, gained[i]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static int CountMoved(StateSnap before, StateSnap after)
        {
            var count = 0;
            for (var seat = 0; seat < before.HandIds.Length; seat++)
            {
                var gained = GainedIds(before.HandIds[seat], after.HandIds[seat]);
                for (var i = 0; i < gained.Length; i++)
                {
                    if (WasInAnyHand(before, gained[i]))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static bool SameCounts(int[] a, int[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            for (var i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryParseSuit(string code, out Suit suit)
        {
            switch (code)
            {
                case SuitCode.Spade:
                    suit = Suit.Spade;
                    return true;
                case SuitCode.Heart:
                    suit = Suit.Heart;
                    return true;
                case SuitCode.Diamond:
                    suit = Suit.Diamond;
                    return true;
                case SuitCode.Club:
                    suit = Suit.Club;
                    return true;
                case SuitCode.Star:
                    suit = Suit.Star;
                    return true;
                case SuitCode.Moon:
                    suit = Suit.Moon;
                    return true;
                default:
                    suit = Suit.None;
                    return false;
            }
        }

        private static bool TryParseQueenMode(string name, out QueenMode mode)
        {
            if (name == QueenModeName.Reverse)
            {
                mode = QueenMode.Reverse;
                return true;
            }

            if (name == QueenModeName.Give)
            {
                mode = QueenMode.Give;
                return true;
            }

            mode = QueenMode.Reverse;
            return false;
        }

        private static bool TryParseKingMode(string name, out KingMode mode)
        {
            if (name == KingModeName.Extra)
            {
                mode = KingMode.Extra;
                return true;
            }

            if (name == KingModeName.Hide)
            {
                mode = KingMode.Hide;
                return true;
            }

            mode = KingMode.Extra;
            return false;
        }

        private static string ToSuitCode(Suit? suit)
        {
            if (!suit.HasValue)
            {
                return null;
            }

            switch (suit.Value)
            {
                case Suit.Spade:
                    return SuitCode.Spade;
                case Suit.Heart:
                    return SuitCode.Heart;
                case Suit.Diamond:
                    return SuitCode.Diamond;
                case Suit.Club:
                    return SuitCode.Club;
                case Suit.Star:
                    return SuitCode.Star;
                case Suit.Moon:
                    return SuitCode.Moon;
                default:
                    return null;
            }
        }

        private static string ToColorCode(ColorGroup? color)
        {
            if (!color.HasValue)
            {
                return null;
            }

            switch (color.Value)
            {
                case ColorGroup.Black:
                    return ColorCode.Black;
                case ColorGroup.Red:
                    return ColorCode.Red;
                case ColorGroup.Blue:
                    return ColorCode.Blue;
                default:
                    return null;
            }
        }

        private static string ToRankCode(Rank? rank)
        {
            if (!rank.HasValue)
            {
                return null;
            }

            switch (rank.Value)
            {
                case Rank.Ace:
                    return RankCode.Ace;
                case Rank.Two:
                    return RankCode.Two;
                default:
                    return null;
            }
        }

        #endregion

        private sealed class StateSnap
        {
            public int ActingSeat;
            public int QueenStack;
            public bool KingExtra;
            public string RequiredSuit;
            public string RequiredColor;
            public int JokerColor;
            public int JokerBw;
            public int JokerMoon;
            public int PendingSuit;
            public int PendingQueen;
            public int PendingGive;
            public int GiveTarget;
            public int PendingKing;
            public int PendingHide;
            public int PendingMirror;
            public int MirrorTarget;
            public bool MatchOver;
            public int[] HandCounts;
            public int[][] HandIds;
            public bool[] Surrendered;
            public HashSet<int> HiddenIds;
        }
    }
}
