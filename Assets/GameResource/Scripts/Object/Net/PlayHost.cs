using System;
using System.Collections.Generic;
using Backend.App;
using Backend.Net;
using Unity.Netcode;
using UnityEngine;

namespace Backend.Object.Net
{
    /// <summary>
    /// 방장 기기에서 <see cref="MatchRuntime"/> 을 돌리고, 게스트는 NGO 메시지로만 붙는다.
    /// 접속 승인은 <see cref="SessionLimits.MaxPlayers"/> 를 넘지 못한다.
    /// </summary>
    public sealed class PlayHost : MonoBehaviour
    {
        private readonly Dictionary<ulong, int> _seatByClient = new Dictionary<ulong, int>();
        private readonly Dictionary<int, ulong> _clientBySeat = new Dictionary<int, ulong>();
        private MatchRuntime _runtime;
        private LocalLoopback _loopback;
        private NetClient _hostClient;
        private NetworkManager _nm;
        private readonly Dictionary<ulong, string> _pendingNicks = new Dictionary<ulong, string>();
        private float _heartbeatAt;
        private bool _relayHost;
        private bool _relayGuest;
        private string _nick;

        /// <summary>방장 좌석 클라. 규칙은 여기서 판결하지 않는다.</summary>
        public NetClient HostClient => _hostClient;

        /// <summary>인프로세스 호스트.</summary>
        public LocalLoopback Loopback => _loopback;

        /// <summary>
        /// NetworkManager 와 호스트 런타임을 이 오브젝트에 붙인다.
        /// </summary>
        public void Bind(NetworkManager nm, string nick)
        {
            _nm = nm;
            _nick = nick ?? string.Empty;
            _nm.ConnectionApprovalCallback += OnApproval;
            _nm.OnClientConnectedCallback += OnClientConnected;
            _nm.OnClientDisconnectCallback += OnClientDisconnected;
        }

        /// <summary>매치 런타임만 연다. 전송 설정 후 <see cref="StartListening"/> 을 부른다.</summary>
        public NetClient PrepareHost(int seatCount, string roomCode)
        {
            var seats = SessionLimits.ClampPlayers(seatCount);
            var seed = UnityEngine.Random.Range(1, int.MaxValue);
            _runtime = new MatchRuntime(seats, seed, roomCode, hostSeat: 0, connectAllSeats: false);
            _loopback = new LocalLoopback(_runtime);
            _hostClient = _loopback.CreateClient(0);
            _loopback.Publish(_runtime.SetNick(0, _nick, NowMs()));
            return _hostClient;
        }

        /// <summary>전송이 준비된 뒤 호스트 또는 클라를 연다.</summary>
        public void StartListening(bool isHost)
        {
            if (_nm == null)
            {
                throw new InvalidOperationException("NetworkManager가 없음");
            }

            if (_nm.IsListening)
            {
                RegisterHandlers();
                if (isHost)
                {
                    _seatByClient[_nm.LocalClientId] = 0;
                    _clientBySeat[0] = _nm.LocalClientId;
                }

                return;
            }

            var ok = isHost ? _nm.StartHost() : _nm.StartClient();
            if (!ok)
            {
                var transport = _nm.NetworkConfig != null ? _nm.NetworkConfig.NetworkTransport : null;
                var hint = transport == null
                    ? " (전송 없음)"
                    : (_nm.IsListening ? " (이미 수신 중)" : " (릴레이 전송 시작 실패)");
                throw new InvalidOperationException(
                    (isHost ? "호스트를 시작하지 못함" : "클라를 시작하지 못함") + hint);
            }

            if (isHost)
            {
                _seatByClient[_nm.LocalClientId] = 0;
                _clientBySeat[0] = _nm.LocalClientId;
            }

            RegisterHandlers();
        }

        /// <summary>Relay 호스트면 로비 하트비트를 켠다.</summary>
        public void MarkRelayHost()
        {
            _relayHost = true;
        }

        /// <summary>Relay 게스트면 종료 시 세션에서 나간다.</summary>
        public void MarkRelayGuest()
        {
            _relayGuest = true;
        }

        /// <summary>턴 초과와 로비 하트비트.</summary>
        public void Pump()
        {
            _loopback?.Pump();
            if (!_relayHost || Time.unscaledTime < _heartbeatAt)
            {
                return;
            }

            _heartbeatAt = Time.unscaledTime + 8f;
            _ = Management.UgsLobbyRelay.HeartbeatHostedAsync();
        }

        /// <summary>네트워크와 로비를 닫는다.</summary>
        public void Shutdown()
        {
            if (_nm != null)
            {
                _nm.OnClientConnectedCallback -= OnClientConnected;
                _nm.OnClientDisconnectCallback -= OnClientDisconnected;
                _nm.ConnectionApprovalCallback -= OnApproval;
                if (_nm.CustomMessagingManager != null)
                {
                    _nm.CustomMessagingManager.UnregisterNamedMessageHandler(PlayClientTransport.CommandChannel);
                    _nm.CustomMessagingManager.UnregisterNamedMessageHandler(PlayClientTransport.EventChannel);
                }

                if (_nm.IsListening)
                {
                    _nm.Shutdown();
                }
            }

            if (_relayHost)
            {
                _ = Management.UgsLobbyRelay.LeaveHostedAsync();
            }

            if (_relayGuest)
            {
                _ = Management.UgsLobbyRelay.LeaveJoinedAsync();
            }

            _relayHost = false;
            _relayGuest = false;
            _runtime = null;
            _loopback = null;
            _hostClient = null;
            _seatByClient.Clear();
            _clientBySeat.Clear();
            _pendingNicks.Clear();
        }

        private void RegisterHandlers()
        {
            _nm.CustomMessagingManager.RegisterNamedMessageHandler(
                PlayClientTransport.CommandChannel,
                OnCommandMessage);
        }

        private void OnApproval(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            response.CreatePlayerObject = false;
            var taken = _nm.ConnectedClientsIds != null ? _nm.ConnectedClientsIds.Count : 0;
            var cap = _runtime != null ? _runtime.SeatCount : SessionLimits.MaxPlayers;
            response.Approved = taken < cap && cap <= SessionLimits.MaxPlayers;
            if (!response.Approved)
            {
                response.Reason = "full";
                return;
            }

            _pendingNicks[request.ClientNetworkId] = TryReadNick(request.Payload);
        }

        private void OnClientConnected(ulong clientId)
        {
            if (_runtime == null || clientId == _nm.LocalClientId)
            {
                return;
            }

            var seat = FirstFreeSeat();
            if (seat < 0)
            {
                _nm.DisconnectClient(clientId);
                return;
            }

            _seatByClient[clientId] = seat;
            _clientBySeat[seat] = clientId;
            _pendingNicks.TryGetValue(clientId, out var nick);
            _pendingNicks.Remove(clientId);
            Deliver(_runtime.Rejoin(seat, NowMs()));
            if (!string.IsNullOrEmpty(nick))
            {
                Deliver(_runtime.SetNick(seat, nick, NowMs()));
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (_runtime == null || !_seatByClient.TryGetValue(clientId, out var seat))
            {
                return;
            }

            _seatByClient.Remove(clientId);
            _clientBySeat.Remove(seat);
            Deliver(_runtime.Disconnect(seat, NowMs()));
        }

        private void OnCommandMessage(ulong sender, FastBufferReader reader)
        {
            if (_runtime == null || !_seatByClient.TryGetValue(sender, out var seat))
            {
                return;
            }

            var json = PlayClientTransport.ReadJson(reader);
            var command = WireJson.DeserializeCommand(json);
            if (command == null)
            {
                return;
            }

            command.seat = seat;
            if (!string.IsNullOrEmpty(command.text) && command.op == OpCode.SnapshotRequest)
            {
                Deliver(_runtime.SetNick(seat, command.text, NowMs()));
            }

            Deliver(_runtime.Submit(command, NowMs()));
        }

        private void Deliver(IReadOnlyList<EventMessage> events)
        {
            if (events == null || events.Count == 0)
            {
                return;
            }

            _loopback?.Publish(events);
            foreach (var pair in _seatByClient)
            {
                if (pair.Key == _nm.LocalClientId)
                {
                    continue;
                }

                var visible = MatchRuntime.EventsForSeat(events, pair.Value);
                for (var i = 0; i < visible.Length; i++)
                {
                    PlayClientTransport.SendNamed(
                        _nm,
                        PlayClientTransport.EventChannel,
                        pair.Key,
                        WireJson.SerializeEvent(visible[i]));
                }
            }
        }

        private int FirstFreeSeat()
        {
            for (var i = 1; i < _runtime.SeatCount; i++)
            {
                if (!_clientBySeat.ContainsKey(i))
                {
                    return i;
                }
            }

            return -1;
        }

        private static long NowMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private static string TryReadNick(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                return string.Empty;
            }

            try
            {
                return System.Text.Encoding.UTF8.GetString(payload);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

    }
}
