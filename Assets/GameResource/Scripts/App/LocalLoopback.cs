using System;
using System.Collections.Generic;
using Backend.Net;

namespace Backend.App
{
    /// <summary>
    /// 소켓 없이 <see cref="MatchRuntime"/> 에 붙는 로컬 전송 허브.
    /// 좌석마다 <see cref="INetTransport"/> / <see cref="NetClient"/> 를 만들고,
    /// 공개 이벤트는 접속 좌석 전원에게, 개인 이벤트는 해당 좌석에만 보낸다.
    /// </summary>
    public sealed class LocalLoopback
    {
        private readonly MatchRuntime _runtime;
        private readonly Func<long> _nowMs;
        private readonly Dictionary<int, LoopbackLink> _links = new Dictionary<int, LoopbackLink>();

        /// <summary>이미 열린 매치 호스트에 루프백을 붙인다.</summary>
        public LocalLoopback(MatchRuntime runtime, Func<long> nowMs = null)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _nowMs = nowMs ?? new Func<long>(() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }

        /// <summary>커맨드를 판결하는 인프로세스 호스트.</summary>
        public MatchRuntime Runtime => _runtime;

        /// <summary>좌석용 <see cref="INetTransport"/> 를 만들거나 교체한다. 아직 Connect 전이다.</summary>
        public INetTransport Attach(int seat)
        {
            EnsureSeat(seat);
            if (_links.TryGetValue(seat, out var previous))
            {
                previous.Detach();
            }

            var link = new LoopbackLink(this, seat);
            _links[seat] = link;
            return link;
        }

        /// <summary>
        /// 좌석 <see cref="NetClient"/> 를 만들고 Connect 한다.
        /// 재접속 스냅샷은 <see cref="NetClient.Reconnect"/> / SnapshotRequest 경로를 쓴다.
        /// </summary>
        public NetClient CreateClient(int seat)
        {
            var client = new NetClient(Attach(seat), seat);
            client.Connect();
            return client;
        }

        /// <summary>만료 턴·유예를 적용하고 접속 좌석에 이벤트를 분배한다.</summary>
        public void Pump()
        {
            Pump(NowMs());
        }

        /// <summary>지정 시각으로 호스트를 한 번 돌린다.</summary>
        public void Pump(long nowMs)
        {
            DeliverAll(_runtime.Pump(nowMs));
        }

        /// <summary>기획서 §6 SnapshotRequest 를 호스트에 넣어 재접속 스냅샷을 보낸다.</summary>
        public void SubmitSnapshotRequest(int seat)
        {
            SubmitSnapshotRequest(seat, NowMs());
        }

        /// <summary>지정 시각으로 SnapshotRequest 를 넣는다.</summary>
        public void SubmitSnapshotRequest(int seat, long nowMs)
        {
            EnsureSeat(seat);
            DeliverAll(_runtime.Submit(CommandMessage.SnapshotRequest(0, seat), nowMs));
        }

        /// <summary>좌석을 끊고 PlayerDisconnected 를 분배한다.</summary>
        public void DisconnectSeat(int seat)
        {
            DisconnectSeat(seat, NowMs());
        }

        /// <summary>지정 시각으로 좌석을 끊는다.</summary>
        public void DisconnectSeat(int seat, long nowMs)
        {
            EnsureSeat(seat);
            if (_links.TryGetValue(seat, out var link))
            {
                link.SetConnected(false);
            }

            DeliverAll(_runtime.Disconnect(seat, nowMs));
        }

        /// <summary>유예 안이면 재접속한다. 스냅샷은 SnapshotRequest 로 따로 받는다.</summary>
        public void RejoinSeat(int seat)
        {
            RejoinSeat(seat, NowMs());
        }

        /// <summary>지정 시각으로 재접속한다.</summary>
        public void RejoinSeat(int seat, long nowMs)
        {
            EnsureSeat(seat);
            DeliverAll(_runtime.Rejoin(seat, nowMs));
            if (_links.TryGetValue(seat, out var link))
            {
                link.SetConnected(true);
            }
        }

        private void ConnectLink(LoopbackLink link)
        {
            if (link.IsConnected)
            {
                return;
            }

            var nowMs = NowMs();
            if (!_runtime.IsSeatConnected(link.Seat))
            {
                DeliverAll(_runtime.Rejoin(link.Seat, nowMs));
            }

            link.SetConnected(true);
        }

        private void DisconnectLink(LoopbackLink link)
        {
            if (!link.IsConnected)
            {
                return;
            }

            link.SetConnected(false);
            DeliverAll(_runtime.Disconnect(link.Seat, NowMs()));
        }

        private void SendFrom(LoopbackLink link, CommandMessage command)
        {
            if (!link.IsConnected || command == null)
            {
                return;
            }

            command.seat = link.Seat;
            DeliverAll(_runtime.Submit(command, NowMs()));
        }

        private void DeliverAll(IReadOnlyList<EventMessage> events)
        {
            if (events == null || events.Count == 0 || _links.Count == 0)
            {
                return;
            }

            foreach (var pair in _links)
            {
                var link = pair.Value;
                if (!link.IsConnected)
                {
                    continue;
                }

                var visible = MatchRuntime.EventsForSeat(events, link.Seat);
                for (var i = 0; i < visible.Length; i++)
                {
                    link.Push(visible[i]);
                }
            }
        }

        private long NowMs()
        {
            return _nowMs();
        }

        private void EnsureSeat(int seat)
        {
            if (seat < 0 || seat >= _runtime.SeatCount)
            {
                throw new ArgumentOutOfRangeException(nameof(seat));
            }
        }

        private sealed class LoopbackLink : INetTransport
        {
            private readonly LocalLoopback _host;
            private bool _detached;

            public LoopbackLink(LocalLoopback host, int seat)
            {
                _host = host;
                Seat = seat;
            }

            public int Seat { get; }

            public bool IsConnected { get; private set; }

            public event Action<EventMessage> EventReceived;

            public void Connect()
            {
                if (_detached)
                {
                    return;
                }

                _host.ConnectLink(this);
            }

            public void Disconnect()
            {
                if (_detached)
                {
                    return;
                }

                _host.DisconnectLink(this);
            }

            public void Send(CommandMessage command)
            {
                if (_detached)
                {
                    return;
                }

                _host.SendFrom(this, command);
            }

            public void SetConnected(bool connected)
            {
                IsConnected = connected;
            }

            public void Detach()
            {
                _detached = true;
                IsConnected = false;
            }

            public void Push(EventMessage ev)
            {
                EventReceived?.Invoke(ev);
            }
        }
    }
}
