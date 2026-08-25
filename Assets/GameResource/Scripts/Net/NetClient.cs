using System;
using System.Collections.Generic;

namespace Backend.Net
{
    /// <summary>
    /// 전송 위 클라. seq 를 붙이고 ackSeq 로 대기하며,
    /// 공개 상태·내 손패·채팅 최근 50을 스냅샷/이벤트로 유지한다.
    /// 수는 판결하지 않는다.
    /// </summary>
    public sealed class NetClient
    {
        /// <summary>기획서 §5 재접속 스냅샷에 넣는 최근 채팅 줄 수.</summary>
        public const int ChatHistoryMax = 50;

        private static readonly int[] EmptyIds = Array.Empty<int>();
        private static readonly string[] EmptyDefs = Array.Empty<string>();

        private readonly INetTransport _transport;
        private int _seat;
        private readonly List<EventMessage> _recentChat = new List<EventMessage>(ChatHistoryMax);

        private int _nextSeq = 1;
        private int _lastSentSeq;
        private int _lastAckSeq;
        private int[] _handInstanceIds = EmptyIds;
        private string[] _handDefIds = EmptyDefs;

        /// <summary>
        /// 전송과 좌석을 묶는다. 이벤트는 생성 즉시 구독한다.
        /// </summary>
        public NetClient(INetTransport transport, int seat)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _seat = seat;
            _transport.EventReceived += OnTransportEvent;
        }

        /// <summary>이 클라의 좌석.</summary>
        public int Seat => _seat;

        /// <summary>서버가 배정한 좌석으로 바꾼다. 이후 커맨드 seat 에 쓴다.</summary>
        public void AssignSeat(int seat)
        {
            if (seat < 0)
            {
                return;
            }

            _seat = seat;
        }

        /// <summary>사용 중인 전송.</summary>
        public INetTransport Transport => _transport;

        /// <summary>호스트 접속 여부.</summary>
        public bool IsConnected => _transport.IsConnected;

        /// <summary>마지막으로 보낸 커맨드 seq. 아직 없으면 0.</summary>
        public int LastSentSeq => _lastSentSeq;

        /// <summary>호스트가 ack 한 마지막 커맨드 seq.</summary>
        public int LastAckSeq => _lastAckSeq;

        /// <summary>보낸 커맨드의 ack 를 아직 못 받았으면 true. Heartbeat 는 제외.</summary>
        public bool HasPendingAck => _lastSentSeq > _lastAckSeq;

        /// <summary>마지막 공개 매치 뷰. 시드·타인 손패 없음.</summary>
        public PublicMatchView PublicMatch { get; private set; }

        /// <summary>마지막 대기실 뷰.</summary>
        public RoomView Room { get; private set; }

        /// <summary>서버 deadlineMs. 스냅샷의 남은 턴.</summary>
        public long DeadlineMs { get; private set; }

        /// <summary>내 손패 instanceId. 타인 패는 없다.</summary>
        public IReadOnlyList<int> HandInstanceIds => _handInstanceIds;

        /// <summary>내 손패 defId.</summary>
        public IReadOnlyList<string> HandDefIds => _handDefIds;

        /// <summary>채팅 최근 최대 50줄. 스냅샷 시 호스트 기록으로 교체된다.</summary>
        public IReadOnlyList<EventMessage> RecentChat => _recentChat;

        /// <summary>좌석에 보이는 이벤트가 적용된 뒤 발행한다.</summary>
        public event Action<EventMessage> EventReceived;

        /// <summary>전송을 연다.</summary>
        public void Connect()
        {
            _transport.Connect();
        }

        /// <summary>전송을 끊는다. 호스트는 45초 유예를 둔다.</summary>
        public void Disconnect()
        {
            _transport.Disconnect();
        }

        /// <summary>
        /// 끊었다가 다시 붙인 뒤 SnapshotRequest 로
        /// 공개 상태+내 손패+채팅 최근 50+남은 턴을 받는다.
        /// </summary>
        public void Reconnect()
        {
            if (_transport.IsConnected)
            {
                _transport.Disconnect();
            }

            _transport.Connect();
            RequestSnapshot();
        }

        /// <summary>seq·seat 를 채운 뒤 보낸다. SnapshotRequest 면 로컬 손패·채팅을 비운다.</summary>
        public int Send(CommandMessage command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            command.seq = _nextSeq;
            command.seat = _seat;
            _nextSeq += 1;

            var waitAck = command.op != OpCode.Heartbeat;
            if (waitAck)
            {
                _lastSentSeq = command.seq;
            }

            if (command.op == OpCode.SnapshotRequest)
            {
                ClearSnapshotTargets();
            }

            _transport.Send(command);
            return command.seq;
        }

        /// <summary>기획서 §6 SnapshotRequest. 재접속 스냅샷을 다시 받는다.</summary>
        public int RequestSnapshot()
        {
            return Send(CommandMessage.SnapshotRequest(0, _seat));
        }

        /// <summary>Ready 커맨드를 보낸다.</summary>
        public int Ready()
        {
            return Send(CommandMessage.Ready(0, _seat));
        }

        /// <summary>StartMatch 커맨드를 보낸다.</summary>
        public int StartMatch()
        {
            return Send(CommandMessage.StartMatch(0, _seat));
        }

        /// <summary>PlayCard 커맨드를 보낸다.</summary>
        public int PlayCard(int instanceId)
        {
            return Send(CommandMessage.PlayCard(0, _seat, instanceId));
        }

        /// <summary>Draw 커맨드를 보낸다.</summary>
        public int Draw()
        {
            return Send(CommandMessage.Draw(0, _seat));
        }

        /// <summary>ChooseSuit 커맨드를 보낸다.</summary>
        public int ChooseSuit(string suit)
        {
            return Send(CommandMessage.ChooseSuit(0, _seat, suit));
        }

        /// <summary>ChooseQueenMode 커맨드를 보낸다.</summary>
        public int ChooseQueenMode(string queenMode)
        {
            return Send(CommandMessage.ChooseQueenMode(0, _seat, queenMode));
        }

        /// <summary>AcceptQueen 커맨드를 보낸다.</summary>
        public int AcceptQueen()
        {
            return Send(CommandMessage.AcceptQueen(0, _seat));
        }

        /// <summary>GiveCards 커맨드를 보낸다.</summary>
        public int GiveCards(int[] instanceIds)
        {
            return Send(CommandMessage.GiveCards(0, _seat, instanceIds));
        }

        /// <summary>ChooseKingMode 커맨드를 보낸다.</summary>
        public int ChooseKingMode(string kingMode)
        {
            return Send(CommandMessage.ChooseKingMode(0, _seat, kingMode));
        }

        /// <summary>HideUnder 커맨드를 보낸다.</summary>
        public int HideUnder(int instanceId)
        {
            return Send(CommandMessage.HideUnder(0, _seat, instanceId));
        }

        /// <summary>MirrorDiscard 커맨드를 보낸다.</summary>
        public int MirrorDiscard(int[] instanceIds)
        {
            return Send(CommandMessage.MirrorDiscard(0, _seat, instanceIds));
        }

        /// <summary>Surrender 커맨드를 보낸다.</summary>
        public int Surrender()
        {
            return Send(CommandMessage.Surrender(0, _seat));
        }

        /// <summary>Chat 커맨드를 보낸다.</summary>
        public int Chat(string text, string channel, string quickId = null)
        {
            return Send(CommandMessage.Chat(0, _seat, text, channel, quickId));
        }

        /// <summary>RematchVote 커맨드를 보낸다.</summary>
        public int RematchVote(bool rematchYes)
        {
            return Send(CommandMessage.RematchVote(0, _seat, rematchYes));
        }

        /// <summary>Heartbeat 커맨드를 보낸다. ack 대기를 걸지 않는다.</summary>
        public int Heartbeat()
        {
            return Send(CommandMessage.Heartbeat(0, _seat));
        }

        private void OnTransportEvent(EventMessage ev)
        {
            if (ev == null)
            {
                return;
            }

            if (ev.ackSeq > _lastAckSeq)
            {
                _lastAckSeq = ev.ackSeq;
            }

            ApplyEvent(ev);
            EventReceived?.Invoke(ev);
        }

        private void ApplyEvent(EventMessage ev)
        {
            if (ev.match != null)
            {
                PublicMatch = ev.match;
            }

            if (ev.room != null)
            {
                Room = ev.room;
            }

            if (ev.deadlineMs > 0)
            {
                DeadlineMs = ev.deadlineMs;
            }

            if (ev.ev == EvCode.HandGranted)
            {
                _handInstanceIds = ev.instanceIds ?? EmptyIds;
                _handDefIds = ev.defIds ?? EmptyDefs;
                return;
            }

            if (ev.ev == EvCode.CardDrawn || ev.ev == EvCode.CardsReceived)
            {
                AppendHand(ev.instanceIds, ev.defIds);
                return;
            }

            if (ev.ev == EvCode.CardPlayed && ev.seat == _seat)
            {
                RemoveHand(ev.instanceId);
                return;
            }

            if (ev.ev == EvCode.QueenGiven && ev.fromSeat == _seat)
            {
                RemoveHands(ev.instanceIds);
                return;
            }

            if (ev.ev == EvCode.KingHidden && ev.seat == _seat)
            {
                RemoveHands(ev.instanceIds);
            }

            if (ev.ev == EvCode.Chat)
            {
                RememberChat(ev);
            }
        }

        private void ClearSnapshotTargets()
        {
            _recentChat.Clear();
            _handInstanceIds = EmptyIds;
            _handDefIds = EmptyDefs;
        }

        private void AppendHand(int[] instanceIds, string[] defIds)
        {
            if (instanceIds == null || instanceIds.Length == 0)
            {
                return;
            }

            var add = instanceIds.Length;
            var nextIds = new int[_handInstanceIds.Length + add];
            var nextDefs = new string[nextIds.Length];
            Array.Copy(_handInstanceIds, nextIds, _handInstanceIds.Length);
            var oldDefs = _handDefIds.Length < _handInstanceIds.Length ? _handDefIds.Length : _handInstanceIds.Length;
            Array.Copy(_handDefIds, nextDefs, oldDefs);
            Array.Copy(instanceIds, 0, nextIds, _handInstanceIds.Length, add);
            if (defIds != null)
            {
                var copyDefs = defIds.Length < add ? defIds.Length : add;
                Array.Copy(defIds, 0, nextDefs, _handInstanceIds.Length, copyDefs);
            }

            _handInstanceIds = nextIds;
            _handDefIds = nextDefs;
        }

        private void RemoveHands(int[] instanceIds)
        {
            if (instanceIds == null || instanceIds.Length == 0)
            {
                return;
            }

            for (var i = 0; i < instanceIds.Length; i++)
            {
                RemoveHand(instanceIds[i]);
            }
        }

        private void RemoveHand(int instanceId)
        {
            var index = -1;
            for (var i = 0; i < _handInstanceIds.Length; i++)
            {
                if (_handInstanceIds[i] == instanceId)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                return;
            }

            var nextIds = new int[_handInstanceIds.Length - 1];
            var nextDefs = new string[_handDefIds.Length > 0 ? _handDefIds.Length - 1 : 0];
            var write = 0;
            for (var i = 0; i < _handInstanceIds.Length; i++)
            {
                if (i == index)
                {
                    continue;
                }

                nextIds[write] = _handInstanceIds[i];
                if (write < nextDefs.Length && i < _handDefIds.Length)
                {
                    nextDefs[write] = _handDefIds[i];
                }

                write++;
            }

            _handInstanceIds = nextIds;
            _handDefIds = nextDefs;
        }

        private void RememberChat(EventMessage ev)
        {
            _recentChat.Add(ev);
            while (_recentChat.Count > ChatHistoryMax)
            {
                _recentChat.RemoveAt(0);
            }
        }
    }
}
