using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Backend.App;
using Backend.Net;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Backend.Object.Net
{
    /// <summary>
    /// 릴레이·LAN 호스트 세션. 웹소켓 경로는 쓰지 않는다.
    /// </summary>
    public static class PlaySession
    {
        private static GameObject _root;
        private static PlayHost _host;
        private static PlayClientTransport _guest;
        private static NetworkManager _nm;

        /// <summary>방장 루프백 클라. 게스트면 null.</summary>
        public static NetClient HostClient => _host != null ? _host.HostClient : null;

        /// <summary>현재 PlaySession NetworkManager. Singleton 과 다를 수 있어 전송은 이쪽을 쓴다.</summary>
        public static NetworkManager Network => _nm;

        /// <summary>게스트 전송.</summary>
        public static PlayClientTransport GuestTransport => _guest;

        /// <summary>호스트 루프백. 매치 Tick 에서 Pump 할 때 쓴다.</summary>
        public static LocalLoopback Loopback => _host != null ? _host.Loopback : null;

        /// <summary>릴레이 또는 LAN 호스트를 연다. 릴레이면 표시용 코드는 Unity 조인 코드다.</summary>
        public static async UniTask<NetClient> StartHostAsync(
            ConnectionMode mode,
            string nick,
            string roomCode,
            int seatCount)
        {
            await StopAsync();
            await EnsureNetworkAsync(nick);
            try
            {
                if (WebBuild.IsPlayer)
                {
                    mode = ConnectionMode.Relay;
                }

                var effectiveCode = roomCode;
                if (mode == ConnectionMode.Relay)
                {
                    var hosted = await UgsLobbyRelay.CreateAsync(
                        seatCount,
                        _root.GetComponent<UnityTransport>());
                    effectiveCode = hosted.Code;
                    _host.MarkRelayHost();
                }
                else
                {
                    ApplyLanHost();
                }

                var client = _host.PrepareHost(seatCount, effectiveCode);
                _host.StartListening(isHost: true);
                return client;
            }
            catch
            {
                await StopAsync();
                throw;
            }
        }

        /// <summary>릴레이 또는 LAN 게스트로 붙는다.</summary>
        public static async UniTask<PlayClientTransport> StartGuestAsync(
            ConnectionMode mode,
            string nick,
            string roomCode)
        {
            await StopAsync();
            await EnsureNetworkAsync(nick);
            try
            {
                if (WebBuild.IsPlayer)
                {
                    mode = ConnectionMode.Relay;
                }

                if (mode == ConnectionMode.Relay)
                {
                    await UgsLobbyRelay.JoinAsync(roomCode, _root.GetComponent<UnityTransport>());
                    _host.MarkRelayGuest();
                }
                else
                {
                    ApplyLanClient(GatewaySettings.LanHost);
                }

                _guest = new PlayClientTransport();
                _host.StartListening(isHost: false);
                _nm.CustomMessagingManager.RegisterNamedMessageHandler(
                    PlayClientTransport.EventChannel,
                    OnGuestEvent);
                return _guest;
            }
            catch
            {
                await StopAsync();
                throw;
            }
        }

        /// <summary>수신·턴 펌프.</summary>
        public static void Pump()
        {
            _guest?.PumpReceived();
            _host?.Pump();
        }

        /// <summary>세션을 닫는다. 매치로 넘길 때는 호출하지 않는다.</summary>
        public static void Stop()
        {
            StopAsync().Forget();
        }

        /// <summary>
        /// NGO·릴레이를 안전하게 내린다.
        /// DestroyImmediate 는 WSS/Relay 종료 중 메인 스레드를 멈출 수 있어 쓰지 않는다.
        /// </summary>
        public static async UniTask StopAsync()
        {
            if (_guest != null && _nm != null && _nm.CustomMessagingManager != null)
            {
                _nm.CustomMessagingManager.UnregisterNamedMessageHandler(PlayClientTransport.EventChannel);
            }

            _host?.Shutdown();
            _guest = null;
            _host = null;
            var root = _root;
            var nm = _nm;
            _root = null;
            _nm = null;

            if (nm != null && nm && nm.IsListening)
            {
                nm.Shutdown();
            }

            if (root != null)
            {
                UnityEngine.Object.Destroy(root);
                await WaitSingletonClearedAsync();
            }
        }

        /// <summary>이 기기의 LAN IPv4. DNS 조회 없이 어댑터만 본다. WebGL 은 빈 문자열.</summary>
        public static string LocalIpv4()
        {
            if (WebBuild.IsPlayer)
            {
                return string.Empty;
            }

            try
            {
                foreach (var adapter in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (adapter.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)
                    {
                        continue;
                    }

                    if (adapter.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                    {
                        continue;
                    }

                    var props = adapter.GetIPProperties();
                    for (var i = 0; i < props.UnicastAddresses.Count; i++)
                    {
                        var addr = props.UnicastAddresses[i].Address;
                        if (addr.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(addr))
                        {
                            return addr.ToString();
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return string.Empty;
        }

        private static async UniTask EnsureNetworkAsync(string nick)
        {
            await ClearStaleSingletonAsync();

            _root = new GameObject("PlaySession");
            UnityEngine.Object.DontDestroyOnLoad(_root);
            var transport = _root.AddComponent<UnityTransport>();
            if (WebBuild.IsPlayer)
            {
                transport.UseWebSockets = true;
            }

            _nm = _root.AddComponent<NetworkManager>();
            _nm.SetSingleton();
            _nm.RunInBackground = true;
            if (_nm.NetworkConfig == null)
            {
                _nm.NetworkConfig = new NetworkConfig();
            }

            var config = _nm.NetworkConfig;
            config.NetworkTransport = transport;
            config.EnableSceneManagement = false;
            config.ConnectionApproval = true;
            config.ForceSamePrefabs = false;
            config.AutoSpawnPlayerPrefabClientSide = false;
            config.PlayerPrefab = null;
            if (config.Prefabs == null)
            {
                config.Prefabs = new NetworkPrefabs();
            }

            config.ConnectionData = Encoding.UTF8.GetBytes(nick ?? string.Empty);
            _host = _root.AddComponent<PlayHost>();
            _host.Bind(_nm, nick);
        }

        private static async UniTask ClearStaleSingletonAsync()
        {
            var stale = NetworkManager.Singleton;
            if (stale == null || !stale)
            {
                return;
            }

            if (stale.IsListening)
            {
                stale.Shutdown();
            }

            if (stale.gameObject != null)
            {
                UnityEngine.Object.Destroy(stale.gameObject);
            }

            await WaitSingletonClearedAsync();
        }

        private static async UniTask WaitSingletonClearedAsync()
        {
            await UniTask.Yield();
            for (var i = 0; i < 30 && NetworkManager.Singleton != null; i++)
            {
                await UniTask.Yield();
            }
        }

        private static void ApplyLanHost()
        {
            var utp = _root.GetComponent<UnityTransport>();
            utp.SetConnectionData("0.0.0.0", SessionLimits.LanPort, "0.0.0.0");
        }

        private static void ApplyLanClient(string hostIp)
        {
            var ip = hostIp != null ? hostIp.Trim() : string.Empty;
            if (ip.StartsWith("ws://", StringComparison.OrdinalIgnoreCase)
                || ip.StartsWith("wss://", StringComparison.OrdinalIgnoreCase)
                || ip.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || ip.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("랜은 서버 주소에 호스트 IP만 넣으세요");
            }

            if (string.IsNullOrEmpty(ip))
            {
                throw new InvalidOperationException("서버 주소에 호스트 LAN IP를 넣으세요");
            }

            var utp = _root.GetComponent<UnityTransport>();
            utp.SetConnectionData(ip, SessionLimits.LanPort);
        }

        private static void OnGuestEvent(ulong sender, FastBufferReader reader)
        {
            _ = sender;
            var json = PlayClientTransport.ReadJson(reader);
            _guest?.EnqueueEventJson(json);
        }
    }
}
