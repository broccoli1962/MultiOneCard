using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.App;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Backend.Object.Management
{
    /// <summary>
    /// Unity Lobby + Relay 를 Multiplayer Session 으로 연다.
    /// 할당·로비 인원은 <see cref="SessionLimits"/> 를 넘지 못한다.
    /// </summary>
    public static class UgsLobbyRelay
    {
        public const string DataSeats = "seats";
        private const string SessionType = "onetable";

        private static ISession _hosted;
        private static ISession _joined;
        private static int _hostedCount;
        private static bool _heartbeatBusy;

        /// <summary>Unity Cloud 프로젝트가 연결되어 있는지.</summary>
        public static bool IsProjectLinked => !string.IsNullOrEmpty(Application.cloudProjectId);

        /// <summary>이 기기가 이미 호스트 중인 방 수.</summary>
        public static int HostedCount => _hostedCount;

        /// <summary>호스트 중인 세션의 Unity 조인 코드. 없으면 null.</summary>
        public static string HostedJoinCode => _hosted != null ? _hosted.Code : null;

        /// <summary>익명 로그인까지 끝낸다. 프로젝트 미연결이면 예외.</summary>
        public static async Task EnsureSignedInAsync()
        {
            if (!IsProjectLinked)
            {
                throw new InvalidOperationException("Unity 프로젝트에 Cloud를 연결하세요");
            }

            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            var auth = AuthenticationService.Instance;
            if (auth == null)
            {
                throw new InvalidOperationException(
                    "Authentication을 사용할 수 없습니다. Dashboard에서 Player Authentication(Anonymous)을 활성화하세요");
            }

            // 같은 PC에서 에디터+빌드·빌드 2개를 켜면 PlayerPrefs 세션을 공유해
            // 동일 플레이어로 로비에 두 번 들어가 실패한다. 프로세스별 프로필로 분리한다.
            EnsureProcessAuthProfile(auth);

            if (!auth.IsSignedIn)
            {
                await auth.SignInAnonymouslyAsync();
            }
        }

        private static void EnsureProcessAuthProfile(IAuthenticationService auth)
        {
            var profile = "p" + System.Diagnostics.Process.GetCurrentProcess().Id;
            if (profile.Length > 30)
            {
                profile = profile.Substring(0, 30);
            }

            if (string.Equals(auth.Profile, profile, StringComparison.Ordinal))
            {
                return;
            }

            if (auth.IsSignedIn)
            {
                auth.SignOut();
            }

            auth.SwitchProfile(profile);
        }

        private static IMultiplayerService RequireMultiplayer()
        {
            var service = MultiplayerService.Instance;
            if (service == null)
            {
                throw new InvalidOperationException(
                    "Multiplayer를 사용할 수 없습니다. Dashboard에서 Lobby와 Relay를 활성화하세요");
            }

            return service;
        }

        /// <summary>
        /// PC·모바일 모두 WSS. DTLS/UDP 가 막힌 망에서도 PC끼리·모바일 호스트가 붙기 쉽다.
        /// </summary>
        private static RelayProtocol PreferredRelayProtocol => RelayProtocol.WSS;

        private static SessionOptions BuildHostOptions(int seats, ApplyRelayHandler handler)
        {
            return new SessionOptions
                {
                    MaxPlayers = seats,
                    IsPrivate = false,
                    Type = SessionType,
                    SessionProperties = new Dictionary<string, SessionProperty>
                    {
                        { DataSeats, new SessionProperty(seats.ToString()) },
                    },
                }
                .WithRelayNetwork()
                .WithNetworkOptions(new NetworkOptions { RelayProtocol = PreferredRelayProtocol })
                .WithNetworkHandler(handler);
        }

        private static JoinSessionOptions BuildJoinOptions(ApplyRelayHandler handler)
        {
            return new JoinSessionOptions { Type = SessionType }
                .WithNetworkOptions(new NetworkOptions { RelayProtocol = PreferredRelayProtocol })
                .WithNetworkHandler(handler);
        }

        /// <summary>
        /// 세션과 Relay 할당을 만든다. 동시 호스트는 <see cref="SessionLimits.MaxHostedSessions"/> 를 넘지 못한다.
        /// 반환의 <see cref="HostedRelay.Code"/> 가 게스트가 입력할 Unity 조인 코드다.
        /// </summary>
        public static async Task<HostedRelay> CreateAsync(int seatCount, UnityTransport transport)
        {
            await EnsureSignedInAsync();
            if (_hostedCount >= SessionLimits.MaxHostedSessions)
            {
                throw new InvalidOperationException("이미 호스트 중인 방이 있습니다");
            }

            var seats = SessionLimits.ClampPlayers(seatCount);
            var handler = new ApplyRelayHandler(transport);
            var session = await RequireMultiplayer().CreateSessionAsync(BuildHostOptions(seats, handler));
            if (session == null)
            {
                throw new InvalidOperationException("Relay 세션을 만들지 못함");
            }

            if (string.IsNullOrEmpty(session.Code))
            {
                await session.AsHost().DeleteAsync();
                throw new InvalidOperationException("릴레이 방 코드를 받지 못함");
            }

            if (session.MaxPlayers > SessionLimits.MaxPlayers)
            {
                await session.AsHost().DeleteAsync();
                throw new InvalidOperationException("방 인원이 한도를 넘음");
            }

            _hosted = session;
            _hostedCount = 1;
            session.PlayerJoined += _ => TryLockIfFull(session);
            TryLockIfFull(session);
            return new HostedRelay(session, seats);
        }

        /// <summary>Unity 세션 조인 코드로 Relay 에 붙는다.</summary>
        public static async Task<JoinedRelay> JoinAsync(string joinCode, UnityTransport transport)
        {
            await EnsureSignedInAsync();
            var code = joinCode != null ? joinCode.Trim().ToUpperInvariant() : string.Empty;
            if (code.Length == 0)
            {
                throw new InvalidOperationException("방을 찾을 수 없음");
            }

            var handler = new ApplyRelayHandler(transport);
            ISession session;
            try
            {
                session = await RequireMultiplayer().JoinSessionByCodeAsync(code, BuildJoinOptions(handler));
            }
            catch (Exception e)
            {
                var detail = e.Message ?? string.Empty;
                if (detail.IndexOf("Cloud", StringComparison.OrdinalIgnoreCase) >= 0
                    || detail.IndexOf("Authentication", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    throw;
                }

                throw new InvalidOperationException("방을 찾을 수 없음");
            }

            if (session == null)
            {
                throw new InvalidOperationException("방을 찾을 수 없음");
            }

            if (session.MaxPlayers > SessionLimits.MaxPlayers || session.PlayerCount > SessionLimits.MaxPlayers)
            {
                await session.LeaveAsync();
                throw new InvalidOperationException("방 인원이 한도를 넘음");
            }

            var seats = SessionLimits.ClampPlayers(session.MaxPlayers);
            if (session.Properties != null
                && session.Properties.TryGetValue(DataSeats, out var seatProp)
                && int.TryParse(seatProp.Value, out var parsed))
            {
                seats = SessionLimits.ClampPlayers(parsed);
            }

            _joined = session;
            return new JoinedRelay(session, seats);
        }

        /// <summary>호스트 세션을 한 번 갱신한다. SDK 하트비트와 별개.</summary>
        public static async Task HeartbeatHostedAsync()
        {
            if (_hosted == null || _heartbeatBusy)
            {
                return;
            }

            _heartbeatBusy = true;
            try
            {
                await _hosted.RefreshAsync();
                TryLockIfFull(_hosted);
            }
            catch (Exception)
            {
            }
            finally
            {
                _heartbeatBusy = false;
            }
        }

        /// <summary>호스트 세션을 지우고 카운트를 비운다.</summary>
        public static async Task LeaveHostedAsync()
        {
            var session = _hosted;
            _hosted = null;
            _hostedCount = 0;
            if (session == null)
            {
                return;
            }

            try
            {
                await session.AsHost().DeleteAsync();
            }
            catch (Exception)
            {
            }
        }

        /// <summary>게스트가 세션에서 나간다.</summary>
        public static async Task LeaveJoinedAsync()
        {
            var session = _joined;
            _joined = null;
            if (session == null)
            {
                return;
            }

            try
            {
                await session.LeaveAsync();
            }
            catch (Exception)
            {
            }
        }

        private static void TryLockIfFull(ISession session)
        {
            if (session == null || !session.IsHost || session.PlayerCount < session.MaxPlayers)
            {
                return;
            }

            try
            {
                var host = session.AsHost();
                if (host.IsLocked)
                {
                    return;
                }

                host.IsLocked = true;
                _ = host.SavePropertiesAsync();
            }
            catch (Exception)
            {
            }
        }

        private sealed class ApplyRelayHandler : INetworkHandler
        {
            private readonly UnityTransport _transport;

            public ApplyRelayHandler(UnityTransport transport)
            {
                _transport = transport;
            }

            public Task StartAsync(NetworkConfiguration configuration)
            {
                if (_transport == null)
                {
                    throw new InvalidOperationException("전송이 없음");
                }

                if (configuration == null)
                {
                    throw new InvalidOperationException("Relay 네트워크 설정이 비어 있습니다");
                }

                // NGO 경로에서는 호스트·클라 모두 RelayServerData 에 할당이 들어간다.
                // RelayClientData 는 Entities 전용이라 비어 있을 수 있다.
                var data = configuration.RelayServerData;
                _transport.UseWebSockets = data.IsWebSocket != 0;
                _transport.SetRelayServerData(data);
                return Task.CompletedTask;
            }

            public Task StopAsync()
            {
                return Task.CompletedTask;
            }
        }
    }

    /// <summary>호스트가 만든 Relay 세션.</summary>
    public readonly struct HostedRelay
    {
        public HostedRelay(ISession session, int seatCount)
        {
            Session = session;
            SeatCount = seatCount;
            Code = session != null ? session.Code : string.Empty;
        }

        public ISession Session { get; }
        public int SeatCount { get; }

        /// <summary>게스트가 입력하는 Unity 조인 코드.</summary>
        public string Code { get; }
    }

    /// <summary>게스트가 받은 Relay 세션.</summary>
    public readonly struct JoinedRelay
    {
        public JoinedRelay(ISession session, int seatCount)
        {
            Session = session;
            SeatCount = seatCount;
        }

        public ISession Session { get; }
        public int SeatCount { get; }
    }
}
